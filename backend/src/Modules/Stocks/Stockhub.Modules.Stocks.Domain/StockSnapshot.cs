using Stockhub.Common.Domain;

namespace Stockhub.Modules.Stocks.Domain;

public sealed class StockSnapshot : IAuditableEntity
{
    public Guid StockId { get; private set; }
    public decimal LastPrice { get; private set; }
    public decimal ChangePercent { get; private set; }
    public decimal MinPrice { get; private set; }
    public decimal MaxPrice { get; private set; }
    public long Volume { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Stock Stock { get; private set; }

    private StockSnapshot() { }

    public StockSnapshot(
        Guid stockId,
        decimal lastPrice,
        decimal changePercent,
        decimal minPrice,
        decimal maxPrice,
        long volume,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        StockId = stockId;
        LastPrice = lastPrice;
        ChangePercent = changePercent;
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        Volume = volume;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }
}
