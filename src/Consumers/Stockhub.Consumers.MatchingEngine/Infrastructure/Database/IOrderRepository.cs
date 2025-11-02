using Stockhub.Consumers.MatchingEngine.Domain.Entities;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

internal interface IOrderRepository
{
    Task<IEnumerable<Order>> GetAllOpenOrdersAsync(CancellationToken cancellationToken);
    Task<Order?> GetAsync(Guid orderId, CancellationToken cancellationToken);
    Task UpdateFilledQuantityAsync(Guid orderId, int filledQuantity, CancellationToken cancellationToken);
    Task CancelAsync(Guid orderId, CancellationToken cancellationToken);

    Task AddTradeAsync(Trade trade, CancellationToken cancellationToken);
}
