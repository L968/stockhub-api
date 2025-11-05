using System.Data;
using Dapper;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

internal sealed class UserRepository(IDbConnection connection) : IUserRepository
{
    public Task<User?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        connection.QuerySingleOrDefaultAsync<User>(@$"
            SELECT
                id AS Id,
                email AS Email,
                full_name AS FullName,
                current_balance AS CurrentBalance,
                created_at AS CreatedAtUtc,
                updated_at AS UpdatedAtUtc
            FROM {Schemas.Users}.user
            WHERE id = @Id
        ", new { Id = userId });

    public async Task<bool> HasSufficientBalanceAsync(Guid userId, decimal requiredAmount, CancellationToken cancellationToken)
    {
        decimal? balance = await connection.QuerySingleOrDefaultAsync<decimal?>(@$"
            SELECT current_balance
            FROM {Schemas.Users}.user
            WHERE id = @Id
        ", new { Id = userId });

        return balance.HasValue && balance.Value >= requiredAmount;
    }

    public Task UpdateBalanceAsync(Guid userId, decimal newBalance, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(@$"
            UPDATE {Schemas.Users}.user
            SET current_balance = @NewBalance
            WHERE id = @Id
        ", new { Id = userId, NewBalance = newBalance });
}
