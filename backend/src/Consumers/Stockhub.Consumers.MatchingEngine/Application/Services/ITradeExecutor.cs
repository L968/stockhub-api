using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Common.Domain.Results;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;

namespace Stockhub.Consumers.MatchingEngine.Application.Services;

internal interface ITradeExecutor
{
    Task<Result<Trade>> ExecuteAsync(TradeProposal proposal, CancellationToken cancellationToken);
}
