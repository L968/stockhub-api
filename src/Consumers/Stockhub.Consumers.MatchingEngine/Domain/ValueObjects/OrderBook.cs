using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;

namespace Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;

internal sealed class OrderBook(Guid stockId, List<Order> orders)
{
    public int Count => orders.Count;
    public IReadOnlyList<Order> Orders => orders;

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

            int fillQuantity = Math.Min(remainingQuantity, oppositeOrder.Quantity - oppositeOrder.FilledQuantity);

            var proposal = new TradeProposal(
                stockId,
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

    private List<Order> FindMatchingBuyOrders(Order sellOrder)
    {
        return orders
            .Where(o => o.Side == OrderSide.Buy
                        && !o.IsCancelled
                        && o.Price >= sellOrder.Price
                        && o.FilledQuantity < o.Quantity)
            .OrderByDescending(o => o.Price)
            .ThenBy(o => o.CreatedAtUtc)
            .ToList();
    }

    private List<Order> FindMatchingSellOrders(Order buyOrder)
    {
        return orders
            .Where(o => o.Side == OrderSide.Sell
                        && !o.IsCancelled
                        && o.Price <= buyOrder.Price
                        && o.FilledQuantity < o.Quantity)
            .OrderBy(o => o.Price)
            .ThenBy(o => o.CreatedAtUtc)
            .ToList();
    }
}
