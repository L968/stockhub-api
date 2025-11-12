using Microsoft.Extensions.Logging;
using Moq;
using Stockhub.Common.Domain.Results;
using Stockhub.Consumers.MatchingEngine.Application.Cache;
using Stockhub.Consumers.MatchingEngine.Application.Queues;
using Stockhub.Consumers.MatchingEngine.Application.Services;
using Stockhub.Consumers.MatchingEngine.Application.Validators;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.UnitTests;

public class MatchingEngineServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITradeExecutor> _tradeExecutorMock;
    private readonly Mock<IDirtyQueue> _dirtyQueueMock;
    private readonly Mock<IProcessedOrderCache> _processedOrderCacheMock;
    private readonly Mock<ILogger<MatchingEngineService>> _loggerMock;
    private readonly OrderBookRepository _orderBookRepository;
    private readonly MatchingEngineService _service;

    public MatchingEngineServiceTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _tradeExecutorMock = new Mock<ITradeExecutor>();
        _dirtyQueueMock = new Mock<IDirtyQueue>();
        _processedOrderCacheMock = new Mock<IProcessedOrderCache>();
        _orderBookRepository = new OrderBookRepository();
        _loggerMock = new Mock<ILogger<MatchingEngineService>>();

        _orderRepositoryMock
            .Setup(x => x.UpdateFilledQuantityAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _orderRepositoryMock
            .Setup(x => x.AddTradeAsync(It.IsAny<Trade>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(x => x.UpdateBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new MatchingEngineService(
            _orderBookRepository,
            _orderRepositoryMock.Object,
            _tradeExecutorMock.Object,
            _dirtyQueueMock.Object,
            new OrderValidator(_userRepositoryMock.Object),
            _processedOrderCacheMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task EnqueueOrderAsync_Should_Add_Order_And_Enqueue_Stock_When_Valid()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Order order = CreateOrder(userId: userId, stockId: stockId, price: 100, quantity: 5);

        _userRepositoryMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, CurrentBalance = 1000 });

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(userId, order.Price * order.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.EnqueueOrderAsync(order, CancellationToken.None);

        // Assert
        Assert.True(_orderBookRepository.ContainsOrder(order.Id));
        _dirtyQueueMock.Verify(x => x.Enqueue(stockId), Times.Once);
    }

    [Fact]
    public async Task EnqueueOrderAsync_Should_Cancel_Order_When_Validation_Fails()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Order order = CreateOrder(userId: userId, stockId: stockId, price: 0);

        _userRepositoryMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, CurrentBalance = 1000 });

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(userId, It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.EnqueueOrderAsync(order, CancellationToken.None);

        // Assert
        _orderRepositoryMock.Verify(x => x.CancelAsync(order.Id, It.IsAny<CancellationToken>()), Times.Once);
        _dirtyQueueMock.Verify(x => x.Enqueue(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task EnqueueOrderAsync_Should_Not_Add_Duplicate_Order()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Order order = CreateOrder(userId: userId, stockId: stockId, price: 100, quantity: 5);

        _userRepositoryMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, CurrentBalance = 1000 });

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(userId, It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _orderBookRepository.AddOrder(order);

        // Act
        await _service.EnqueueOrderAsync(order, CancellationToken.None);

        // Assert
        _dirtyQueueMock.Verify(x => x.Enqueue(It.IsAny<Guid>()), Times.Never);
        Assert.Equal(1, _orderBookRepository.GetOrderBookSnapshot(stockId).Count);
    }

    [Fact]
    public async Task InitializeAsync_Should_Build_OrderBook_And_Enqueue_Distinct_Stocks()
    {
        // Arrange
        var stockA = Guid.NewGuid();
        var stockB = Guid.NewGuid();

        Order[] openOrders =
        [
            CreateOrder(stockId: stockA),
            CreateOrder(stockId: stockA),
            CreateOrder(stockId: stockB)
        ];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(openOrders);

        // Act
        await _service.InitializeAsync(CancellationToken.None);

        // Assert
        _dirtyQueueMock.Verify(x => x.Enqueue(stockA), Times.Once);
        _dirtyQueueMock.Verify(x => x.Enqueue(stockB), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_Should_Build_OrderBooks_And_Enqueue_All_Stocks()
    {
        // Arrange
        var stockA = Guid.NewGuid();
        var stockB = Guid.NewGuid();
        Order[] orders =
        [
            CreateOrder(stockId: stockA),
            CreateOrder(stockId: stockA),
            CreateOrder(stockId: stockB)
        ];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        await _service.InitializeAsync(CancellationToken.None);

        // Assert
        _dirtyQueueMock.Verify(x => x.Enqueue(stockA), Times.Once);
        _dirtyQueueMock.Verify(x => x.Enqueue(stockB), Times.Once);
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Continue_After_First_Iteration_When_Proposals_Still_Available()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId1 = Guid.NewGuid();
        var sellerId2 = Guid.NewGuid();
        Order buyOrder = CreateOrder(stockId: stockId, userId: buyerId, side: OrderSide.Buy, price: 100, quantity: 20);
        Order sellOrder1 = CreateOrder(stockId: stockId, userId: sellerId1, side: OrderSide.Sell, price: 100, quantity: 10);
        Order sellOrder2 = CreateOrder(stockId: stockId, userId: sellerId2, side: OrderSide.Sell, price: 100, quantity: 10);
        _orderBookRepository.AddOrder(buyOrder);
        _orderBookRepository.AddOrder(sellOrder1);
        _orderBookRepository.AddOrder(sellOrder2);

        var trade1 = new Trade(stockId, buyerId, sellerId1, buyOrder.Id, sellOrder1.Id, 100, 10);
        var trade2 = new Trade(stockId, buyerId, sellerId2, buyOrder.Id, sellOrder2.Id, 100, 10);
        int callCount = 0;
        _tradeExecutorMock
            .Setup(x => x.ExecuteAsync(It.IsAny<TradeProposal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradeProposal proposal, CancellationToken _) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    buyOrder.Fill(proposal.Quantity);
                    sellOrder1.Fill(proposal.Quantity);
                    _orderBookRepository.UpdateOrderFilledQuantity(buyOrder.Id, buyOrder.FilledQuantity);
                    _orderBookRepository.UpdateOrderFilledQuantity(sellOrder1.Id, sellOrder1.FilledQuantity);
                    return Result<Trade>.Success(trade1);
                }
                else
                {
                    buyOrder.Fill(proposal.Quantity);
                    sellOrder2.Fill(proposal.Quantity);
                    _orderBookRepository.UpdateOrderFilledQuantity(buyOrder.Id, buyOrder.FilledQuantity);
                    _orderBookRepository.UpdateOrderFilledQuantity(sellOrder2.Id, sellOrder2.FilledQuantity);
                    return Result<Trade>.Success(trade2);
                }
            });

        // Act
        List<Trade> result = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Id == trade1.Id);
        Assert.Contains(result, t => t.Id == trade2.Id);
        _dirtyQueueMock.Verify(x => x.MarkProcessed(stockId), Times.Once);
        _tradeExecutorMock.Verify(x => x.ExecuteAsync(It.IsAny<TradeProposal>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Execute_Multiple_Trades_And_Return_All_Results()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId1 = Guid.NewGuid();
        var buyerId2 = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        Order buyOrder1 = CreateOrder(stockId: stockId, userId: buyerId1, side: OrderSide.Buy, price: 100, quantity: 5);
        Order buyOrder2 = CreateOrder(stockId: stockId, userId: buyerId2, side: OrderSide.Buy, price: 100, quantity: 5);
        Order sellOrder = CreateOrder(stockId: stockId, userId: sellerId, side: OrderSide.Sell, price: 100, quantity: 10);
        _orderBookRepository.AddOrder(buyOrder1);
        _orderBookRepository.AddOrder(buyOrder2);
        _orderBookRepository.AddOrder(sellOrder);

        var trade1 = new Trade(stockId, buyerId1, sellerId, buyOrder1.Id, sellOrder.Id, 100, 5);
        var trade2 = new Trade(stockId, buyerId2, sellerId, buyOrder2.Id, sellOrder.Id, 100, 5);
        int callCount = 0;
        _tradeExecutorMock
            .Setup(x => x.ExecuteAsync(It.IsAny<TradeProposal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradeProposal proposal, CancellationToken _) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    buyOrder1.Fill(proposal.Quantity);
                    sellOrder.Fill(proposal.Quantity);
                    _orderBookRepository.UpdateOrderFilledQuantity(buyOrder1.Id, buyOrder1.FilledQuantity);
                    _orderBookRepository.UpdateOrderFilledQuantity(sellOrder.Id, sellOrder.FilledQuantity);
                    return Result<Trade>.Success(trade1);
                }
                else
                {
                    buyOrder2.Fill(proposal.Quantity);
                    sellOrder.Fill(proposal.Quantity);
                    _orderBookRepository.UpdateOrderFilledQuantity(buyOrder2.Id, buyOrder2.FilledQuantity);
                    _orderBookRepository.UpdateOrderFilledQuantity(sellOrder.Id, sellOrder.FilledQuantity);
                    return Result<Trade>.Success(trade2);
                }
            });

        // Act
        List<Trade> result = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Id == trade1.Id);
        Assert.Contains(result, t => t.Id == trade2.Id);
        _dirtyQueueMock.Verify(x => x.MarkProcessed(stockId), Times.Once);
        _tradeExecutorMock.Verify(x => x.ExecuteAsync(It.IsAny<TradeProposal>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Execute_Single_Trade_And_Return_Result()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        Order buyOrder = CreateOrder(stockId: stockId, userId: buyerId, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder = CreateOrder(stockId: stockId, userId: sellerId, side: OrderSide.Sell, price: 100, quantity: 10);
        _orderBookRepository.AddOrder(buyOrder);
        _orderBookRepository.AddOrder(sellOrder);

        var trade = new Trade(stockId, buyerId, sellerId, buyOrder.Id, sellOrder.Id, 100, 10);
        _tradeExecutorMock
            .Setup(x => x.ExecuteAsync(It.IsAny<TradeProposal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Trade>.Success(trade))
            .Callback<TradeProposal, CancellationToken>((proposal, _) =>
            {
                buyOrder.Fill(proposal.Quantity);
                sellOrder.Fill(proposal.Quantity);
                _orderBookRepository.UpdateOrderFilledQuantity(buyOrder.Id, buyOrder.FilledQuantity);
                _orderBookRepository.UpdateOrderFilledQuantity(sellOrder.Id, sellOrder.FilledQuantity);
            });

        // Act
        List<Trade> result = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(trade.Id, result[0].Id);
        _dirtyQueueMock.Verify(x => x.MarkProcessed(stockId), Times.Once);
        _tradeExecutorMock.Verify(x => x.ExecuteAsync(It.IsAny<TradeProposal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Handle_Partial_Fills_Correctly()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        Order buyOrder = CreateOrder(stockId: stockId, userId: buyerId, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder = CreateOrder(stockId: stockId, userId: sellerId, side: OrderSide.Sell, price: 100, quantity: 5);
        _orderBookRepository.AddOrder(buyOrder);
        _orderBookRepository.AddOrder(sellOrder);

        var trade = new Trade(stockId, buyerId, sellerId, buyOrder.Id, sellOrder.Id, 100, 5);
        _tradeExecutorMock
            .Setup(x => x.ExecuteAsync(It.IsAny<TradeProposal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Trade>.Success(trade))
            .Callback<TradeProposal, CancellationToken>((proposal, _) =>
            {
                buyOrder.Fill(proposal.Quantity);
                sellOrder.Fill(proposal.Quantity);
                _orderBookRepository.UpdateOrderFilledQuantity(buyOrder.Id, buyOrder.FilledQuantity);
                _orderBookRepository.UpdateOrderFilledQuantity(sellOrder.Id, sellOrder.FilledQuantity);
            });

        // Act
        List<Trade> result = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(trade.Id, result[0].Id);
        _dirtyQueueMock.Verify(x => x.MarkProcessed(stockId), Times.Once);
        _tradeExecutorMock.Verify(x => x.ExecuteAsync(It.IsAny<TradeProposal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Process_Multiple_Iterations_When_New_Proposals_Are_Generated()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId1 = Guid.NewGuid();
        var buyerId2 = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        Order buyOrder1 = CreateOrder(stockId: stockId, userId: buyerId1, side: OrderSide.Buy, price: 100, quantity: 10);
        Order buyOrder2 = CreateOrder(stockId: stockId, userId: buyerId2, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder1 = CreateOrder(stockId: stockId, userId: sellerId, side: OrderSide.Sell, price: 100, quantity: 5);
        Order sellOrder2 = CreateOrder(stockId: stockId, userId: sellerId, side: OrderSide.Sell, price: 100, quantity: 5);
        _orderBookRepository.AddOrder(buyOrder1);
        _orderBookRepository.AddOrder(buyOrder2);
        _orderBookRepository.AddOrder(sellOrder1);
        _orderBookRepository.AddOrder(sellOrder2);

        var trade1 = new Trade(stockId, buyerId1, sellerId, buyOrder1.Id, sellOrder1.Id, 100, 5);
        var trade2 = new Trade(stockId, buyerId1, sellerId, buyOrder1.Id, sellOrder2.Id, 100, 5);
        int callCount = 0;
        _tradeExecutorMock
            .Setup(x => x.ExecuteAsync(It.IsAny<TradeProposal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradeProposal proposal, CancellationToken _) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    buyOrder1.Fill(proposal.Quantity);
                    sellOrder1.Fill(proposal.Quantity);
                    _orderBookRepository.UpdateOrderFilledQuantity(buyOrder1.Id, buyOrder1.FilledQuantity);
                    _orderBookRepository.UpdateOrderFilledQuantity(sellOrder1.Id, sellOrder1.FilledQuantity);
                    return Result<Trade>.Success(trade1);
                }
                else
                {
                    buyOrder1.Fill(proposal.Quantity);
                    sellOrder2.Fill(proposal.Quantity);
                    _orderBookRepository.UpdateOrderFilledQuantity(buyOrder1.Id, buyOrder1.FilledQuantity);
                    _orderBookRepository.UpdateOrderFilledQuantity(sellOrder2.Id, sellOrder2.FilledQuantity);
                    return Result<Trade>.Success(trade2);
                }
            });

        // Act
        List<Trade> result = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Id == trade1.Id);
        Assert.Contains(result, t => t.Id == trade2.Id);
        _dirtyQueueMock.Verify(x => x.MarkProcessed(stockId), Times.Once);
        _tradeExecutorMock.Verify(x => x.ExecuteAsync(It.IsAny<TradeProposal>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Return_Empty_List_When_No_Proposals_Available()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        Order buyOrder = CreateOrder(stockId: stockId, side: OrderSide.Buy, price: 90, quantity: 10);
        Order sellOrder = CreateOrder(stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10);
        _orderBookRepository.AddOrder(buyOrder);
        _orderBookRepository.AddOrder(sellOrder);

        // Act
        List<Trade> result = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        Assert.Empty(result);
        _dirtyQueueMock.Verify(x => x.MarkProcessed(stockId), Times.Once);
        _tradeExecutorMock.Verify(x => x.ExecuteAsync(It.IsAny<TradeProposal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Return_Empty_List_When_OrderBook_Is_Empty()
    {
        // Arrange
        var stockId = Guid.NewGuid();

        // Act
        List<Trade> result = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        Assert.Empty(result);
        _dirtyQueueMock.Verify(x => x.MarkProcessed(stockId), Times.Once);
        _tradeExecutorMock.Verify(x => x.ExecuteAsync(It.IsAny<TradeProposal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Throw_Exception_When_Safety_Limit_Exceeded()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        Order buyOrder = CreateOrder(stockId: stockId, userId: buyerId, side: OrderSide.Buy, price: 100, quantity: 10, filledQuantity: 0);
        Order sellOrder = CreateOrder(stockId: stockId, userId: sellerId, side: OrderSide.Sell, price: 100, quantity: 10, filledQuantity: 0);
        _orderBookRepository.AddOrder(buyOrder);
        _orderBookRepository.AddOrder(sellOrder);

        var trade = new Trade(stockId, buyerId, sellerId, buyOrder.Id, sellOrder.Id, 100, 1);
        _tradeExecutorMock
            .Setup(x => x.ExecuteAsync(It.IsAny<TradeProposal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Trade>.Success(trade));

        _orderRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => id == buyOrder.Id ? buyOrder : sellOrder);

        _orderRepositoryMock
            .Setup(x => x.UpdateFilledQuantityAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, int, CancellationToken>((id, filled, _) =>
            {
                if (id == buyOrder.Id)
                {
                    buyOrder.FilledQuantity = filled;
                    _orderBookRepository.UpdateOrderFilledQuantity(buyOrder.Id, filled);
                }
                else if (id == sellOrder.Id)
                {
                    sellOrder.FilledQuantity = filled;
                    _orderBookRepository.UpdateOrderFilledQuantity(sellOrder.Id, filled);
                }
            })
            .Returns(Task.CompletedTask);

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.MatchPendingOrdersAsync(stockId, CancellationToken.None)
        );
        Assert.Contains("Potential infinite loop detected", exception.Message);
        Assert.Contains(stockId.ToString(), exception.Message);
    }

    private static Order CreateOrder(
        Guid? orderId = null,
        Guid? userId = null,
        Guid? stockId = null,
        OrderSide side = OrderSide.Buy,
        decimal price = 100,
        int quantity = 10,
        int filledQuantity = 0,
        bool isCancelled = false)
    {
        DateTime now = DateTime.UtcNow;

        return new Order
        {
            Id = orderId ?? Guid.CreateVersion7(),
            UserId = userId ?? Guid.CreateVersion7(),
            StockId = stockId ?? Guid.CreateVersion7(),
            Side = side,
            Price = price,
            Quantity = quantity,
            FilledQuantity = filledQuantity,
            IsCancelled = isCancelled,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }
}
