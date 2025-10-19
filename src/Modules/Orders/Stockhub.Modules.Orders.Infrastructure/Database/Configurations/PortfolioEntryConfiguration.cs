using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockhub.Modules.Orders.Domain.PortfolioEntries;

namespace Stockhub.Modules.Orders.Infrastructure.Database.Configurations;

internal sealed class PortfolioEntryConfiguration : IEntityTypeConfiguration<PortfolioEntry>
{
    public void Configure(EntityTypeBuilder<PortfolioEntry> builder)
    {
        builder.ToTable("portfolio");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.UserId).HasColumnName("user_id");
        builder.Property(p => p.StockId).HasColumnName("stock_id");
        builder.Property(p => p.Quantity).HasColumnName("quantity");
        builder.Property(p => p.AvgPrice).HasColumnName("avg_price").HasPrecision(18, 2);
        builder.Property(p => p.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at");

        builder.HasOne(p => p.Stock)
               .WithMany(s => s.PortfolioEntries)
               .HasForeignKey(p => p.StockId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
