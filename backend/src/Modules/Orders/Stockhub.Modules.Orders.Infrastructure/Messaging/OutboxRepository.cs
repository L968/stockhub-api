using Dapper;
using Npgsql;

namespace Stockhub.Modules.Orders.Infrastructure.Messaging;

internal sealed class OutboxRepository(NpgsqlDataSource dataSource) : IOutboxRepository
{
    private const int BatchSize = 200;

    public async Task<IReadOnlyList<OutboxItem>> ClaimAsync(
        Guid lockId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH pending AS (
                SELECT id
                FROM orders.integration_outbox
                WHERE published_at IS NULL
                  AND (locked_until IS NULL OR locked_until < NOW() AT TIME ZONE 'UTC')
                ORDER BY occurred_at
                FOR UPDATE SKIP LOCKED
                LIMIT @BatchSize
            )
            UPDATE orders.integration_outbox AS message
            SET lock_id = @LockId,
                locked_until = (NOW() AT TIME ZONE 'UTC') + INTERVAL '30 seconds',
                attempts = attempts + 1,
                last_error = NULL
            FROM pending
            WHERE message.id = pending.id
            RETURNING message.id AS Id,
                      message.stock_id AS StockId,
                      message.payload::text AS Payload;
            """;

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        IEnumerable<OutboxItem> items = await connection.QueryAsync<OutboxItem>(
            new CommandDefinition(sql, new { LockId = lockId, BatchSize }, cancellationToken: cancellationToken));

        return items.ToList();
    }

    public Task MarkPublishedAsync(
        Guid lockId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) => ExecuteAsync(
            """
            UPDATE orders.integration_outbox
            SET published_at = NOW() AT TIME ZONE 'UTC',
                lock_id = NULL,
                locked_until = NULL
            WHERE lock_id = @LockId AND id = ANY(@Ids);
            """,
            new { LockId = lockId, Ids = ids.ToArray() },
            cancellationToken);

    public Task ReleaseAsync(
        Guid lockId,
        IReadOnlyCollection<Guid> ids,
        string error,
        CancellationToken cancellationToken) => ExecuteAsync(
            """
            UPDATE orders.integration_outbox
            SET lock_id = NULL,
                locked_until = NULL,
                last_error = @Error
            WHERE lock_id = @LockId AND id = ANY(@Ids);
            """,
            new { LockId = lockId, Ids = ids.ToArray(), Error = error[..Math.Min(error.Length, 2000)] },
            cancellationToken);

    private async Task ExecuteAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }
}
