using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

internal interface IOrderBookRepository
{
    int Count { get; }
    int TotalOrders { get; }

    OrderBook Get(Guid stockId);
    void Set(OrderBook orderBook);
    void Remove(Guid stockId);
    void BuildFromOrders(IEnumerable<Order> orders);
}
