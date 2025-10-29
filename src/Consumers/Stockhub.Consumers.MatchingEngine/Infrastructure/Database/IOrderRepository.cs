using Stockhub.Consumers.MatchingEngine.Domain.Entities;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

internal interface IOrderRepository
{
    Task<IEnumerable<Order>> GetAllOpenOrdersAsync(CancellationToken cancellationToken);
    Task<Order?> GetAsync(Guid orderId, CancellationToken cancellationToken);
    Task CancelAsync(Guid orderId, CancellationToken cancellationToken);
    Task UpdateFilledQuantity(Guid orderId, int newQuantity, CancellationToken cancellationToken);

    Task AddTradeAsync(Trade trade, CancellationToken cancellationToken);
}
