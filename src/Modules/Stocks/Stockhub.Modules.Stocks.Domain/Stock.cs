using Stockhub.Common.Domain;

namespace Stockhub.Modules.Stocks.Domain;

public sealed class Stock : IAuditableEntity
{
    public Guid Id { get; private set; }
    public string Symbol { get; private set; }
    public string Name { get; private set; }
    public string Sector { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public StockSnapshot Snapshot { get; private set; }

    private Stock() { }

    public Stock(string symbol, string name, string sector)
    {
        Id = Guid.CreateVersion7();
        Symbol = symbol;
        Name = name;
        Sector = sector;
    }

    public void UpdateSymbol(string symbol)
    {
        Symbol = symbol;
    }

    public void UpdateName(string name)
    {
        Name = name;
    }

    public void UpdateSector(string sector)
    {
        Sector = sector;
    }
}
