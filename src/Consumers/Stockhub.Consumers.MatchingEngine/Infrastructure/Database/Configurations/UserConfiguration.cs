using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("user");

        builder.HasKey(p => p.Id);

        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.Email).HasColumnName("email");
        builder.Property(u => u.FullName).HasColumnName("full_name");
        builder.Property(u => u.CurrentBalance).HasColumnName("current_balance").HasPrecision(18, 2);
        builder.Property(u => u.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAtUtc).HasColumnName("updated_at");

        builder.HasIndex(u => u.Email).IsUnique();
    }
}
