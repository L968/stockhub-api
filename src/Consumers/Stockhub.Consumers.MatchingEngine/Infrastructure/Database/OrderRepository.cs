using System.Data;
using Dapper;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

internal sealed class OrderRepository(IDbConnection connection) : IOrderRepository
{
    public Task<IEnumerable<Order>> GetAllOpenOrdersAsync(CancellationToken cancellationToken) =>
        connection.QueryAsync<Order>(
            $"SELECT * FROM {Schemas.Orders}.order WHERE status = 0 OR status = 1");

    public Task<Order?> GetAsync(Guid orderId, CancellationToken cancellationToken) =>
        connection.QuerySingleOrDefaultAsync<Order>(
            $"SELECT * FROM {Schemas.Orders}.order WHERE id = @Id",
            new { Id = orderId });

    public Task CancelAsync(Guid orderId, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(
            $"UPDATE {Schemas.Orders}.order SET is_cancelled = TRUE WHERE id = @Id",
            new { Id = orderId });

    public Task UpdateFilledQuantity(Guid orderId, int newQuantity, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(
            $"UPDATE {Schemas.Orders}.order SET filled_quantity = @NewQuantity WHERE Id = @Id",
            new { Id = orderId, NewQuantity = newQuantity });

    public Task AddTradeAsync(Trade trade, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(
            $"INSERT INTO {Schemas.Orders}.trade (Id, StockId, BuyOrderId, SellOrderId, Price, Quantity, CreatedAtUtc) " +
            "VALUES (@Id, @StockId, @BuyOrderId, @SellOrderId, @Price, @Quantity, @CreatedAtUtc)",
            trade);
}
