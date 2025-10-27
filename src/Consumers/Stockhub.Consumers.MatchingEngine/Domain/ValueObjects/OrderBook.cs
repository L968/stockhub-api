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

    public void Remove(Guid orderId)
    {
        _buyOrders.RemoveAll(o => o.Id == orderId);
        _sellOrders.RemoveAll(o => o.Id == orderId);
    }

    public List<TradeProposal> ProposeTrades(Order incomingOrder)
    {
        List<Order> orderMatches = incomingOrder.Side == OrderSide.Buy
            ? FindMatchingSellOrders(incomingOrder)
            : FindMatchingBuyOrders(incomingOrder);

        var proposals = new List<TradeProposal>();
        int remainingQuantity = incomingOrder.Quantity - incomingOrder.FilledQuantity;

        foreach (Order oppositeOrder in orderMatches)
        {
            if (remainingQuantity <= 0)
            {
                break;
            }

            int fillQuantity = CalculateFillQuantity(oppositeOrder, remainingQuantity);

            var proposal = new TradeProposal(
                StockId: StockId,
                BuyOrderId: incomingOrder.Side == OrderSide.Buy ? incomingOrder.Id : oppositeOrder.Id,
                SellOrderId: incomingOrder.Side == OrderSide.Sell ? incomingOrder.Id : oppositeOrder.Id,
                Price: incomingOrder.Side == OrderSide.Buy ? oppositeOrder.Price : incomingOrder.Price,
                Quantity: fillQuantity
            );

            proposals.Add(proposal);
            remainingQuantity -= fillQuantity;
        }

        return proposals;
    }

    public void CommitTrade(Trade trade)
    {
        Order? buyOrder = _buyOrders.FirstOrDefault(o => o.Id == trade.BuyOrderId);
        Order? sellOrder = _sellOrders.FirstOrDefault(o => o.Id == trade.SellOrderId);

        buyOrder?.Fill(trade.Quantity);
        sellOrder?.Fill(trade.Quantity);

        RemoveFilledOrders();
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

    private static int CalculateFillQuantity(Order oppositeOrder, int remainingQuantity)
    {
        return Math.Min(
            remainingQuantity,
            oppositeOrder.Quantity - oppositeOrder.FilledQuantity
        );
    }

    private void RemoveFilledOrders()
    {
        _buyOrders.RemoveAll(o => o.Status == OrderStatus.Filled || o.Status == OrderStatus.Cancelled);
        _sellOrders.RemoveAll(o => o.Status == OrderStatus.Filled || o.Status == OrderStatus.Cancelled);
    }
}
