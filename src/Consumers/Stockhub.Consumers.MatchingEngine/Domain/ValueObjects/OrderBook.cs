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

    public IEnumerable<Trade> Match(Order incomingOrder)
    {
        List<Trade> trades = incomingOrder.Side == OrderSide.Buy
            ? MatchIncomingBuyOrder(incomingOrder)
            : MatchIncomingSellOrder(incomingOrder);

        RemoveFilledOrders();

        return trades;
    }

    private List<Trade> MatchIncomingBuyOrder(Order incomingOrder)
    {
        List<Order> matches = FindMatchingSellOrders(incomingOrder);
        return ExecuteMatches(incomingOrder, matches);
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

    private List<Trade> ExecuteMatches(Order incomingOrder, List<Order> oppositeOrders)
    {
        var trades = new List<Trade>();

        foreach (Order oppositeOrder in oppositeOrders)
        {
            if (incomingOrder.Status == OrderStatus.Filled)
            {
                break;
            }

            int quantityToFill = GetQuantityToFill(incomingOrder, oppositeOrder);

            Trade trade = incomingOrder.Side == OrderSide.Buy
                ? CreateTrade(incomingOrder, oppositeOrder, quantityToFill)
                : CreateTrade(oppositeOrder, incomingOrder, quantityToFill);

            trades.Add(trade);

            incomingOrder.Fill(quantityToFill);
            oppositeOrder.Fill(quantityToFill);
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

    private static int GetQuantityToFill(Order incomingOrder, Order oppositeOrder)
    {
        return Math.Min(
            incomingOrder.Quantity - incomingOrder.FilledQuantity,
            oppositeOrder.Quantity - oppositeOrder.FilledQuantity
        );
    }

    private void RemoveFilledOrders()
    {
        _buyOrders.RemoveAll(o => o.Status == OrderStatus.Filled || o.Status == OrderStatus.Cancelled);
        _sellOrders.RemoveAll(o => o.Status == OrderStatus.Filled || o.Status == OrderStatus.Cancelled);
    }
}
