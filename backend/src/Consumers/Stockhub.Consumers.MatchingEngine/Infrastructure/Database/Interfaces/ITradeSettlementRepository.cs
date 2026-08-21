using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

internal interface ITradeSettlementRepository
{
    Task<TradeSettlementResult> ExecuteAsync(TradeProposal proposal, CancellationToken cancellationToken);
}
