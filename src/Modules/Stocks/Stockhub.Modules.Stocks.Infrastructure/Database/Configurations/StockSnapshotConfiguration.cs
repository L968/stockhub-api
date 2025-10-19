using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockhub.Modules.Stocks.Domain;

namespace Stockhub.Modules.Stocks.Infrastructure.Database.Configurations;

internal sealed class StockSnapshotConfiguration : IEntityTypeConfiguration<StockSnapshot>
{
    public void Configure(EntityTypeBuilder<StockSnapshot> builder)
    {
        builder.ToTable("stock_snapshot");

        builder.HasKey(s => s.StockId);

        builder.Property(s => s.StockId).HasColumnName("stock_id");
        builder.Property(s => s.LastPrice).HasColumnName("last_price").HasPrecision(18, 2);
        builder.Property(s => s.ChangePercent).HasColumnName("change_percent").HasPrecision(5, 2);
        builder.Property(s => s.MinPrice).HasColumnName("min_price").HasPrecision(18, 2);
        builder.Property(s => s.MaxPrice).HasColumnName("max_price").HasPrecision(18, 2);
        builder.Property(s => s.Volume).HasColumnName("volume");
        builder.Property(s => s.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(s => s.UpdatedAtUtc).HasColumnName("updated_at");

        builder.HasOne<Stock>()
            .WithOne(s => s.Snapshot)
            .HasForeignKey<StockSnapshot>(s => s.StockId);
    }
}
