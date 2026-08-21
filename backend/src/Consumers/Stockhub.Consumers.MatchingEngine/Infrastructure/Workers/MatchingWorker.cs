using System.Collections.Concurrent;
using System.Buffers;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;
using Stockhub.Common.Messaging;
using Stockhub.Common.Messaging.Contracts.Orders;
using Stockhub.Consumers.MatchingEngine.Application.Services;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;
using DomainOrderSide = Stockhub.Consumers.MatchingEngine.Domain.Enums.OrderSide;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Workers;

internal sealed class MatchingWorkerHostedService(
    string rabbitMqConnectionString,
    IOptions<RabbitMqStreamOptions> options,
    IOrderRepository orderRepository,
    IOrderBookRepository orderBooks,
    IMatchingEngineService matchingEngine,
    ILoggerFactory loggerFactory,
    ILogger<MatchingWorkerHostedService> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<string, Task> _partitionInitializations = new();
    private StreamSystem? _streamSystem;
    private Consumer? _consumer;
    private string[] _partitions = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _streamSystem = await CreateStreamSystemAsync();

        if (!await _streamSystem.SuperStreamExists(RabbitMqStreamOptions.OrderStream))
        {
            await _streamSystem.CreateSuperStream(
                new PartitionsSuperStreamSpec(RabbitMqStreamOptions.OrderStream, options.Value.Partitions));
        }

        _partitions = [.. await _streamSystem.QueryPartition(RabbitMqStreamOptions.OrderStream)];

        _consumer = await Consumer.Create(
            new ConsumerConfig(_streamSystem, RabbitMqStreamOptions.OrderStream)
            {
                IsSuperStream = true,
                IsSingleActiveConsumer = true,
                Reference = RabbitMqStreamOptions.MatchingConsumerGroup,
                ClientProvidedName = $"matching-{Environment.MachineName}-{Environment.ProcessId}",
                OffsetSpec = new OffsetTypeFirst(),
                ConsumerUpdateListener = OnConsumerUpdateAsync,
                MessageHandler = HandleMessageAsync
            },
            loggerFactory.CreateLogger<Consumer>());

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Matching worker connected to {Stream} with {PartitionCount} partitions",
                RabbitMqStreamOptions.OrderStream,
                _partitions.Length);
        }

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task<IOffsetType> OnConsumerUpdateAsync(
        string reference,
        string partition,
        bool isActive)
    {
        if (!isActive)
        {
            _partitionInitializations.TryRemove(partition, out _);
            orderBooks.RemovePartition(partition);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Released matching partition {Partition}", partition);
            }
            return new OffsetTypeNext();
        }

        _partitionInitializations[partition] = InitializePartitionAsync(partition, CancellationToken.None);
        ulong? offset = await _streamSystem!.TryQueryOffset(reference, partition);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Acquired matching partition {Partition}", partition);
        }
        return offset.HasValue ? new OffsetTypeOffset(offset.Value + 1) : new OffsetTypeFirst();
    }

    private async Task HandleMessageAsync(
        string partition,
        IConsumer consumer,
        MessageContext context,
        Message message)
    {
        Task initialization = _partitionInitializations.GetOrAdd(
            partition,
            key => InitializePartitionAsync(key, CancellationToken.None));

        await initialization;

        OrderPlaced integrationEvent = JsonSerializer.Deserialize<OrderPlaced>(message.Data.Contents.ToArray())
            ?? throw new InvalidOperationException("OrderPlaced payload is invalid.");

        var order = new Order
        {
            Id = integrationEvent.OrderId,
            UserId = integrationEvent.UserId,
            StockId = integrationEvent.StockId,
            Side = (DomainOrderSide)integrationEvent.Side,
            Price = integrationEvent.Price,
            Quantity = integrationEvent.Quantity,
            CreatedAtUtc = integrationEvent.CreatedAtUtc,
            UpdatedAtUtc = integrationEvent.CreatedAtUtc
        };

        await matchingEngine.ProcessOrderAsync(partition, order, CancellationToken.None);
        await consumer.StoreOffset(context.Offset);
    }

    private async Task InitializePartitionAsync(string partition, CancellationToken cancellationToken)
    {
        IEnumerable<Order> openOrders = await orderRepository.GetAllOpenOrdersAsync(cancellationToken);
        var routing = new HashRoutingMurmurStrategy(message => message.Properties.MessageId.ToString()!);
        var partitionOrders = new List<Order>();
        List<string> partitions = _partitions.ToList();

        foreach (Order order in openOrders)
        {
            var routeMessage = new Message([])
            {
                Properties = new RabbitMQ.Stream.Client.AMQP.Properties
                {
                    MessageId = order.StockId.ToString("N")
                }
            };

            List<string> route = await routing.Route(routeMessage, partitions);

            if (route.Contains(partition, StringComparer.Ordinal))
            {
                partitionOrders.Add(order);
            }
        }

        orderBooks.ReplacePartition(partition, partitionOrders);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Rebuilt partition {Partition} with {OrderCount} open orders",
                partition,
                partitionOrders.Count);
        }
    }

    private Task<StreamSystem> CreateStreamSystemAsync()
    {
        Uri connection = new(rabbitMqConnectionString);
        string[] credentials = connection.UserInfo.Split(':', 2);
        var streamEndpoint = new DnsEndPoint(connection.Host, options.Value.Port);
        var config = new StreamSystemConfig
        {
            UserName = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
            VirtualHost = string.IsNullOrWhiteSpace(connection.AbsolutePath) || connection.AbsolutePath == "/"
                ? "/"
                : Uri.UnescapeDataString(connection.AbsolutePath.TrimStart('/')),
            Endpoints = [streamEndpoint],
            AddressResolver = new AddressResolver(streamEndpoint),
            ClientProvidedName = $"stockhub-matching-{Environment.ProcessId}"
        };

        return StreamSystem.Create(config, loggerFactory.CreateLogger<StreamSystem>());
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_consumer is not null)
        {
            await _consumer.Close();
        }

        if (_streamSystem is not null)
        {
            await _streamSystem.Close();
        }

        await base.StopAsync(cancellationToken);
    }
}
