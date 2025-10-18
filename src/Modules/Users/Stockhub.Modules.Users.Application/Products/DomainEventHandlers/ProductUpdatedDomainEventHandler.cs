using Stockhub.Common.Application.DomainEvent;
using Stockhub.Modules.Users.Domain.Products;

namespace Stockhub.Modules.Users.Application.Products.DomainEventHandlers;

internal sealed class ProductUpdatedDomainEventHandler : DomainEventHandler<ProductUpdatedDomainEvent>
{
    public override Task Handle(ProductUpdatedDomainEvent notification, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
