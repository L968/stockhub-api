using Stockhub.Common.Domain.DomainEvents;

namespace Stockhub.Modules.Stocks.Domain.Products;

public sealed record ProductUpdatedDomainEvent(Guid ProductId) : DomainEvent;
