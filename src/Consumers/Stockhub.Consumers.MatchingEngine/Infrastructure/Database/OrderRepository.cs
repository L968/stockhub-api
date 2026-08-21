using System.Data;
using Dapper;
using Npgsql;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

internal sealed class OrderRepository(NpgsqlDataSource dataSource) : IOrderRepository
{
    public async Task<IEnumerable<Order>> GetAllOpenOrdersAsync(CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

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

}
