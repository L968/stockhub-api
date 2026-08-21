using Dapper;
using Npgsql;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

internal sealed class TradeSettlementRepository(NpgsqlDataSource dataSource) : ITradeSettlementRepository
{
    public async Task<TradeSettlementResult> ExecuteAsync(TradeProposal proposal, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        var orders = (await connection.QueryAsync<Order>(new CommandDefinition(
            OrderLockSql, new { proposal.BuyOrderId, proposal.SellOrderId }, transaction,
            cancellationToken: cancellationToken))).ToList();
        Order? buy = orders.SingleOrDefault(order => order.Id == proposal.BuyOrderId);
        Order? sell = orders.SingleOrDefault(order => order.Id == proposal.SellOrderId);

        if (!IsExecutable(buy, sell, proposal))
        {
            Guid rejectedId = GetRejectedOrderId(buy, sell, proposal);
            await CancelAsync(connection, transaction, rejectedId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return TradeSettlementResult.Rejected(rejectedId);
        }

        var accounts = (await connection.QueryAsync<AccountBalance>(new CommandDefinition(
            AccountLockSql, new { BuyerId = buy!.UserId, SellerId = sell!.UserId }, transaction,
            cancellationToken: cancellationToken))).ToList();
        AccountBalance? buyer = accounts.SingleOrDefault(account => account.Id == buy.UserId);
        AccountBalance? seller = accounts.SingleOrDefault(account => account.Id == sell.UserId);
        PortfolioPosition? sellerPosition = await connection.QuerySingleOrDefaultAsync<PortfolioPosition>(
            new CommandDefinition(SellerPositionLockSql, new { sell.UserId, proposal.StockId }, transaction,
                cancellationToken: cancellationToken));

        int quantity = Math.Min(proposal.Quantity,
            Math.Min(buy.Quantity - buy.FilledQuantity, sell.Quantity - sell.FilledQuantity));
        decimal total = proposal.Price * quantity;

        if (buyer is null || buyer.CurrentBalance < total)
        {
            await CancelAsync(connection, transaction, buy.Id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return TradeSettlementResult.Rejected(buy.Id);
        }

        if (seller is null || sellerPosition is null || sellerPosition.Quantity < quantity)
        {
            await CancelAsync(connection, transaction, sell.Id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return TradeSettlementResult.Rejected(sell.Id);
        }

        int buyFilled = buy.FilledQuantity + quantity;
        int sellFilled = sell.FilledQuantity + quantity;
        var trade = new Trade(proposal.StockId, buy.UserId, sell.UserId, buy.Id, sell.Id, proposal.Price, quantity);

        await connection.ExecuteAsync(new CommandDefinition(SettlementSql, new
        {
            trade.Id,
            trade.StockId,
            trade.BuyerId,
            trade.SellerId,
            trade.BuyOrderId,
            trade.SellOrderId,
            trade.Price,
            trade.Quantity,
            BuyFilled = buyFilled,
            SellFilled = sellFilled,
            Total = total,
            BuyerPortfolioId = Guid.CreateVersion7()
        }, transaction, cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return TradeSettlementResult.Executed(trade, buyFilled, sellFilled);
    }

    private static bool IsExecutable(Order? buy, Order? sell, TradeProposal proposal) =>
        buy is not null && sell is not null && !buy.IsCancelled && !sell.IsCancelled
        && buy.Side == OrderSide.Buy && sell.Side == OrderSide.Sell
        && buy.UserId != sell.UserId
        && buy.StockId == proposal.StockId && sell.StockId == proposal.StockId
        && buy.Price >= sell.Price && buy.FilledQuantity < buy.Quantity
        && sell.FilledQuantity < sell.Quantity && proposal.Quantity > 0;

    private static Guid GetRejectedOrderId(Order? buy, Order? sell, TradeProposal proposal)
    {
        if (buy is null || buy.IsCancelled || buy.Side != OrderSide.Buy || buy.FilledQuantity >= buy.Quantity
            || buy.UserId == sell?.UserId)
        {
            return proposal.BuyOrderId;
        }

        return sell?.Id ?? proposal.SellOrderId;
    }

    private static Task<int> CancelAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        Guid orderId, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition(CancelSql, new { OrderId = orderId }, transaction,
            cancellationToken: cancellationToken));

    private const string OrderLockSql = """
        SELECT id, user_id AS UserId, stock_id AS StockId, side AS Side, price,
               quantity, filled_quantity AS FilledQuantity, is_cancelled AS IsCancelled,
               created_at AS CreatedAtUtc, updated_at AS UpdatedAtUtc
        FROM orders."order"
        WHERE id IN (@BuyOrderId, @SellOrderId)
        ORDER BY id FOR UPDATE;
        """;

    private const string AccountLockSql = """
        SELECT id, current_balance AS CurrentBalance
        FROM users."user"
        WHERE id IN (@BuyerId, @SellerId)
        ORDER BY id FOR UPDATE;
        """;

    private const string SellerPositionLockSql = """
        SELECT id, quantity, avg_price AS AvgPrice
        FROM orders.portfolio
        WHERE user_id = @UserId AND stock_id = @StockId
        FOR UPDATE;
        """;

    private const string CancelSql = """
        UPDATE orders."order"
        SET is_cancelled = TRUE, updated_at = NOW() AT TIME ZONE 'UTC'
        WHERE id = @OrderId AND filled_quantity < quantity;
        """;

    private const string SettlementSql = """
        UPDATE orders."order"
        SET filled_quantity = CASE id WHEN @BuyOrderId THEN @BuyFilled WHEN @SellOrderId THEN @SellFilled END,
            updated_at = NOW() AT TIME ZONE 'UTC'
        WHERE id IN (@BuyOrderId, @SellOrderId);

        UPDATE users."user"
        SET current_balance = CASE id WHEN @BuyerId THEN current_balance - @Total
                                      WHEN @SellerId THEN current_balance + @Total END,
            updated_at = NOW() AT TIME ZONE 'UTC'
        WHERE id IN (@BuyerId, @SellerId);

        UPDATE orders.portfolio
        SET quantity = quantity - @Quantity, updated_at = NOW() AT TIME ZONE 'UTC'
        WHERE user_id = @SellerId AND stock_id = @StockId;

        INSERT INTO orders.portfolio
            (id, user_id, stock_id, quantity, avg_price, created_at, updated_at)
        VALUES (@BuyerPortfolioId, @BuyerId, @StockId, @Quantity, @Price,
                NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC')
        ON CONFLICT (user_id, stock_id) DO UPDATE
        SET avg_price = ((orders.portfolio.avg_price * orders.portfolio.quantity) + (@Price * @Quantity))
                        / (orders.portfolio.quantity + @Quantity),
            quantity = orders.portfolio.quantity + @Quantity,
            updated_at = NOW() AT TIME ZONE 'UTC';

        DELETE FROM orders.portfolio
        WHERE user_id = @SellerId AND stock_id = @StockId AND quantity = 0;

        INSERT INTO orders.trade
            (id, stock_id, buyer_id, seller_id, buy_order_id, sell_order_id, price, quantity, executed_at)
        VALUES (@Id, @StockId, @BuyerId, @SellerId, @BuyOrderId, @SellOrderId,
                @Price, @Quantity, NOW() AT TIME ZONE 'UTC');
        """;

    private sealed record AccountBalance(Guid Id, decimal CurrentBalance);
    private sealed record PortfolioPosition(Guid Id, int Quantity, decimal AvgPrice);
}
