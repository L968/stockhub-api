using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.AMQP;
using RabbitMQ.Stream.Client.Reliable;
using Stockhub.Common.Messaging;

namespace Stockhub.Modules.Orders.Infrastructure.Messaging;

internal sealed class OrderStreamPublisher : IOrderStreamPublisher, IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource> _confirmations = new();
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly string _connectionString;
    private readonly RabbitMqStreamOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private StreamSystem? _streamSystem;
    private Producer? _producer;

    public OrderStreamPublisher(
        string connectionString,
        IOptions<RabbitMqStreamOptions> options,
        ILoggerFactory loggerFactory)
    {
        _connectionString = connectionString;
        _options = options.Value;
        _loggerFactory = loggerFactory;
    }

    public async Task PublishAsync(
        IReadOnlyCollection<OutboxItem> items,
        CancellationToken cancellationToken)
    {
        Producer producer = await GetProducerAsync(cancellationToken);
        var pending = new List<Task>(items.Count);

        foreach (OutboxItem item in items)
        {
            var confirmation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            if (!_confirmations.TryAdd(item.Id, confirmation))
            {
                throw new InvalidOperationException($"Event {item.Id} is already awaiting confirmation.");
            }

            var message = new Message(Encoding.UTF8.GetBytes(item.Payload))
            {
                Properties = new Properties { MessageId = item.Id.ToString("N") },
                ApplicationProperties = new ApplicationProperties
                {
                    ["stock-id"] = item.StockId.ToString("N")
                }
            };

            pending.Add(confirmation.Task);

            try
            {
                await producer.Send(message);
            }
            catch
            {
                _confirmations.TryRemove(item.Id, out _);
                throw;
            }
        }

        await Task.WhenAll(pending).WaitAsync(cancellationToken);
    }

    private async Task<Producer> GetProducerAsync(CancellationToken cancellationToken)
    {
        await _initializationLock.WaitAsync(cancellationToken);

        try
        {
            if (_producer is not null)
            {
                return _producer;
            }

            Uri connection = new(_connectionString);
            string[] credentials = connection.UserInfo.Split(':', 2);
            var streamEndpoint = new DnsEndPoint(connection.Host, _options.Port);
            var config = new StreamSystemConfig
            {
                UserName = Uri.UnescapeDataString(credentials[0]),
                Password = Uri.UnescapeDataString(credentials[1]),
                VirtualHost = string.IsNullOrWhiteSpace(connection.AbsolutePath) || connection.AbsolutePath == "/"
                    ? "/"
                    : Uri.UnescapeDataString(connection.AbsolutePath.TrimStart('/')),
                Endpoints = [streamEndpoint],
                AddressResolver = new AddressResolver(streamEndpoint),
                ClientProvidedName = "stockhub-order-outbox"
            };

            _streamSystem = await StreamSystem.Create(
                config,
                _loggerFactory.CreateLogger<StreamSystem>());

            if (!await _streamSystem.SuperStreamExists(RabbitMqStreamOptions.OrderStream))
            {
                await _streamSystem.CreateSuperStream(
                    new PartitionsSuperStreamSpec(RabbitMqStreamOptions.OrderStream, _options.Partitions));
            }

            _producer = await Producer.Create(
                new ProducerConfig(_streamSystem, RabbitMqStreamOptions.OrderStream)
                {
                    ClientProvidedName = "stockhub-order-outbox",
                    SuperStreamConfig = new SuperStreamConfig
                    {
                        Routing = message => (string)message.ApplicationProperties["stock-id"]
                    },
                    ConfirmationHandler = HandleConfirmationAsync
                },
                _loggerFactory.CreateLogger<Producer>());

            return _producer;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private Task HandleConfirmationAsync(MessagesConfirmation confirmation)
    {
        foreach (Message message in confirmation.Messages)
        {
            if (!Guid.TryParse(message.Properties.MessageId?.ToString(), out Guid eventId)
                || !_confirmations.TryRemove(eventId, out TaskCompletionSource? completion))
            {
                continue;
            }

            if (confirmation.Status == ConfirmationStatus.Confirmed)
            {
                completion.SetResult();
            }
            else
            {
                completion.SetException(new InvalidOperationException(
                    $"RabbitMQ rejected event {eventId}: {confirmation.Status}."));
            }
        }

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_producer is not null)
        {
            await _producer.Close();
        }

        if (_streamSystem is not null)
        {
            await _streamSystem.Close();
        }

        _initializationLock.Dispose();
    }
}
