using Stockhub.Common.Domain.DomainEvents;

namespace Stockhub.Modules.Orders.Domain.Products;

public sealed record ProductUpdatedDomainEvent(Guid ProductId) : DomainEvent;
