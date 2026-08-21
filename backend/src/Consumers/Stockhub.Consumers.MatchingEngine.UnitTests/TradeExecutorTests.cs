using Microsoft.Extensions.Logging;
using Moq;
using Stockhub.Common.Domain.Results;
using Stockhub.Consumers.MatchingEngine.Application.Services;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.UnitTests;

public sealed class TradeExecutorTests
{
    [Fact]
    public async Task Execute_UpdatesLocalBookAfterAtomicSettlement()
    {
        var settlement = new Mock<ITradeSettlementRepository>();
        var books = new Mock<IOrderBookRepository>();
        var proposal = new TradeProposal(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100, 5);
        var trade = new Trade(proposal.StockId, Guid.NewGuid(), Guid.NewGuid(),
            proposal.BuyOrderId, proposal.SellOrderId, proposal.Price, proposal.Quantity);
        settlement.Setup(repository => repository.ExecuteAsync(proposal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TradeSettlementResult.Executed(trade, 8, 10));
        var executor = new TradeExecutor(settlement.Object, books.Object, Mock.Of<ILogger<TradeExecutor>>());

        Result<Trade> result = await executor.ExecuteAsync(proposal, CancellationToken.None);

        Assert.True(result.IsSuccess);
        books.Verify(repository => repository.UpdateOrderFilledQuantity(proposal.BuyOrderId, 8), Times.Once);
        books.Verify(repository => repository.UpdateOrderFilledQuantity(proposal.SellOrderId, 10), Times.Once);
    }

    [Fact]
    public async Task Execute_RemovesRejectedOrderFromLocalBook()
    {
        var settlement = new Mock<ITradeSettlementRepository>();
        var books = new Mock<IOrderBookRepository>();
        var proposal = new TradeProposal(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100, 5);
        settlement.Setup(repository => repository.ExecuteAsync(proposal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TradeSettlementResult.Rejected(proposal.BuyOrderId));
        var executor = new TradeExecutor(settlement.Object, books.Object, Mock.Of<ILogger<TradeExecutor>>());

        Result<Trade> result = await executor.ExecuteAsync(proposal, CancellationToken.None);

        Assert.True(result.IsFailure);
        books.Verify(repository => repository.CancelOrder(proposal.BuyOrderId), Times.Once);
    }
}
