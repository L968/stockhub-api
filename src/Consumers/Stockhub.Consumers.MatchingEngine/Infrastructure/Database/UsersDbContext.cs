using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Stockhub.Common.Infrastructure.Extensions;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

internal sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Users);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }
}
