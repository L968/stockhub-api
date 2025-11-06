using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;

namespace Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;

internal sealed class OrderBook(Guid stockId, List<Order> orders)
{
    public int Count => orders.Count;
    public IReadOnlyList<Order> Orders => orders;

    public List<TradeProposal> ProposeAllPossibleTrades()
    {
        var proposals = new List<TradeProposal>();

        var buyOrders = orders
            .Where(o => o.Side == OrderSide.Buy && !o.IsCancelled && o.FilledQuantity < o.Quantity)
            .OrderByDescending(o => o.Price)
            .ThenBy(o => o.CreatedAtUtc)
            .Select(o => new { Order = o, Remaining = o.Quantity - o.FilledQuantity })
            .ToList();

        var sellOrders = orders
            .Where(o => o.Side == OrderSide.Sell && !o.IsCancelled && o.FilledQuantity < o.Quantity)
            .OrderBy(o => o.Price)
            .ThenBy(o => o.CreatedAtUtc)
            .Select(o => new { Order = o, Remaining = o.Quantity - o.FilledQuantity })
            .ToList();

        int i = 0, j = 0;

        while (i < buyOrders.Count && j < sellOrders.Count)
        {
            var buyEntry = buyOrders[i];
            var sellEntry = sellOrders[j];

            if (buyEntry.Order.Price < sellEntry.Order.Price)
            {
                i++;
                continue;
            }

            int fillQuantity = Math.Min(buyEntry.Remaining, sellEntry.Remaining);

            var proposal = new TradeProposal(
                stockId,
                BuyOrderId: buyEntry.Order.Id,
                SellOrderId: sellEntry.Order.Id,
                Price: sellEntry.Order.Price,
                Quantity: fillQuantity
            );

            proposals.Add(proposal);

            buyEntry = new { buyEntry.Order, Remaining = buyEntry.Remaining - fillQuantity };
            sellEntry = new { sellEntry.Order, Remaining = sellEntry.Remaining - fillQuantity };

            if (buyEntry.Remaining == 0)
            {
                i++;
            }
            else
            {
                buyOrders[i] = buyEntry;
            }

            if (sellEntry.Remaining == 0)
            {
                j++;
            }
            else
            {
                sellOrders[j] = sellEntry;
            }
        }

        return proposals;
    }
}
