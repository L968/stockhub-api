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
            ? MatchBuyOrder(incoming)
            : MatchSellOrder(incoming);

        RemoveFilledOrders();

        return trades;
    }

    private List<Trade> MatchBuyOrder(OrderPlacedEvent incoming)
    {
        var trades = new List<Trade>();
        var matches = _sellOrders
            .Where(s => s.Price <= incoming.Price && s.FilledQuantity < s.Quantity)
            .OrderBy(s => s.Price)
            .ThenBy(s => s.CreatedAtUtc)
            .ToList();

        foreach (OrderPlacedEvent? sellOrder in matches)
        {
            if (incoming.Status == OrderStatus.Filled)
            {
                break;
            }

            int quantityToFill = GetQuantityToFill(incoming, sellOrder);

            trades.Add(CreateTrade(incoming, sellOrder, quantityToFill));

            incoming.FilledQuantity += quantityToFill;
            sellOrder.FilledQuantity += quantityToFill;

            sellOrder.Status = GetOrderStatus(sellOrder);
            incoming.Status = GetOrderStatus(incoming);
        }

        return trades;
    }

    private List<Trade> MatchSellOrder(OrderPlacedEvent incoming)
    {
        var trades = new List<Trade>();
        var matches = _buyOrders
            .Where(b => b.Price >= incoming.Price && b.FilledQuantity < b.Quantity)
            .OrderByDescending(b => b.Price)
            .ThenBy(s => s.CreatedAtUtc)
            .ToList();

        foreach (OrderPlacedEvent? buyOrder in matches)
        {
            if (incoming.Status == OrderStatus.Filled)
            {
                break;
            }

            int quantityToFill = GetQuantityToFill(incoming, buyOrder);

            trades.Add(CreateTrade(buyOrder, incoming, quantityToFill));

            incoming.FilledQuantity += quantityToFill;
            buyOrder.FilledQuantity += quantityToFill;

            buyOrder.Status = GetOrderStatus(buyOrder);
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
