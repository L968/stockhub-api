using Stockhub.Common.Domain.DomainEvents;

namespace Stockhub.Modules.Users.Domain.Products;

public sealed record ProductUpdatedDomainEvent(Guid ProductId) : DomainEvent;
