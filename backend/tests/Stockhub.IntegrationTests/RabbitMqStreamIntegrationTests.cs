using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.AMQP;
using RabbitMQ.Stream.Client.Reliable;

namespace Stockhub.IntegrationTests;

[Collection(RabbitMqStreamCollection.Name)]
public sealed class RabbitMqStreamIntegrationTests(RabbitMqStreamFixture rabbitMq)
{
    [Fact]
    public async Task SuperStream_RoutesTheSameStockToOnePartition()
    {
        string stream = $"orders-{Guid.NewGuid():N}";
        await rabbitMq.StreamSystem.CreateSuperStream(new PartitionsSuperStreamSpec(stream, 4));
        var receivedPartitions = new ConcurrentBag<string>();
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Consumer consumer = await Consumer.Create(new ConsumerConfig(rabbitMq.StreamSystem, stream)
        {
            IsSuperStream = true,
            OffsetSpec = new OffsetTypeFirst(),
            MessageHandler = (partition, _, _, _) =>
            {
                receivedPartitions.Add(partition);
                if (receivedPartitions.Count == 3)
                {
                    received.TrySetResult();
                }

                return Task.CompletedTask;
            }
        });
        Producer producer = await Producer.Create(new ProducerConfig(rabbitMq.StreamSystem, stream)
        {
            SuperStreamConfig = new SuperStreamConfig
            {
                Routing = message => (string)message.ApplicationProperties["stock-id"]
            }
        });
        string stockId = Guid.NewGuid().ToString("N");

        for (int index = 0; index < 3; index++)
        {
            await producer.Send(new Message(Encoding.UTF8.GetBytes($"order-{index}"))
            {
                Properties = new Properties { MessageId = Guid.NewGuid().ToString("N") },
                ApplicationProperties = new ApplicationProperties { ["stock-id"] = stockId }
            });
        }

        await received.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Single(receivedPartitions.Distinct());
        await producer.Close();
        await consumer.Close();
    }

    [Fact]
    public async Task SingleActiveConsumer_DistributesAndReassignsPartitions()
    {
        string stream = $"orders-{Guid.NewGuid():N}";
        string group = $"matching-{Guid.NewGuid():N}";
        await rabbitMq.StreamSystem.CreateSuperStream(new PartitionsSuperStreamSpec(stream, 4));
        var firstPartitions = new ConcurrentDictionary<string, byte>();
        var secondPartitions = new ConcurrentDictionary<string, byte>();

        Consumer first = await CreateGroupedConsumerAsync(stream, group, firstPartitions);
        Consumer second = await CreateGroupedConsumerAsync(stream, group, secondPartitions);

        await WaitUntilAsync(
            () => !firstPartitions.IsEmpty
                  && !secondPartitions.IsEmpty
                  && firstPartitions.Keys.Concat(secondPartitions.Keys).Distinct().Count() == 4
                  && !firstPartitions.Keys.Intersect(secondPartitions.Keys).Any());

        Assert.Equal(4, firstPartitions.Count + secondPartitions.Count);

        await first.Close();
        await WaitUntilAsync(() => secondPartitions.Count == 4);

        Assert.Equal(4, secondPartitions.Count);

        await second.Close();
    }

    private Task<Consumer> CreateGroupedConsumerAsync(
        string stream,
        string group,
        ConcurrentDictionary<string, byte> activePartitions) =>
        Consumer.Create(new ConsumerConfig(rabbitMq.StreamSystem, stream)
        {
            IsSuperStream = true,
            IsSingleActiveConsumer = true,
            Reference = group,
            OffsetSpec = new OffsetTypeFirst(),
            ConsumerUpdateListener = (reference, partition, isActive) =>
            {
                if (isActive)
                {
                    activePartitions[partition] = 0;
                }
                else
                {
                    activePartitions.TryRemove(partition, out _);
                }

                return Task.FromResult<IOffsetType>(new OffsetTypeFirst());
            },
            MessageHandler = (_, _, _, _) => Task.CompletedTask
        });

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        while (!condition())
        {
            await Task.Delay(100, timeout.Token);
        }
    }
}
