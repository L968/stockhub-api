using Dapper;
using Npgsql;
using Stockhub.Modules.Orders.Infrastructure.Messaging;

namespace Stockhub.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class OutboxIntegrationTests(PostgresFixture database)
{
    [Fact]
    public async Task ConcurrentDispatchers_DoNotClaimTheSameMessages()
    {
        await database.ResetAsync();
        await using NpgsqlConnection connection = await database.DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            """
            INSERT INTO orders.integration_outbox
                (id, order_id, stock_id, type, payload, occurred_at, attempts)
            SELECT gen_random_uuid(), gen_random_uuid(), gen_random_uuid(), 'OrderPlaced', '{}'::jsonb,
                   CURRENT_TIMESTAMP + (number * INTERVAL '1 millisecond'), 0
            FROM generate_series(1, 20) AS number;
            """);
        var repository = new OutboxRepository(database.DataSource);

        IReadOnlyList<OutboxItem>[] batches = await Task.WhenAll(
            repository.ClaimAsync(Guid.NewGuid(), CancellationToken.None),
            repository.ClaimAsync(Guid.NewGuid(), CancellationToken.None));
        List<OutboxItem> claimed = [.. batches.SelectMany(batch => batch)];

        Assert.Equal(20, claimed.Count);
        Assert.Equal(20, claimed.Select(item => item.Id).Distinct().Count());
    }
}
