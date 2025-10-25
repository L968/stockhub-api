using Stockhub.Consumers.Entities;
using Stockhub.Consumers.Events.OrderPlaced;

namespace Stockhub.Consumers.Matching;

internal sealed class OrderBook(Guid stockId)
{
    private readonly List<OrderPlacedEvent> _buyOrders = [];
    private readonly List<OrderPlacedEvent> _sellOrders = [];

    public bool IsEmpty => !_buyOrders.Any() && !_sellOrders.Any();

    public Guid StockId { get; } = stockId;

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
        var trades = new List<Trade>();

        if (incoming.Side == OrderSide.Buy)
        {
            var matches = _sellOrders
                .Where(s => s.Price <= incoming.Price)
                .OrderBy(s => s.Price)
                .ToList();

            foreach (OrderPlacedEvent? sell in matches)
            {
                if (incoming.Quantity <= 0)
                {
                    break;
                }

                int quantity = Math.Min(incoming.Quantity, sell.Quantity);

                trades.Add(new Trade(
                    stockId: StockId,
                    buyerId: incoming.UserId,
                    sellerId: sell.UserId,
                    buyOrderId: incoming.OrderId,
                    sellOrderId: sell.OrderId,
                    price: sell.Price,
                    quantity: quantity
                ));

                incoming.Quantity -= quantity;
                sell.Quantity -= quantity;

                if (sell.Quantity > 0)
                {
                    sell.Status = OrderStatus.PartiallyFilled;
                }
            }

            if (incoming.Quantity > 0 && incoming.Quantity < incoming.Quantity + trades.Sum(t => t.Quantity))
            {
                incoming.Status = OrderStatus.PartiallyFilled;
            }

            _sellOrders.RemoveAll(s => s.Quantity == 0);
        }
        else
        {
            var matches = _buyOrders
                .Where(b => b.Price >= incoming.Price)
                .OrderByDescending(b => b.Price)
                .ToList();

            foreach (OrderPlacedEvent? buy in matches)
            {
                if (incoming.Quantity <= 0)
                {
                    break;
                }

                int quantity = Math.Min(incoming.Quantity, buy.Quantity);

                trades.Add(new Trade(
                    stockId: StockId,
                    buyerId: buy.UserId,
                    sellerId: incoming.UserId,
                    buyOrderId: buy.OrderId,
                    sellOrderId: incoming.OrderId,
                    price: buy.Price,
                    quantity: quantity
                ));

                incoming.Quantity -= quantity;
                buy.Quantity -= quantity;

                if (buy.Quantity > 0)
                {
                    buy.Status = OrderStatus.PartiallyFilled;
                }
            }

            if (incoming.Quantity > 0 && incoming.Quantity < incoming.Quantity + trades.Sum(t => t.Quantity))
            {
                incoming.Status = OrderStatus.PartiallyFilled;
            }

            _buyOrders.RemoveAll(b => b.Quantity == 0);
        }

        return trades;
    }


    public void RemoveFilledOrders()
    {
        _buyOrders.RemoveAll(o => o.Status == OrderStatus.Filled);
        _sellOrders.RemoveAll(o => o.Status == OrderStatus.Filled);
    }
}
