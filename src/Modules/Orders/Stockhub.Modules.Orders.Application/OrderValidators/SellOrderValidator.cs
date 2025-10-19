using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Orders.Application.Abstractions;
using Stockhub.Modules.Orders.Application.Orders.PlaceOrder;
using Stockhub.Modules.Orders.Domain.Orders;
using Stockhub.Modules.Orders.Domain.PortfolioEntries;

namespace Stockhub.Modules.Orders.Application.OrderValidators;

public sealed class SellOrderValidator(IOrdersDbContext dbContext) : ISideOrderValidator
{
    public OrderSide Side => OrderSide.Sell;

    public async Task<Result> ValidateAsync(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        PortfolioEntry? portfolio = await dbContext.PortfolioEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.UserId == command.UserId && p.StockId == command.StockId,
                cancellationToken
            );

        int availableQuantity = portfolio?.Quantity ?? 0;

        if (availableQuantity < command.Quantity)
        {
            return Result.Failure(OrderErrors.InsufficientPortfolio);
        }

        return Result.Success();
    }
}
