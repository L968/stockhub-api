using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockhub.Consumers.Entities;

namespace Stockhub.Consumers.Database.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("order");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).HasColumnName("id");
        builder.Property(o => o.UserId).HasColumnName("user_id");
        builder.Property(o => o.StockId).HasColumnName("stock_id");
        builder.Property(o => o.Side).HasColumnName("side");
        builder.Property(o => o.Price).HasColumnName("price").HasPrecision(18, 2);
        builder.Property(o => o.Quantity).HasColumnName("quantity");
        builder.Property(o => o.FilledQuantity).HasColumnName("filled_quantity");
        builder.Property(o => o.Status).HasColumnName("status");
        builder.Property(o => o.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(o => o.UpdatedAtUtc).HasColumnName("updated_at");
    }
}
