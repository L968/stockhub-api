using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;

namespace Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;

internal sealed class OrderBook(Guid stockId, List<Order> orders)
{
    public int Count => orders.Count;
    public IReadOnlyList<Order> Orders => orders;

    public List<TradeProposal> ProposeAllPossibleTrades()
    {
        List<(Order Order, int Remaining)> buyOrders = PrepareBuyOrders();
        List<(Order Order, int Remaining)> sellOrders = PrepareSellOrders();

        return GenerateTradeProposals(buyOrders, sellOrders);
    }

    private List<(Order Order, int Remaining)> PrepareBuyOrders()
    {
        return orders
            .Where(o => o.Side == OrderSide.Buy && !o.IsCancelled && o.FilledQuantity < o.Quantity)
            .OrderByDescending(o => o.Price)
            .ThenBy(o => o.CreatedAtUtc)
            .Select(o => (o, o.Quantity - o.FilledQuantity))
            .ToList();
    }

    private List<(Order Order, int Remaining)> PrepareSellOrders()
    {
        return orders
            .Where(o => o.Side == OrderSide.Sell && !o.IsCancelled && o.FilledQuantity < o.Quantity)
            .OrderBy(o => o.Price)
            .ThenBy(o => o.CreatedAtUtc)
            .Select(o => (o, o.Quantity - o.FilledQuantity))
            .ToList();
    }

    private List<TradeProposal> GenerateTradeProposals(
        List<(Order Order, int Remaining)> buyOrders,
        List<(Order Order, int Remaining)> sellOrders)
    {
        var proposals = new List<TradeProposal>();
        int i = 0, j = 0;

        while (i < buyOrders.Count && j < sellOrders.Count)
        {
            (Order Order, int Remaining) buy = buyOrders[i];
            (Order Order, int Remaining) sell = sellOrders[j];

            if (buy.Order.Price < sell.Order.Price)
            {
                i++;
                continue;
            }

            int fillQuantity = Math.Min(buy.Remaining, sell.Remaining);

            proposals.Add(CreateProposal(buy.Order, sell.Order, fillQuantity));

            buy.Remaining -= fillQuantity;
            sell.Remaining -= fillQuantity;

            if (buy.Remaining == 0)
            {
                i++;
            }
            else
            {
                buyOrders[i] = buy;
            }

            if (sell.Remaining == 0)
            {
                j++;
            }
            else
            {
                sellOrders[j] = sell;
            }
        }

        return proposals;
    }

    private TradeProposal CreateProposal(Order buy, Order sell, int quantity)
    {
        return new TradeProposal(
            stockId,
            BuyOrderId: buy.Id,
            SellOrderId: sell.Id,
            Price: sell.Price,
            Quantity: quantity
        );
    }
}
