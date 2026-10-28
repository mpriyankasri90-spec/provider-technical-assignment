using Candidate.API.Contracts;
using Candidate.API.Domain;

namespace Candidate.API.Services;

internal static class LicenseMapper
{
    public static LicenseDto ToDto(License license, DateOnly today) => new(
        license.LicenseId,
        license.ProviderId,
        license.LicenseNumber,
        license.LicenseStatus,
        LicenseRules.GetEffectiveStatus(license.LicenseStatus, license.ExpirationDate, today),
        license.IssuedDate,
        license.ExpirationDate,
        IsExpired: LicenseRules.IsExpiredOn(license.ExpirationDate, today),
        IsCurrentlyValid: LicenseRules.IsCurrentlyValidOn(license.LicenseStatus, license.ExpirationDate, today),
        DaysUntilExpiration: license.ExpirationDate.DayNumber - today.DayNumber);
}
