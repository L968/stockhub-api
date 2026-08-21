using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockhub.Modules.Orders.Infrastructure.Messaging;

namespace Stockhub.Modules.Orders.Infrastructure.Database.Configurations;

internal sealed class IntegrationOutboxMessageConfiguration : IEntityTypeConfiguration<IntegrationOutboxMessage>
{
    public void Configure(EntityTypeBuilder<IntegrationOutboxMessage> builder)
    {
        builder.ToTable("integration_outbox");
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id).HasColumnName("id");
        builder.Property(message => message.OrderId).HasColumnName("order_id");
        builder.Property(message => message.StockId).HasColumnName("stock_id");
        builder.Property(message => message.Type).HasColumnName("type").HasMaxLength(200);
        builder.Property(message => message.Payload).HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(message => message.OccurredAtUtc).HasColumnName("occurred_at");
        builder.Property(message => message.PublishedAtUtc).HasColumnName("published_at");
        builder.Property(message => message.Attempts).HasColumnName("attempts");
        builder.Property(message => message.LockId).HasColumnName("lock_id");
        builder.Property(message => message.LockedUntilUtc).HasColumnName("locked_until");
        builder.Property(message => message.LastError).HasColumnName("last_error").HasMaxLength(2000);

        builder.HasIndex(message => new { message.PublishedAtUtc, message.OccurredAtUtc });
        builder.HasIndex(message => message.OrderId).IsUnique();
    }
}
