using System.Data;
using Dapper;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

internal sealed class UserRepository(IDbConnection connection) : IUserRepository
{
    public Task<User?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        connection.QuerySingleOrDefaultAsync<User>(
            $"SELECT * FROM {Schemas.Orders}.user WHERE id = @Id",
            new { Id = userId });

    public async Task<bool> HasSufficientBalanceAsync(Guid userId, decimal requiredAmount, CancellationToken cancellationToken)
    {
        decimal? balance = await connection.QuerySingleOrDefaultAsync<decimal?>(
            $"SELECT current_balance FROM {Schemas.Orders}.user WHERE id = @Id",
            new { Id = userId });

        return balance.HasValue && balance.Value >= requiredAmount;
    }

    public Task UpdateBalanceAsync(Guid userId, decimal newBalance, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(
            $"UPDATE {Schemas.Orders}.user SET current_balance = @NewBalance WHERE id = @Id",
            new { Id = userId, NewBalance = newBalance });
}
