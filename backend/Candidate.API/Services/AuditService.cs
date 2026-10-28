using Candidate.API.Contracts;
using Candidate.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Candidate.API.Services;

/// <summary>
/// Audit and maintenance access to soft-deleted data. This is the only place that calls
/// IgnoreQueryFilters(), so bypassing soft delete is explicit and easy to review.
/// </summary>
public sealed class AuditService(AppDbContext db, ILogger<AuditService> logger)
{
    public async Task<IReadOnlyList<DeletedProviderDto>> ListDeletedProvidersAsync(CancellationToken cancellationToken) =>
        await db.Providers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.IsDeleted)
            .OrderByDescending(p => p.DeletedAtUtc)
            .Select(p => new DeletedProviderDto(
                p.ProviderId,
                p.ProviderName,
                p.County,
                p.Status,
                p.CreatedAtUtc,
                p.CreatedBy,
                p.DeletedAtUtc,
                p.DeletedBy,
                p.DeleteReason,
                p.Licenses.Count(l => !l.IsDeleted)))
            .ToListAsync(cancellationToken);

    public Task<int> CountDeletedProvidersAsync(CancellationToken cancellationToken) =>
        db.Providers.IgnoreQueryFilters().CountAsync(p => p.IsDeleted, cancellationToken);

    /// <summary>Full record of any provider, deleted or not, with all licenses including deleted ones.</summary>
    public async Task<ProviderAuditDto?> GetProviderAsync(int providerId, CancellationToken cancellationToken)
    {
        var provider = await db.Providers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(p => p.Licenses)
            .FirstOrDefaultAsync(p => p.ProviderId == providerId, cancellationToken);

        return provider is null
            ? null
            : new ProviderAuditDto(
                provider.ProviderId,
                provider.ProviderName,
                provider.County,
                provider.Status,
                provider.CreatedAtUtc,
                provider.CreatedBy,
                provider.ModifiedAtUtc,
                provider.ModifiedBy,
                provider.Version,
                provider.IsDeleted,
                provider.DeletedAtUtc,
                provider.DeletedBy,
                provider.DeleteReason,
                provider.Licenses
                    .OrderBy(l => l.ExpirationDate)
                    .Select(l => new LicenseAuditDto(
                        l.LicenseId, l.LicenseNumber, l.LicenseStatus, l.ExpirationDate, l.IsDeleted, l.DeletedAtUtc, l.DeletedBy))
                    .ToList());
    }

    /// <summary>
    /// Reverses a soft delete. The provider's licenses were never modified by the delete, so they
    /// reappear unchanged. Fails if a live provider now has the same name in the same county.
    /// </summary>
    public async Task<ServiceResult> RestoreProviderAsync(int providerId, CancellationToken cancellationToken)
    {
        var provider = await db.Providers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.ProviderId == providerId && p.IsDeleted, cancellationToken);

        if (provider is null)
        {
            return ServiceResult.NotFound($"Deleted provider {providerId} was not found.");
        }

        var duplicateExists = await db.Providers.AnyAsync(
            p => p.ProviderName == provider.ProviderName && p.County == provider.County, cancellationToken);
        if (duplicateExists)
        {
            return ServiceResult.Conflict(
                ErrorCodes.DuplicateProvider,
                $"Cannot restore: a provider named '{provider.ProviderName}' already exists in {provider.County} County.");
        }

        logger.LogInformation(
            "Restoring provider {ProviderId} (deleted {DeletedAtUtc} by {DeletedBy}, reason: {DeleteReason})",
            provider.ProviderId, provider.DeletedAtUtc, provider.DeletedBy, provider.DeleteReason);

        provider.IsDeleted = false;
        provider.DeletedAtUtc = null;
        provider.DeletedBy = null;
        provider.DeleteReason = null;
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }
}
