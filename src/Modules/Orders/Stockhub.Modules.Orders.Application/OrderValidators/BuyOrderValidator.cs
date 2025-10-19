using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Orders.Application.Abstractions;
using Stockhub.Modules.Orders.Application.Orders.PlaceOrder;
using Stockhub.Modules.Orders.Domain.Orders;
using Stockhub.Modules.Orders.Domain.Users;

namespace Stockhub.Modules.Orders.Application.OrderValidators;

public sealed class BuyOrderValidator(IOrdersDbContext dbContext) : ISideOrderValidator
{
    public OrderSide Side => OrderSide.Buy;

    public async Task<Result> ValidateAsync(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        User? user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        decimal totalCost = command.Price * command.Quantity;

        if (user.CurrentBalance < totalCost)
        {
            return Result.Failure(OrderErrors.InsufficientBalance);
        }

        return Result.Success();
    }
}
