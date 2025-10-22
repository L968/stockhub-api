using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Orders.Application.Abstractions;
using Stockhub.Modules.Orders.Application.Features.Orders.PlaceOrder;
using Stockhub.Modules.Orders.Domain.Orders;
using Stockhub.Modules.Orders.Domain.PortfolioEntries;
using Stockhub.Modules.Orders.Domain.Stocks;
using Stockhub.Modules.Orders.Domain.Users;

namespace Stockhub.Modules.Orders.Application.OrderValidators;

public sealed class SellOrderValidator(IOrdersDbContext dbContext) : ISideOrderValidator
{
    public OrderSide Side => OrderSide.Sell;

    public async Task<Result> ValidateAsync(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        User? user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        bool exists = await dbContext.Stocks
            .AsNoTracking()
            .AnyAsync(s => s.Id == command.StockId, cancellationToken);

        if (!exists)
        {
            return Result.Failure(StockErrors.NotFound(command.StockId));
        }

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
