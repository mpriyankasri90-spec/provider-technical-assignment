using System.Linq.Expressions;

namespace Candidate.API.Domain;

/// <summary>
/// Single definition of license validity, used by API queries (as EF-translatable expressions)
/// and mirrored by the database views <c>vw_License_Current</c> and the scenario views.
///
///   Expired         = ExpirationDate is before today (regardless of the recorded status)
///   Currently valid = recorded status Active AND ExpirationDate is today or later
///   Effective status: Suspended wins; otherwise Expired if the date has passed; otherwise the recorded status
///
/// Provider status is deliberately not part of any rule.
/// </summary>
public static class LicenseRules
{
    public static Expression<Func<License, bool>> IsExpired(DateOnly today) =>
        l => l.ExpirationDate < today;

    public static Expression<Func<License, bool>> IsCurrentlyValid(DateOnly today) =>
        l => l.LicenseStatus == LicenseStatus.Active && l.ExpirationDate >= today;

    public static LicenseStatus GetEffectiveStatus(LicenseStatus recorded, DateOnly expirationDate, DateOnly today)
    {
        if (recorded == LicenseStatus.Suspended)
        {
            return LicenseStatus.Suspended;
        }

        return expirationDate < today ? LicenseStatus.Expired : recorded;
    }
}
