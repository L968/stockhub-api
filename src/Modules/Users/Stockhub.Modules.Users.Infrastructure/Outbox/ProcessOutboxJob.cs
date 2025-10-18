using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;
using Stockhub.Common.Application.DomainEvent;
using Stockhub.Common.Domain.DomainEvents;
using Stockhub.Common.Infrastructure.Outbox;
using Stockhub.Common.Infrastructure.Serialization;
using Stockhub.Modules.Users.Application;
using Stockhub.Modules.Users.Infrastructure.Database;

namespace Stockhub.Modules.Users.Infrastructure.Outbox;

[DisallowConcurrentExecution]
internal sealed class ProcessOutboxJob(
    UsersDbContext dbContext,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<OutboxOptions> outboxOptions,
    ILogger<ProcessOutboxJob> logger) : IJob
{
    private const string ModuleName = "Users";

    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("{Module} - Beginning to process outbox messages", ModuleName);

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();

        IReadOnlyList<OutboxMessage> outboxMessages = await GetUnprocessedOutboxMessagesAsync();

        logger.LogInformation("{Module} - Found {Count} unprocessed messages", ModuleName, outboxMessages.Count);

        foreach (OutboxMessage outboxMessage in outboxMessages)
        {
            logger.LogInformation("{Module} - Processing outbox message {MessageId}", ModuleName, outboxMessage.Id);

            try
            {
                IDomainEvent domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(
                    outboxMessage.Payload,
                    SerializerSettings.Instance)!;

                using IServiceScope scope = serviceScopeFactory.CreateScope();

                IEnumerable<IDomainEventHandler> domainEventHandlers = DomainEventHandlersFactory.GetHandlers(
                    domainEvent.GetType(),
                    scope.ServiceProvider,
                    AssemblyReference.Assembly);

                foreach (IDomainEventHandler domainEventHandler in domainEventHandlers)
                {
                    await domainEventHandler.Handle(domainEvent);
                }

                outboxMessage.Error = "";
                outboxMessage.ProcessedOnUtc = DateTime.UtcNow;

                logger.LogInformation(
                    "{Module} - Successfully processed message {MessageId} - {EventName}",
                    ModuleName,
                    outboxMessage.Id,
                    domainEvent.GetType().Name);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "{Module} - Exception while processing outbox message {MessageId}",
                    ModuleName,
                    outboxMessage.Id);

                outboxMessage.Error = exception.ToString();
            }
        }

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        logger.LogInformation("{Module} - Completed processing outbox messages", ModuleName);
    }

    private async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedOutboxMessagesAsync()
    {
        string sql =$@"
             SELECT *
             FROM {Schemas.Users}.""OutboxMessages""
             WHERE ""ProcessedOnUtc"" IS NULL
             ORDER BY ""OccurredOnUtc""
             LIMIT {outboxOptions.Value.BatchSize}
             FOR UPDATE SKIP LOCKED
        ";

        return await dbContext.OutboxMessages
            .FromSqlRaw(sql)
            .ToListAsync();
    }
}
