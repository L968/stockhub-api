using System.Collections.Concurrent;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

internal sealed class OrderBookRepository : IOrderBookRepository
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Order>> _books = new();
    private readonly ConcurrentDictionary<Guid, Guid> _stockByOrder = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, byte>> _stocksByPartition = new();

    public void ReplacePartition(string partition, IEnumerable<Order> orders)
    {
        RemovePartition(partition);

        foreach (Order order in orders)
        {
            AddOrder(partition, order);
        }
    }

    public void RemovePartition(string partition)
    {
        if (!_stocksByPartition.TryRemove(partition, out ConcurrentDictionary<Guid, byte>? stocks))
        {
            return;
        }

        foreach (Guid stockId in stocks.Keys)
        {
            if (!_books.TryRemove(stockId, out ConcurrentDictionary<Guid, Order>? orders))
            {
                continue;
            }

            foreach (Guid orderId in orders.Keys)
            {
                _stockByOrder.TryRemove(orderId, out _);
            }
        }
    }

    public void AddOrder(string partition, Order order)
    {
        ConcurrentDictionary<Guid, Order> book = _books.GetOrAdd(order.StockId, _ => new());
        book[order.Id] = order;
        _stockByOrder[order.Id] = order.StockId;

        ConcurrentDictionary<Guid, byte> stocks = _stocksByPartition.GetOrAdd(partition, _ => new());
        stocks.TryAdd(order.StockId, 0);
    }

    public void CancelOrder(Guid orderId)
    {
        if (TryGetOrder(orderId, out Order? order) && order is not null)
        {
            order.Cancel();
            RemoveOrder(orderId);
        }
    }

    public void UpdateOrderFilledQuantity(Guid orderId, int filledQuantity)
    {
        if (!TryGetOrder(orderId, out Order? order) || order is null)
        {
            return;
        }

        order.FilledQuantity = filledQuantity;

        if (order.Status == OrderStatus.Filled)
        {
            RemoveOrder(orderId);
        }
    }

    public void RemoveOrder(Guid orderId)
    {
        if (!_stockByOrder.TryRemove(orderId, out Guid stockId)
            || !_books.TryGetValue(stockId, out ConcurrentDictionary<Guid, Order>? book))
        {
            return;
        }

        book.TryRemove(orderId, out _);
    }

    public bool ContainsOrder(Guid orderId) => _stockByOrder.ContainsKey(orderId);

    public OrderBook GetOrderBookSnapshot(Guid stockId)
    {
        List<Order> orders = _books.TryGetValue(stockId, out ConcurrentDictionary<Guid, Order>? book)
            ? book.Values.ToList()
            : [];

        return new OrderBook(stockId, orders);
    }

    private bool TryGetOrder(Guid orderId, out Order? order)
    {
        order = null;

        return _stockByOrder.TryGetValue(orderId, out Guid stockId)
            && _books.TryGetValue(stockId, out ConcurrentDictionary<Guid, Order>? book)
            && book.TryGetValue(orderId, out order);
    }
}
