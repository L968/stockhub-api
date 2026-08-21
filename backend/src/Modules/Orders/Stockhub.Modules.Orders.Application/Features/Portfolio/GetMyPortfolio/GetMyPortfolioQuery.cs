using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Orders.Application.Features.Portfolio.GetMyPortfolio;

public sealed record GetMyPortfolioQuery(Guid UserId) : IRequest<Result<GetMyPortfolioResponse>>;
