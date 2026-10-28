using Candidate.API.Domain;
using Candidate.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Candidate.API.Data.Interceptors;

/// <summary>
/// Maintains <see cref="IAuditable"/> columns on the server side: creation stamp on insert,
/// modification stamp and version increment on update. Created* columns are made immutable.
/// </summary>
public sealed class AuditInterceptor(TimeProvider clock, ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = clock.GetUtcNow().UtcDateTime;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    // Seed scripts may stamp their own name; requests never can (not on any DTO).
                    if (string.IsNullOrWhiteSpace(entry.Entity.CreatedBy))
                    {
                        entry.Entity.CreatedBy = currentUser.UserName;
                    }
                    entry.Entity.ModifiedAtUtc = null;
                    entry.Entity.ModifiedBy = null;
                    entry.Entity.Version = 1;
                    break;

                case EntityState.Modified:
                    entry.Property(e => e.CreatedAtUtc).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified = false;
                    entry.Entity.ModifiedAtUtc = now;
                    entry.Entity.ModifiedBy = currentUser.UserName;
                    // OriginalValue stays in the UPDATE's WHERE clause (concurrency check).
                    entry.Entity.Version = entry.Property(e => e.Version).OriginalValue + 1;
                    break;
            }
        }
    }
}
