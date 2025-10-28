using System.Collections.Concurrent;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

internal sealed class OrderBookRepository : IOrderBookRepository
{
    private readonly ConcurrentDictionary<Guid, OrderBook> _orderBooks = new();

    public int Count => _orderBooks.Count;
    public int TotalOrders => _orderBooks.Sum(o => o.Value.TotalOrders);

    public OrderBook Get(Guid stockId)
        => _orderBooks.GetOrAdd(stockId, id => new OrderBook(id));

    public void Set(OrderBook orderBook)
    {
        _orderBooks.AddOrUpdate(
            orderBook.StockId,
            orderBook,
            (_, _) => orderBook
        );
    }

    public void Remove(Guid stockId)
    {
        _orderBooks.TryRemove(stockId, out _);
    }

    public void BuildFromOrders(IEnumerable<Order> orders)
    {
        _orderBooks.Clear();

        IEnumerable<IGrouping<Guid, Order>> groupedOrders = orders.GroupBy(o => o.StockId);

        foreach (IGrouping<Guid, Order> group in groupedOrders)
        {
            var orderBook = new OrderBook(group.Key);

            foreach (Order order in group)
            {
                orderBook.Add(new Order
                {
                    Id = order.Id,
                    UserId = order.UserId,
                    StockId = order.StockId,
                    Side = order.Side,
                    Price = order.Price,
                    Quantity = order.Quantity,
                    FilledQuantity = order.FilledQuantity,
                    IsCancelled = order.IsCancelled,
                    CreatedAtUtc = order.CreatedAtUtc,
                    UpdatedAtUtc = order.UpdatedAtUtc
                });
            }

            Set(orderBook);
        }
    }
}
