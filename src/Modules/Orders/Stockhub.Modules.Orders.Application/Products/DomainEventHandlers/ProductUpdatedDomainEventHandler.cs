using Stockhub.Common.Application.DomainEvent;
using Stockhub.Modules.Orders.Domain.Products;

namespace Stockhub.Modules.Orders.Application.Products.DomainEventHandlers;

internal sealed class ProductUpdatedDomainEventHandler : DomainEventHandler<ProductUpdatedDomainEvent>
{
    public override Task Handle(ProductUpdatedDomainEvent notification, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
