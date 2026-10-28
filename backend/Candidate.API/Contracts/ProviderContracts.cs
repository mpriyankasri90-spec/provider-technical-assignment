using System.ComponentModel.DataAnnotations;
using Candidate.API.Domain;

namespace Candidate.API.Contracts;

/// <summary>Query string for the provider listing. Deleted providers are never included.</summary>
public sealed class ProviderListQuery
{
    /// <summary>Case-insensitive "contains" match on the provider name.</summary>
    [StringLength(200)]
    public string? Search { get; init; }

    public ProviderStatus? Status { get; init; }

    [StringLength(100)]
    public string? County { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record ProviderListItemDto(
    int ProviderId,
    string ProviderName,
    string County,
    ProviderStatus Status,
    DateTime CreatedAtUtc,
    int Version,
    int LicenseCount,
    int ValidLicenseCount,
    int ExpiredLicenseCount);

public sealed record ProviderDetailDto(
    int ProviderId,
    string ProviderName,
    string County,
    ProviderStatus Status,
    DateTime CreatedAtUtc,
    string CreatedBy,
    DateTime? ModifiedAtUtc,
    string? ModifiedBy,
    int Version,
    IReadOnlyList<LicenseDto> Licenses);

/// <summary>
/// License with both the recorded status and the status derived from the expiration date
/// (see <see cref="LicenseRules"/>). Validity is computed here, never in the UI.
/// </summary>
public sealed record LicenseDto(
    int LicenseId,
    int ProviderId,
    string LicenseNumber,
    LicenseStatus LicenseStatus,
    LicenseStatus EffectiveStatus,
    DateOnly? IssuedDate,
    DateOnly ExpirationDate,
    bool IsExpired,
    bool IsCurrentlyValid,
    int DaysUntilExpiration);

public class CreateProviderRequest
{
    [Required, StringLength(200)]
    public string ProviderName { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string County { get; init; } = string.Empty;

    /// <summary>Nullable so a missing value is a validation error, not a silent default.</summary>
    [Required]
    public ProviderStatus? Status { get; init; }
}

public sealed class UpdateProviderRequest : CreateProviderRequest
{
    /// <summary>The <c>version</c> the client loaded; a mismatch returns 409 Conflict.</summary>
    [Required]
    public int? Version { get; init; }
}
