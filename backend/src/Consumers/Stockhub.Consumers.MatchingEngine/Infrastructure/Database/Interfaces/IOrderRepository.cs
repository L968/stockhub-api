using Stockhub.Consumers.MatchingEngine.Domain.Entities;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

internal interface IOrderRepository
{
    Task<IEnumerable<Order>> GetAllOpenOrdersAsync(CancellationToken cancellationToken);
}
