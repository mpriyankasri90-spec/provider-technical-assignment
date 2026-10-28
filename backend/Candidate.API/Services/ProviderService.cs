using Candidate.API.Contracts;
using Candidate.API.Data;
using Candidate.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace Candidate.API.Services;

/// <summary>
/// Provider create/update/delete. Business rules are enforced here (not only in the UI):
/// inputs are normalized, a provider name must be unique within its county among
/// non-deleted providers, updates use optimistic concurrency, and deletes are soft.
/// </summary>
public sealed class ProviderService(AppDbContext db, ProviderQueryService queries, ILogger<ProviderService> logger)
{
    public async Task<ServiceResult<ProviderDetailDto>> CreateAsync(CreateProviderRequest request, CancellationToken cancellationToken)
    {
        var name = Normalize(request.ProviderName);
        var county = Normalize(request.County);

        if (await IsDuplicateAsync(name, county, excludeProviderId: null, cancellationToken))
        {
            return ServiceResult<ProviderDetailDto>.Conflict(DuplicateMessage(name, county));
        }

        var provider = new Provider
        {
            ProviderName = name,
            County = county,
            Status = request.Status!.Value
        };

        db.Providers.Add(provider);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Provider {ProviderId} created by {User}", provider.ProviderId, provider.CreatedBy);
        return ServiceResult<ProviderDetailDto>.Success((await queries.GetAsync(provider.ProviderId, cancellationToken))!);
    }

    public async Task<ServiceResult<ProviderDetailDto>> UpdateAsync(
        int providerId, UpdateProviderRequest request, CancellationToken cancellationToken)
    {
        var provider = await db.Providers.FirstOrDefaultAsync(p => p.ProviderId == providerId, cancellationToken);
        if (provider is null)
        {
            return ServiceResult<ProviderDetailDto>.NotFound(NotFoundMessage(providerId));
        }

        if (provider.Version != request.Version)
        {
            return ServiceResult<ProviderDetailDto>.Conflict(StaleMessage);
        }

        var name = Normalize(request.ProviderName);
        var county = Normalize(request.County);

        if (await IsDuplicateAsync(name, county, providerId, cancellationToken))
        {
            return ServiceResult<ProviderDetailDto>.Conflict(DuplicateMessage(name, county));
        }

        provider.ProviderName = name;
        provider.County = county;
        provider.Status = request.Status!.Value;

        // A concurrent change between the read above and this save is caught by the Version
        // concurrency token and returned as 409 by DatabaseExceptionHandler.
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Provider {ProviderId} updated to version {Version}", provider.ProviderId, provider.Version);
        return ServiceResult<ProviderDetailDto>.Success((await queries.GetAsync(providerId, cancellationToken))!);
    }

    // expectedVersion is optional; when supplied the delete fails with 409 if the record changed.
    public async Task<ServiceResult> SoftDeleteAsync(
        int providerId, string? reason, int? expectedVersion, CancellationToken cancellationToken)
    {
        // Already-deleted providers are invisible here, so deleting twice returns 404.
        var provider = await db.Providers.FirstOrDefaultAsync(p => p.ProviderId == providerId, cancellationToken);
        if (provider is null)
        {
            return ServiceResult.NotFound(NotFoundMessage(providerId));
        }

        if (expectedVersion is not null && provider.Version != expectedVersion)
        {
            return ServiceResult.Conflict(StaleMessage);
        }

        provider.DeleteReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        // SoftDeleteInterceptor turns this into an UPDATE that sets IsDeleted, DeletedAtUtc and
        // DeletedBy. Licenses are left untouched and are hidden through the query filter.
        db.Providers.Remove(provider);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Provider {ProviderId} soft-deleted by {User}", provider.ProviderId, provider.DeletedBy);
        return ServiceResult.Success();
    }

    private Task<bool> IsDuplicateAsync(string name, string county, int? excludeProviderId, CancellationToken cancellationToken) =>
        // Both columns use NOCASE collation, so this comparison is case-insensitive.
        db.Providers.AnyAsync(
            p => p.ProviderName == name && p.County == county && p.ProviderId != excludeProviderId,
            cancellationToken);

    // Trim and collapse internal whitespace so "Acme  Care " and "Acme Care" are the same name.
    private static string Normalize(string value) =>
        string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string NotFoundMessage(int providerId) => $"Provider {providerId} was not found.";

    private static string DuplicateMessage(string name, string county) =>
        $"A provider named '{name}' already exists in {county} County.";

    private const string StaleMessage =
        "The provider was changed by someone else after you loaded it. Reload it and apply your changes again.";
}
