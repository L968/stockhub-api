using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Stockhub.Common.Infrastructure.Extensions;
using Stockhub.Modules.Users.Application.Abstractions;
using Stockhub.Modules.Users.Domain;
using Stockhub.Modules.Users.Infrastructure.Database.Configurations;

namespace Stockhub.Modules.Users.Infrastructure.Database;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options), IUsersDbContext
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Users);
        modelBuilder.ApplyConfiguration(new UserConfiguration());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }
}
