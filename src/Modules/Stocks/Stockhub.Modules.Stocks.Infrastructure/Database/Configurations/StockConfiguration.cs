using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockhub.Modules.Stocks.Domain;

namespace Stockhub.Modules.Stocks.Infrastructure.Database.Configurations;

internal sealed class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("stock");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.Symbol).HasColumnName("symbol");
        builder.Property(s => s.Name).HasColumnName("name");
        builder.Property(s => s.Sector).HasColumnName("sector");
        builder.Property(s => s.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(s => s.UpdatedAtUtc).HasColumnName("updated_at");

        builder.HasIndex(s => s.Symbol).IsUnique();
    }
}
