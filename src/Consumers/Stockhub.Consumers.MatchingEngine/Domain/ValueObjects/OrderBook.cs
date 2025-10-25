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

    public void RemoveFilledOrders()
    {
        RemoveOrders(_buyOrders);
        RemoveOrders(_sellOrders);
    }

    public IEnumerable<Trade> Match(OrderPlacedEvent incoming)
    {
        return incoming.Side == OrderSide.Buy
            ? MatchBuyOrder(incoming)
            : MatchSellOrder(incoming);
    }

    private List<Trade> MatchBuyOrder(OrderPlacedEvent incoming)
    {
        var trades = new List<Trade>();
        var matches = _sellOrders
            .Where(s => s.Price <= incoming.Price && s.FilledQuantity < s.Quantity)
            .OrderBy(s => s.Price)
            .ThenBy(s => s.CreatedAtUtc)
            .ToList();

        foreach (OrderPlacedEvent? sell in matches)
        {
            if (incoming.Quantity - incoming.FilledQuantity <= 0)
            {
                break;
            }

            int quantityToFill = Math.Min(incoming.Quantity - incoming.FilledQuantity, sell.Quantity - sell.FilledQuantity);

            trades.Add(CreateTrade(incoming, sell, quantityToFill));

            incoming.FilledQuantity += quantityToFill;
            sell.FilledQuantity += quantityToFill;

            sell.Status = (sell.FilledQuantity == sell.Quantity) ? OrderStatus.Filled : OrderStatus.PartiallyFilled;
        }

        if (incoming.FilledQuantity == incoming.Quantity && trades.Count > 0)
        {
            incoming.Status = OrderStatus.Filled;
        }
        else if (incoming.FilledQuantity > 0 && trades.Count > 0)
        {
            incoming.Status = OrderStatus.PartiallyFilled;
        }

        _sellOrders.RemoveAll(s => s.FilledQuantity == s.Quantity);

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

        foreach (OrderPlacedEvent? buy in matches)
        {
            if (incoming.Quantity - incoming.FilledQuantity <= 0)
            {
                break;
            }

            int quantityToFill = Math.Min(incoming.Quantity - incoming.FilledQuantity, buy.Quantity - buy.FilledQuantity);

            trades.Add(CreateTrade(buy, incoming, quantityToFill));

            incoming.FilledQuantity += quantityToFill;
            buy.FilledQuantity += quantityToFill;

            buy.Status = (buy.FilledQuantity == buy.Quantity) ? OrderStatus.Filled : OrderStatus.PartiallyFilled;
        }

        if (incoming.FilledQuantity == incoming.Quantity && trades.Count > 0)
        {
            incoming.Status = OrderStatus.Filled;
        }
        else if (incoming.FilledQuantity > 0 && trades.Count > 0)
        {
            incoming.Status = OrderStatus.PartiallyFilled;
        }

        _buyOrders.RemoveAll(b => b.FilledQuantity == b.Quantity);

        return trades;
    }

    private static void RemoveOrders(List<OrderPlacedEvent> orders)
    {
        orders.RemoveAll(o => o.Status == OrderStatus.Filled || o.FilledQuantity == o.Quantity);
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
}
