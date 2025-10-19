using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockhub.Modules.Orders.Domain.Orders;

namespace Stockhub.Modules.Orders.Infrastructure.Database.Configurations;

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

        builder.HasOne(o => o.Stock)
               .WithMany(s => s.Orders)
               .HasForeignKey(o => o.StockId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
