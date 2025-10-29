using Stockhub.Consumers.MatchingEngine.Domain.Entities;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

internal interface IUserRepository
{
    Task<User?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> HasSufficientBalanceAsync(Guid userId, decimal requiredAmount, CancellationToken cancellationToken);
    Task UpdateBalanceAsync(Guid userId, decimal newBalance, CancellationToken cancellationToken);
}
