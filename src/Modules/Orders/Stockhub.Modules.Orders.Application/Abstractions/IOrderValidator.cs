using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Orders.Application.Features.Orders.PlaceOrder;

namespace Stockhub.Modules.Orders.Application.Abstractions;

public interface IOrderValidationService
{
    Task<Result> ValidateAsync(PlaceOrderCommand command, CancellationToken cancellationToken);
}
