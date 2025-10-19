using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockhub.Modules.Orders.Domain.Users;

namespace Stockhub.Modules.Orders.Infrastructure.Database.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("user");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.Email).HasColumnName("email");
        builder.Property(u => u.FullName).HasColumnName("full_name");
        builder.Property(u => u.CurrentBalance).HasColumnName("current_balance").HasPrecision(18, 2);
        builder.Property(u => u.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAtUtc).HasColumnName("updated_at");

        builder.HasMany(u => u.Orders)
               .WithOne()
               .HasForeignKey(o => o.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.PortfolioEntries)
               .WithOne()
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
