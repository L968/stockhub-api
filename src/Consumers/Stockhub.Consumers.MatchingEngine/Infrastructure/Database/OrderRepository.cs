using System.Data;
using Dapper;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

internal sealed class OrderRepository(IDbConnection connection) : IOrderRepository
{
    public async Task<IEnumerable<Order>> GetAllOpenOrdersAsync(CancellationToken cancellationToken)
    {
        const string sql = @$"
            SELECT id,
                   user_id AS UserId,
                   stock_id AS StockId,
                   side AS Side,
                   price AS Price,
                   quantity AS Quantity,
                   filled_quantity AS FilledQuantity,
                   is_cancelled AS IsCancelled,
                   created_at AS CreatedAtUtc,
                   updated_at AS UpdatedAtUtc
            FROM {Schemas.Orders}.order
            WHERE is_cancelled = FALSE
            AND filled_quantity < quantity
        ";

        return await connection.QueryAsync<Order>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<Order?> GetAsync(Guid orderId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id,
                   user_id AS UserId,
                   stock_id AS StockId,
                   side AS Side,
                   price AS Price,
                   quantity AS Quantity,
                   filled_quantity AS FilledQuantity,
                   is_cancelled AS IsCancelled,
                   created_at AS CreatedAtUtc,
                   updated_at AS UpdatedAtUtc
            FROM orders.order
            WHERE id = @Id
        ";

        return await connection.QuerySingleOrDefaultAsync<Order>(
            new CommandDefinition(sql, new { Id = orderId }, cancellationToken: cancellationToken));
    }

    public async Task UpdateFilledQuantityAsync(Guid orderId, int filledQuantity, CancellationToken cancellationToken)
    {
        const string sql = @$"
            UPDATE {Schemas.Orders}.order
            SET filled_quantity = @FilledQuantity,
                updated_at = NOW() AT TIME ZONE 'UTC'
            WHERE id = @Id
            ";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = orderId, FilledQuantity = filledQuantity }, cancellationToken: cancellationToken));
    }

    public async Task CancelAsync(Guid orderId, CancellationToken cancellationToken)
    {
        const string sql = @$"
            UPDATE {Schemas.Orders}.order
            SET is_cancelled = TRUE,
                updated_at = NOW() AT TIME ZONE 'UTC'
            WHERE id = @Id
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = orderId }, cancellationToken: cancellationToken));
    }

    public async Task AddTradeAsync(Trade trade, CancellationToken cancellationToken)
    {
        const string sql = @$"
            INSERT INTO {Schemas.Orders}.trade (
                id,
                stock_id,
                buyer_id,
                seller_id,
                buy_order_id,
                sell_order_id,
                price,
                quantity,
                executed_at
            )
            VALUES (
                @Id,
                @StockId,
                @BuyerId,
                @SellerId,
                @BuyOrderId,
                @SellOrderId,
                @Price,
                @Quantity,
                NOW() AT TIME ZONE 'UTC'
            )
        ";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, trade, cancellationToken: cancellationToken));
    }
}
