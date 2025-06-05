global using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TaskTracker.Domain.Common;

namespace TaskTracker.Infrastructure.Data.Interceptors;
internal class AuditableEntityInterceptor(ICurrentUser currentUser, TimeProvider dateTime) : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly TimeProvider _dateTime = dateTime;
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
    private void UpdateEntities(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                var utcNow = _dateTime.GetUtcNow();
                if (entry.State is EntityState.Added)
                {
                    entry.Entity.Created = utcNow;
                    entry.Entity.CreatedBy = _currentUser.Email;
                }
                entry.Entity.LastModified = utcNow;
                entry.Entity.LastModifiedBy = _currentUser.Email;
            }
        }
    }
}
public static class Extensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry<BaseAuditableEntity> entry) =>
        entry.References.Any(r => r.TargetEntry is not null &&
            r.TargetEntry.Metadata.IsOwned() &&
            (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}
