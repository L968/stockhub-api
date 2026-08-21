using Dapper;
using Npgsql;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

namespace Stockhub.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class SettlementIntegrationTests(PostgresFixture database)
{
    [Fact]
    public async Task Settlement_UpdatesTradeOrdersBalancesAndPortfolioAtomically()
    {
        await database.ResetAsync();
        Scenario scenario = await SeedScenarioAsync(buyerBalance: 2_000, sellerQuantity: 10);
        var repository = new TradeSettlementRepository(database.DataSource);

        TradeSettlementResult result = await repository.ExecuteAsync(
            scenario.Proposal,
            CancellationToken.None);

        Assert.True(result.IsExecuted);

        await using NpgsqlConnection connection = await database.DataSource.OpenConnectionAsync();
        Assert.Equal(5, await FilledQuantityAsync(connection, scenario.BuyOrderId));
        Assert.Equal(5, await FilledQuantityAsync(connection, scenario.SellOrderId));
        Assert.Equal(1_500, await BalanceAsync(connection, scenario.BuyerId));
        Assert.Equal(500, await BalanceAsync(connection, scenario.SellerId));
        Assert.Equal(5, await PositionAsync(connection, scenario.BuyerId, scenario.StockId));
        Assert.Equal(5, await PositionAsync(connection, scenario.SellerId, scenario.StockId));
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM orders.trade;"));
    }

    [Theory]
    [InlineData(400, 10, true, false)]
    [InlineData(2_000, 4, false, false)]
    [InlineData(2_000, 10, true, true)]
    public async Task Settlement_RejectsFrequentlyInvalidFundsOrPosition(
        decimal buyerBalance,
        int sellerQuantity,
        bool rejectBuyOrder,
        bool sameUser)
    {
        await database.ResetAsync();
        Scenario scenario = await SeedScenarioAsync(buyerBalance, sellerQuantity, sameUser);
        var repository = new TradeSettlementRepository(database.DataSource);

        TradeSettlementResult result = await repository.ExecuteAsync(
            scenario.Proposal,
            CancellationToken.None);

        Assert.False(result.IsExecuted);
        Assert.Equal(rejectBuyOrder ? scenario.BuyOrderId : scenario.SellOrderId, result.RejectedOrderId);

        await using NpgsqlConnection connection = await database.DataSource.OpenConnectionAsync();
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM orders.trade;"));
        Assert.True(await connection.ExecuteScalarAsync<bool>(
            "SELECT is_cancelled FROM orders.\"order\" WHERE id = @Id;",
            new { Id = result.RejectedOrderId }));
    }

    private async Task<Scenario> SeedScenarioAsync(
        decimal buyerBalance,
        int sellerQuantity,
        bool sameUser = false)
    {
        var buyerId = Guid.NewGuid();
        var scenario = new Scenario(
            buyerId, sameUser ? buyerId : Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using NpgsqlConnection connection = await database.DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            """
            INSERT INTO users."user" (id, current_balance) VALUES (@BuyerId, @BuyerBalance);
            INSERT INTO users."user" (id, current_balance)
            SELECT @SellerId, 0 WHERE @SellerId <> @BuyerId;
            INSERT INTO orders."order"
                (id, user_id, stock_id, side, price, quantity, filled_quantity, is_cancelled, created_at, updated_at)
            VALUES
                (@BuyOrderId, @BuyerId, @StockId, 0, 110, 5, 0, FALSE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
                (@SellOrderId, @SellerId, @StockId, 1, 100, 5, 0, FALSE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
            INSERT INTO orders.portfolio
                (id, user_id, stock_id, quantity, avg_price, created_at, updated_at)
            VALUES
                (@PortfolioId, @SellerId, @StockId, @SellerQuantity, 80, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
            """,
            new
            {
                scenario.BuyerId,
                scenario.SellerId,
                scenario.StockId,
                scenario.BuyOrderId,
                scenario.SellOrderId,
                BuyerBalance = buyerBalance,
                SellerQuantity = sellerQuantity,
                PortfolioId = Guid.NewGuid()
            });
        return scenario;
    }

    private static Task<int> FilledQuantityAsync(NpgsqlConnection connection, Guid orderId) =>
        connection.ExecuteScalarAsync<int>(
            "SELECT filled_quantity FROM orders.\"order\" WHERE id = @OrderId;",
            new { OrderId = orderId });

    private static Task<decimal> BalanceAsync(NpgsqlConnection connection, Guid userId) =>
        connection.ExecuteScalarAsync<decimal>(
            "SELECT current_balance FROM users.\"user\" WHERE id = @UserId;",
            new { UserId = userId });

    private static Task<int> PositionAsync(NpgsqlConnection connection, Guid userId, Guid stockId) =>
        connection.ExecuteScalarAsync<int>(
            "SELECT quantity FROM orders.portfolio WHERE user_id = @UserId AND stock_id = @StockId;",
            new { UserId = userId, StockId = stockId });

    private sealed record Scenario(
        Guid BuyerId,
        Guid SellerId,
        Guid StockId,
        Guid BuyOrderId,
        Guid SellOrderId)
    {
        public TradeProposal Proposal => new(StockId, BuyOrderId, SellOrderId, 100, 5);
    }
}
