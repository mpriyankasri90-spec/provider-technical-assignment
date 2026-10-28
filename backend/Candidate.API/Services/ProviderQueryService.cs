using Candidate.API.Contracts;
using Candidate.API.Data;
using Candidate.API.Domain;
using Candidate.API.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Candidate.API.Services;

/// <summary>
/// Read-side queries for standard application views. Soft-deleted rows are excluded by the
/// global query filters in <see cref="AppDbContext"/>; nothing here needs its own IsDeleted check.
/// </summary>
public sealed class ProviderQueryService(AppDbContext db, TimeProvider clock)
{
    public async Task<PagedResult<ProviderListItemDto>> ListAsync(ProviderListQuery query, CancellationToken cancellationToken)
    {
        var today = clock.GetTodayUtc();
        var isValid = LicenseRules.IsCurrentlyValid(today);
        var isExpired = LicenseRules.IsExpired(today);

        IQueryable<Provider> providers = db.Providers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";
            providers = providers.Where(p => EF.Functions.Like(p.ProviderName, pattern, "\\"));
        }

        if (query.Status is { } status)
        {
            providers = providers.Where(p => p.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.County))
        {
            var county = query.County.Trim();
            providers = providers.Where(p => p.County == county);
        }

        var totalCount = await providers.CountAsync(cancellationToken);

        var items = await providers
            .OrderBy(p => p.ProviderName)
            .ThenBy(p => p.ProviderId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new ProviderListItemDto(
                p.ProviderId,
                p.ProviderName,
                p.County,
                p.Status,
                p.CreatedAtUtc,
                p.Version,
                p.Licenses.Count,
                p.Licenses.AsQueryable().Count(isValid),
                p.Licenses.AsQueryable().Count(isExpired)))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProviderListItemDto>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<ProviderDetailDto?> GetAsync(int providerId, CancellationToken cancellationToken)
    {
        var provider = await db.Providers
            .AsNoTracking()
            .Include(p => p.Licenses)
            .FirstOrDefaultAsync(p => p.ProviderId == providerId, cancellationToken);

        if (provider is null)
        {
            return null;
        }

        var today = clock.GetTodayUtc();
        return new ProviderDetailDto(
            provider.ProviderId,
            provider.ProviderName,
            provider.County,
            provider.Status,
            provider.CreatedAtUtc,
            provider.CreatedBy,
            provider.ModifiedAtUtc,
            provider.ModifiedBy,
            provider.Version,
            provider.Licenses
                .OrderBy(l => l.ExpirationDate)
                .Select(l => LicenseMapper.ToDto(l, today))
                .ToList());
    }

    /// <summary>Licenses of a non-deleted provider, or null when the provider is not visible.</summary>
    public async Task<IReadOnlyList<LicenseDto>?> GetLicensesAsync(int providerId, CancellationToken cancellationToken)
    {
        if (!await db.Providers.AnyAsync(p => p.ProviderId == providerId, cancellationToken))
        {
            return null;
        }

        var today = clock.GetTodayUtc();
        var licenses = await db.Licenses
            .AsNoTracking()
            .Where(l => l.ProviderId == providerId)
            .OrderBy(l => l.ExpirationDate)
            .ToListAsync(cancellationToken);

        return licenses.Select(l => LicenseMapper.ToDto(l, today)).ToList();
    }

    public async Task<LookupsDto> GetLookupsAsync(CancellationToken cancellationToken)
    {
        var counties = await db.Providers
            .AsNoTracking()
            .Select(p => p.County)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);

        return new LookupsDto(Enum.GetNames<ProviderStatus>(), Enum.GetNames<LicenseStatus>(), counties);
    }

    // User input must not act as LIKE wildcards ("100%" should match the literal text).
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
