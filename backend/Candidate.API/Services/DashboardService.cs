using Candidate.API.Contracts;
using Candidate.API.Data;
using Candidate.API.Domain;
using Candidate.API.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Candidate.API.Services;

/// <summary>
/// Aggregates for the dashboard. Counting happens in SQL; the UI only renders the numbers, so
/// business rules (license validity, soft delete) are never re-implemented in the frontend.
/// </summary>
public sealed class DashboardService(AppDbContext db, AuditService audit, TimeProvider clock)
{
    private const int MaxExpiringLicenses = 50;

    public async Task<DashboardSummaryDto> GetSummaryAsync(int expiringWithinDays, CancellationToken cancellationToken)
    {
        var today = clock.GetTodayUtc();
        var isValid = LicenseRules.IsCurrentlyValid(today);
        var isExpired = LicenseRules.IsExpired(today);

        var countsByStatus = await db.Providers
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        // Every status is listed, including those with no providers, so charts keep a stable shape.
        var byStatus = Enum.GetValues<ProviderStatus>()
            .Select(status => new StatusCountDto(status.ToString(), countsByStatus.GetValueOrDefault(status)))
            .ToList();

        var activeWithExpiredLicenses = await db.Providers.CountAsync(
            p => p.Status == ProviderStatus.Active && p.Licenses.AsQueryable().Any(isExpired), cancellationToken);

        var totalLicenses = await db.Licenses.CountAsync(cancellationToken);
        var validLicenses = await db.Licenses.CountAsync(isValid, cancellationToken);
        var suspendedLicenses = await db.Licenses.CountAsync(l => l.LicenseStatus == LicenseStatus.Suspended, cancellationToken);
        // Effective statuses partition the licenses (see LicenseRules.GetEffectiveStatus):
        // Suspended, else valid (recorded Active and not past its date), else Expired.
        var expiredLicenses = totalLicenses - validLicenses - suspendedLicenses;

        var expiresBy = today.AddDays(expiringWithinDays);
        var expiring = await db.Licenses
            .AsNoTracking()
            .Where(isValid)
            .Where(l => l.ExpirationDate <= expiresBy)
            .OrderBy(l => l.ExpirationDate)
            .ThenBy(l => l.Provider.ProviderName)
            .Take(MaxExpiringLicenses)
            .Select(l => new
            {
                l.ProviderId,
                l.Provider.ProviderName,
                ProviderStatus = l.Provider.Status,
                l.LicenseId,
                l.LicenseNumber,
                l.ExpirationDate
            })
            .ToListAsync(cancellationToken);

        return new DashboardSummaryDto(
            today,
            expiringWithinDays,
            new ProviderMetricsDto(
                byStatus.Sum(s => s.Count),
                activeWithExpiredLicenses,
                await audit.CountDeletedProvidersAsync(cancellationToken),
                byStatus),
            new LicenseMetricsDto(totalLicenses, validLicenses, expiredLicenses, suspendedLicenses),
            expiring
                .Select(l => new ExpiringLicenseDto(
                    l.ProviderId,
                    l.ProviderName,
                    l.ProviderStatus,
                    l.LicenseId,
                    l.LicenseNumber,
                    l.ExpirationDate,
                    l.ExpirationDate.DayNumber - today.DayNumber))
                .ToList());
    }
}
