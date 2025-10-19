using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockhub.Modules.Orders.Domain.Trades;

namespace Stockhub.Modules.Orders.Infrastructure.Database.Configurations;

internal sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("trade");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.Symbol).HasColumnName("symbol");
        builder.Property(t => t.BuyerId).HasColumnName("buyer_id");
        builder.Property(t => t.SellerId).HasColumnName("seller_id");
        builder.Property(t => t.BuyOrderId).HasColumnName("buy_order_id");
        builder.Property(t => t.SellOrderId).HasColumnName("sell_order_id");
        builder.Property(t => t.Price).HasColumnName("price").HasPrecision(18, 2);
        builder.Property(t => t.Quantity).HasColumnName("quantity");
        builder.Property(t => t.ExecutedAt).HasColumnName("executed_at");
        builder.Property(t => t.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(t => t.UpdatedAtUtc).HasColumnName("updated_at");
    }
}
