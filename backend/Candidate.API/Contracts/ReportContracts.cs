using Candidate.API.Domain;

namespace Candidate.API.Contracts;

/// <summary>A provider together with the licenses that match a report's rule.</summary>
public sealed record ProviderLicensesDto(
    int ProviderId,
    string ProviderName,
    string County,
    ProviderStatus Status,
    IReadOnlyList<LicenseDto> Licenses);

/// <summary>Audit view of a soft-deleted provider.</summary>
public sealed record DeletedProviderDto(
    int ProviderId,
    string ProviderName,
    string County,
    ProviderStatus Status,
    DateTime CreatedAtUtc,
    string CreatedBy,
    DateTime? DeletedAtUtc,
    string? DeletedBy,
    string? DeleteReason,
    int LicenseCount);

/// <summary>Full audit record of a provider, deleted or not, including deleted licenses.</summary>
public sealed record ProviderAuditDto(
    int ProviderId,
    string ProviderName,
    string County,
    ProviderStatus Status,
    DateTime CreatedAtUtc,
    string CreatedBy,
    DateTime? ModifiedAtUtc,
    string? ModifiedBy,
    int Version,
    bool IsDeleted,
    DateTime? DeletedAtUtc,
    string? DeletedBy,
    string? DeleteReason,
    IReadOnlyList<LicenseAuditDto> Licenses);

public sealed record LicenseAuditDto(
    int LicenseId,
    string LicenseNumber,
    LicenseStatus LicenseStatus,
    DateOnly ExpirationDate,
    bool IsDeleted,
    DateTime? DeletedAtUtc,
    string? DeletedBy);

/// <summary>Reference values for forms and filters, so the UI does not hard-code them.</summary>
public sealed record LookupsDto(
    IReadOnlyList<string> ProviderStatuses,
    IReadOnlyList<string> LicenseStatuses,
    IReadOnlyList<string> Counties);
