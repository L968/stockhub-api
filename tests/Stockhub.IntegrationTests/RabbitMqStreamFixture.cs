using System.Net;
using DotNet.Testcontainers.Containers;
using RabbitMQ.Stream.Client;
using Testcontainers.RabbitMq;

namespace Stockhub.IntegrationTests;

public sealed class RabbitMqStreamFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4-management")
        .WithPortBinding(5552, true)
        .Build();

    public StreamSystem StreamSystem { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ExecResult pluginResult = await _container.ExecAsync(
            ["rabbitmq-plugins", "enable", "rabbitmq_stream", "rabbitmq_stream_management"]);

        if (pluginResult.ExitCode != 0)
        {
            throw new InvalidOperationException(pluginResult.Stderr);
        }

        Uri amqp = new(_container.GetConnectionString());
        string[] credentials = amqp.UserInfo.Split(':', 2);
        var endpoint = new DnsEndPoint(_container.Hostname, _container.GetMappedPublicPort(5552));
        var config = new StreamSystemConfig
        {
            UserName = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
            Endpoints = [endpoint],
            AddressResolver = new AddressResolver(endpoint),
            ClientProvidedName = "stockhub-integration-tests"
        };

        StreamSystem = await StreamSystem.Create(config);
    }

    public async Task DisposeAsync()
    {
        if (StreamSystem is not null)
        {
            await StreamSystem.Close();
        }

        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RabbitMqStreamCollection : ICollectionFixture<RabbitMqStreamFixture>
{
    public const string Name = "rabbitmq-stream";
}
