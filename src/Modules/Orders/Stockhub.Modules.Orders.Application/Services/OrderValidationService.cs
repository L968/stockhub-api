using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Orders.Application.Abstractions;
using Stockhub.Modules.Orders.Application.Orders.PlaceOrder;
using Stockhub.Modules.Orders.Domain.Orders;

namespace Stockhub.Modules.Orders.Application.Services;

public sealed class OrderValidationService(IEnumerable<ISideOrderValidator> sideValidators) : IOrderValidationService
{
    public async Task<Result> ValidateAsync(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        ISideOrderValidator? validator = sideValidators.FirstOrDefault(v => v.Side == command.Side);

        if (validator is null)
        {
            return Result.Failure(OrderErrors.ValidatorNotFound);
        }

        return await validator.ValidateAsync(command, cancellationToken);
    }
}
