using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

internal interface IOrderBookRepository
{
    void BuildFromOrders(IEnumerable<Order> orders);

    void AddOrder(Order order);
    void CancelOrder(Guid orderId);
    void UpdateOrderFilledQuantity(Guid orderId, int filledQuantity);
    void RemoveOrder(Guid orderId);
    bool ContainsOrder(Guid orderId);

    OrderBook GetOrderBookSnapshot(Guid stockId);
}
