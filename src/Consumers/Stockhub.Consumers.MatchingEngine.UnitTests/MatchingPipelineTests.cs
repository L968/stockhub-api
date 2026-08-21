using Microsoft.Extensions.Logging;
using Moq;
using Stockhub.Common.Domain.Results;
using Stockhub.Consumers.MatchingEngine.Application.Services;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

namespace Stockhub.Consumers.MatchingEngine.UnitTests;

public sealed class MatchingPipelineTests
{
    [Fact]
    public async Task ProcessOrder_DoesNotSettleWhenBookHasNoCounterparty()
    {
        var tradeExecutor = new Mock<ITradeExecutor>();
        var books = new OrderBookRepository();
        var service = new MatchingEngineService(books, tradeExecutor.Object, Mock.Of<ILogger<MatchingEngineService>>());

        IReadOnlyList<Trade> trades = await service.ProcessOrderAsync(
            "orders-0", CreateOrder(OrderSide.Buy), CancellationToken.None);

        Assert.Empty(trades);
        tradeExecutor.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessOrder_SettlesCrossingOrdersDirectlyFromMemory()
    {
        var tradeExecutor = new Mock<ITradeExecutor>();
        var books = new OrderBookRepository();
        var service = new MatchingEngineService(books, tradeExecutor.Object, Mock.Of<ILogger<MatchingEngineService>>());
        var stockId = Guid.NewGuid();
        Order sell = CreateOrder(OrderSide.Sell, stockId);
        Order buy = CreateOrder(OrderSide.Buy, stockId);

        tradeExecutor
            .Setup(executor => executor.ExecuteAsync(It.IsAny<TradeProposal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradeProposal proposal, CancellationToken _) =>
            {
                books.UpdateOrderFilledQuantity(proposal.BuyOrderId, proposal.Quantity);
                books.UpdateOrderFilledQuantity(proposal.SellOrderId, proposal.Quantity);
                return Result.Success(new Trade(
                    proposal.StockId, buy.UserId, sell.UserId, proposal.BuyOrderId,
                    proposal.SellOrderId, proposal.Price, proposal.Quantity));
            });

        await service.ProcessOrderAsync("orders-0", sell, CancellationToken.None);
        IReadOnlyList<Trade> trades = await service.ProcessOrderAsync("orders-0", buy, CancellationToken.None);

        Assert.Single(trades);
        tradeExecutor.Verify(
            executor => executor.ExecuteAsync(It.IsAny<TradeProposal>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessOrder_IgnoresRedeliveredOrder()
    {
        var tradeExecutor = new Mock<ITradeExecutor>();
        var books = new OrderBookRepository();
        var service = new MatchingEngineService(books, tradeExecutor.Object, Mock.Of<ILogger<MatchingEngineService>>());
        Order order = CreateOrder(OrderSide.Buy);

        await service.ProcessOrderAsync("orders-0", order, CancellationToken.None);
        await service.ProcessOrderAsync("orders-0", order, CancellationToken.None);

        Assert.Single(books.GetOrderBookSnapshot(order.StockId).Orders);
        tradeExecutor.VerifyNoOtherCalls();
    }

    private static Order CreateOrder(OrderSide side, Guid? stockId = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        StockId = stockId ?? Guid.NewGuid(),
        Side = side,
        Price = 100,
        Quantity = 10,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };
}
