using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Domain.Events.OrderPlaced;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;

namespace Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;

internal sealed class OrderBook(Guid stockId)
{
    private readonly List<OrderPlacedEvent> _buyOrders = [];
    private readonly List<OrderPlacedEvent> _sellOrders = [];

    public Guid StockId { get; } = stockId;
    public bool IsEmpty => !_buyOrders.Any() && !_sellOrders.Any();
    public int TotalOrders => _buyOrders.Count + _sellOrders.Count;

    public void Add(OrderPlacedEvent order)
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

    public IEnumerable<Trade> Match(OrderPlacedEvent incoming)
    {
        List<Trade> trades = incoming.Side == OrderSide.Buy
            ? MatchIncomingBuyOrder(incoming)
            : MatchIncomingSellOrder(incoming);

        RemoveFilledOrders();

        return trades;
    }

    private List<Trade> MatchIncomingBuyOrder(OrderPlacedEvent incoming)
    {
        List<OrderPlacedEvent> matches = FindMatchingSellOrders(incoming);
        return ExecuteMatches(incoming, matches);
    }

    private List<Trade> MatchIncomingSellOrder(OrderPlacedEvent incoming)
    {
        List<OrderPlacedEvent> matches = FindMatchingBuyOrders(incoming);
        return ExecuteMatches(incoming, matches);
    }

    private List<OrderPlacedEvent> FindMatchingBuyOrders(OrderPlacedEvent sellOrder)
    {
        return _buyOrders
            .Where(b => b.Price >= sellOrder.Price && b.FilledQuantity < b.Quantity)
            .OrderByDescending(b => b.Price)
            .ThenBy(b => b.CreatedAtUtc)
            .ToList();
    }

    private List<OrderPlacedEvent> FindMatchingSellOrders(OrderPlacedEvent buyOrder)
    {
        return _sellOrders
            .Where(s => s.Price <= buyOrder.Price && s.FilledQuantity < s.Quantity)
            .OrderBy(s => s.Price)
            .ThenBy(s => s.CreatedAtUtc)
            .ToList();
    }

    private List<Trade> ExecuteMatches(OrderPlacedEvent incoming, List<OrderPlacedEvent> oppositeOrders)
    {
        var trades = new List<Trade>();

        foreach (OrderPlacedEvent oppositeOrder in oppositeOrders)
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

    private Trade CreateTrade(OrderPlacedEvent buyOrder, OrderPlacedEvent sellOrder, int quantity)
    {
        return new Trade(
            stockId: StockId,
            buyerId: buyOrder.UserId,
            sellerId: sellOrder.UserId,
            buyOrderId: buyOrder.OrderId,
            sellOrderId: sellOrder.OrderId,
            price: sellOrder.Price,
            quantity: quantity
        );
    }

    private static int GetQuantityToFill(OrderPlacedEvent incoming, OrderPlacedEvent opposite)
    {
        return Math.Min(
            incoming.Quantity - incoming.FilledQuantity,
            opposite.Quantity - opposite.FilledQuantity
        );
    }

    private static OrderStatus GetOrderStatus(OrderPlacedEvent order)
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
