using System.Linq.Expressions;
using Candidate.API.Contracts;
using Candidate.API.Data;
using Candidate.API.Domain;
using Candidate.API.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Candidate.API.Services;

/// <summary>
/// The required license scenarios. Equivalent SQL views: vw_ActiveProviders_ActiveLicenses and
/// vw_ActiveProviders_ExpiredLicenses. Soft-deleted providers and licenses are excluded by the
/// global query filters (scenario 3).
/// </summary>
public sealed class ReportService(AppDbContext db, TimeProvider clock)
{
    /// <summary>Scenario 1: active providers and their currently valid licenses.</summary>
    public Task<IReadOnlyList<ProviderLicensesDto>> GetActiveProvidersWithActiveLicensesAsync(CancellationToken cancellationToken) =>
        GetActiveProvidersWithLicensesAsync(LicenseRules.IsCurrentlyValid, cancellationToken);

    /// <summary>Scenario 2: providers whose status is Active but that hold licenses past their expiration date.</summary>
    public Task<IReadOnlyList<ProviderLicensesDto>> GetActiveProvidersWithExpiredLicensesAsync(CancellationToken cancellationToken) =>
        GetActiveProvidersWithLicensesAsync(LicenseRules.IsExpired, cancellationToken);

    private async Task<IReadOnlyList<ProviderLicensesDto>> GetActiveProvidersWithLicensesAsync(
        Func<DateOnly, Expression<Func<License, bool>>> licenseRule, CancellationToken cancellationToken)
    {
        var today = clock.GetTodayUtc();

        var licenses = await db.Licenses
            .AsNoTracking()
            .Include(l => l.Provider)
            .Where(l => l.Provider.Status == ProviderStatus.Active)
            .Where(licenseRule(today))
            .OrderBy(l => l.Provider.ProviderName)
            .ThenBy(l => l.ExpirationDate)
            .ToListAsync(cancellationToken);

        return licenses
            .GroupBy(l => l.ProviderId)
            .Select(g =>
            {
                var provider = g.First().Provider;
                return new ProviderLicensesDto(
                    provider.ProviderId,
                    provider.ProviderName,
                    provider.County,
                    provider.Status,
                    g.Select(l => LicenseMapper.ToDto(l, today)).ToList());
            })
            .ToList();
    }
}
