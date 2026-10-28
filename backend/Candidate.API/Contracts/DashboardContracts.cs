using Candidate.API.Domain;

namespace Candidate.API.Contracts;

/// <summary>
/// Read-only dashboard metrics, all computed by the API with the same rules as the reports.
/// Soft-deleted providers (and their licenses) are excluded except from the explicit deleted count.
/// </summary>
public sealed record DashboardSummaryDto(
    DateOnly AsOfDate,
    int ExpiringWithinDays,
    ProviderMetricsDto Providers,
    LicenseMetricsDto Licenses,
    IReadOnlyList<ExpiringLicenseDto> ExpiringLicenses);

public sealed record ProviderMetricsDto(
    int Total,
    int ActiveWithExpiredLicenses,
    int Deleted,
    IReadOnlyList<StatusCountDto> ByStatus);

/// <summary>Licenses by effective status. The three counts always add up to <see cref="Total"/>.</summary>
public sealed record LicenseMetricsDto(int Total, int CurrentlyValid, int Expired, int Suspended);

public sealed record StatusCountDto(string Status, int Count);

public sealed record ExpiringLicenseDto(
    int ProviderId,
    string ProviderName,
    ProviderStatus ProviderStatus,
    int LicenseId,
    string LicenseNumber,
    DateOnly ExpirationDate,
    int DaysUntilExpiration);
