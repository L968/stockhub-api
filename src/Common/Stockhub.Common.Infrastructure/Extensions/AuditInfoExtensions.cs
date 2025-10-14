using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Stockhub.Common.Domain;

namespace Stockhub.Common.Infrastructure.Extensions;

public static class AuditInfoExtensions
{
    public static void ApplyAuditInfo(this ChangeTracker changeTracker)
    {
        DateTime utcNow = DateTime.UtcNow;
        IEnumerable<EntityEntry<IAuditableEntity>> entries = changeTracker.Entries<IAuditableEntity>();

        foreach (EntityEntry<IAuditableEntity> entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(e => e.CreatedAtUtc).CurrentValue = utcNow;
                    entry.Property(e => e.UpdatedAtUtc).CurrentValue = utcNow;
                    break;

                case EntityState.Modified:
                    entry.Property(e => e.UpdatedAtUtc).CurrentValue = utcNow;
                    break;
            }
        }
    }
}
