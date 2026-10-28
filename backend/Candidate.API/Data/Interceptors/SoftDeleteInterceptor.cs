using Candidate.API.Domain;
using Candidate.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Candidate.API.Data.Interceptors;

/// <summary>
/// Turns any EF delete of an <see cref="ISoftDeletable"/> entity into an update that sets
/// the soft-delete columns. Even an accidental <c>context.Remove(entity)</c> cannot physically
/// delete a row. Must run before <see cref="AuditInterceptor"/> so the update is audited.
/// </summary>
public sealed class SoftDeleteInterceptor(TimeProvider clock, ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ConvertDeletes(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ConvertDeletes(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void ConvertDeletes(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = clock.GetUtcNow().UtcDateTime;

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>().Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAtUtc ??= now;
            entry.Entity.DeletedBy ??= currentUser.UserName;
        }
    }
}
