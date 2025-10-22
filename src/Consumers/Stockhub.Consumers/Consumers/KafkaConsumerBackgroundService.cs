using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Stockhub.Consumers.Consumers;

public abstract class KafkaConsumerBackgroundService<TMessage>(
    IServiceProvider serviceProvider,
    ILogger logger,
    string topic,
    ConsumerConfig config
) : BackgroundService
{
    protected abstract Task HandleMessageAsync(TMessage message, IServiceProvider scope, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IConsumer<string, string> consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(topic);

        logger.LogInformation("Consumer {Consumer} iniciado para tópico {Topic}", GetType().Name, topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ConsumeResult<string, string> result = consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null)
                {
                    continue;
                }

                TMessage? message = Deserialize(result.Message.Value);
                if (message is null)
                {
                    logger.LogWarning("Mensagem inválida: {Value}", result.Message.Value);
                    continue;
                }

                using IServiceScope scope = serviceProvider.CreateScope();
                await HandleMessageAsync(message, scope.ServiceProvider, stoppingToken);
                consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Erro no consumo do tópico {Topic}", topic);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro inesperado no consumer {Consumer}", GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }

        consumer.Close();
        logger.LogInformation("Consumer {Consumer} finalizado", GetType().Name);
    }

    private static TMessage? Deserialize(string json)
        => JsonSerializer.Deserialize<TMessage>(json);
}
