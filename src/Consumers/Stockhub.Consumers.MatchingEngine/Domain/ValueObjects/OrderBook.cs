using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;

namespace Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;

internal sealed class OrderBook(Guid stockId)
{
    private readonly List<Order> _buyOrders = [];
    private readonly List<Order> _sellOrders = [];

    public Guid StockId { get; } = stockId;
    public bool IsEmpty => !_buyOrders.Any() && !_sellOrders.Any();
    public int TotalOrders => _buyOrders.Count + _sellOrders.Count;

    public void Add(Order order)
    {
        if (order.Side == OrderSide.Buy)
        {
            _buyOrders.Add(order);
        }
        else
        {
            _sellOrders.Add(order);
        }
    }

    public IEnumerable<Trade> Match(Order incoming)
    {
        List<Trade> trades = incoming.Side == OrderSide.Buy
            ? MatchIncomingBuyOrder(incoming)
            : MatchIncomingSellOrder(incoming);

        RemoveFilledOrders();

        return trades;
    }

    private List<Trade> MatchIncomingBuyOrder(Order incoming)
    {
        List<Order> matches = FindMatchingSellOrders(incoming);
        return ExecuteMatches(incoming, matches);
    }

    private List<Trade> MatchIncomingSellOrder(Order incoming)
    {
        List<Order> matches = FindMatchingBuyOrders(incoming);
        return ExecuteMatches(incoming, matches);
    }

    private List<Order> FindMatchingBuyOrders(Order sellOrder)
    {
        return _buyOrders
            .Where(b => b.Price >= sellOrder.Price && b.FilledQuantity < b.Quantity)
            .OrderByDescending(b => b.Price)
            .ThenBy(b => b.CreatedAtUtc)
            .ToList();
    }

    private List<Order> FindMatchingSellOrders(Order buyOrder)
    {
        return _sellOrders
            .Where(s => s.Price <= buyOrder.Price && s.FilledQuantity < s.Quantity)
            .OrderBy(s => s.Price)
            .ThenBy(s => s.CreatedAtUtc)
            .ToList();
    }

    private List<Trade> ExecuteMatches(Order incoming, List<Order> oppositeOrders)
    {
        var trades = new List<Trade>();

        foreach (Order oppositeOrder in oppositeOrders)
        {
            if (incoming.Status == OrderStatus.Filled)
            {
                break;
            }

            int quantityToFill = GetQuantityToFill(incoming, oppositeOrder);

            Trade trade = incoming.Side == OrderSide.Buy
                ? CreateTrade(incoming, oppositeOrder, quantityToFill)
                : CreateTrade(oppositeOrder, incoming, quantityToFill);

            trades.Add(trade);

            incoming.FilledQuantity += quantityToFill;
            oppositeOrder.FilledQuantity += quantityToFill;

            oppositeOrder.Status = GetOrderStatus(oppositeOrder);
            incoming.Status = GetOrderStatus(incoming);
        }

        return trades;
    }

    private Trade CreateTrade(Order buyOrder, Order sellOrder, int quantity)
    {
        return new Trade(
            stockId: StockId,
            buyerId: buyOrder.UserId,
            sellerId: sellOrder.UserId,
            buyOrderId: buyOrder.Id,
            sellOrderId: sellOrder.Id,
            price: sellOrder.Price,
            quantity: quantity
        );
    }

    private static int GetQuantityToFill(Order incoming, Order opposite)
    {
        return Math.Min(
            incoming.Quantity - incoming.FilledQuantity,
            opposite.Quantity - opposite.FilledQuantity
        );
    }

    private static OrderStatus GetOrderStatus(Order order)
    {
        if (order.FilledQuantity == 0)
        {
            return OrderStatus.Pending;
        }

        if (order.FilledQuantity == order.Quantity)
        {
            return OrderStatus.Filled;
        }

        return OrderStatus.PartiallyFilled;
    }

    private void RemoveFilledOrders()
    {
        _buyOrders.RemoveAll(o => o.Status == OrderStatus.Filled);
        _sellOrders.RemoveAll(o => o.Status == OrderStatus.Filled);
    }
}
