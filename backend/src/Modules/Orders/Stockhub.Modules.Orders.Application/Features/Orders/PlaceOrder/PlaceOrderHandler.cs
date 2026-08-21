using Stockhub.Common.Domain.Results;
using Stockhub.Common.Messaging.Contracts.Orders;
using Stockhub.Modules.Orders.Application.Abstractions;
using Stockhub.Modules.Orders.Domain.Orders;
using ContractOrderSide = Stockhub.Common.Messaging.Contracts.Orders.OrderSide;

namespace Stockhub.Modules.Orders.Application.Features.Orders.PlaceOrder;

internal sealed class PlaceOrderHandler(
    IOrdersDbContext dbContext,
    IOrderValidationService validationService,
    IIntegrationEventPublisher eventPublisher,
    ILogger<PlaceOrderHandler> logger
) : IRequestHandler<PlaceOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        Result validationResult = await validationService.ValidateAsync(request, cancellationToken);

        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        var order = new Order(
            request.UserId,
            request.StockId,
            request.Side,
            request.Price,
            request.Quantity
        );

        await dbContext.Orders.AddAsync(order, cancellationToken);

        DateTime occurredAtUtc = DateTime.UtcNow;

        await eventPublisher.PublishAsync(
            new OrderPlaced(
                Guid.CreateVersion7(),
                order.Id,
                order.UserId,
                order.StockId,
                (ContractOrderSide)order.Side,
                order.Price,
                order.Quantity,
                occurredAtUtc,
                occurredAtUtc),
            cancellationToken
        );

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Created new order {@Order}", order);

        return Result.Success(order.Id);
    }
}
