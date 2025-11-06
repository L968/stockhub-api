using System.Collections.Concurrent;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

internal sealed class OrderBookRepository : IOrderBookRepository
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public void BuildFromOrders(IEnumerable<Order> orders)
    {
        _orders.Clear();

        foreach (Order order in orders)
        {
            _orders[order.Id] = order;
        }
    }

    public void AddOrder(Order order)
    {
        _orders[order.Id] = order;
    }

    public void CancelOrder(Guid orderId)
    {
        if (_orders.TryGetValue(orderId, out Order? order))
        {
            order.Cancel();
            RemoveOrder(orderId);
        }
    }

    public void UpdateOrderFilledQuantity(Guid orderId, int filledQuantity)
    {
        if (_orders.TryGetValue(orderId, out Order? order))
        {
            order.FilledQuantity = filledQuantity;
        }
    }

    public void RemoveOrder(Guid orderId)
    {
        _orders.TryRemove(orderId, out _);
    }

    public bool ContainsOrder(Guid orderId)
    {
        return _orders.ContainsKey(orderId);
    }

    public OrderBook GetOrderBookSnapshot(Guid stockId)
    {
        var stockOrders = _orders.Values
            .Where(o => o.StockId == stockId)
            .ToList();

        return new OrderBook(stockId, stockOrders);
    }
}
