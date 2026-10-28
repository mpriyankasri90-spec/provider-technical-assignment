using Candidate.API.Domain;

namespace Candidate.API.Tests.Domain;

public class LicenseRulesTests
{
    private static readonly DateOnly Today = new(2026, 6, 15);

    public static TheoryData<LicenseStatus, string, LicenseStatus, bool, bool> Cases => new()
    {
        // recorded status, expiration, expected effective status, expected expired, expected valid
        { LicenseStatus.Active, "2026-06-15", LicenseStatus.Active, false, true },       // expires today: still valid
        { LicenseStatus.Active, "2026-06-14", LicenseStatus.Expired, true, false },      // recorded Active, date passed
        { LicenseStatus.Active, "2030-01-01", LicenseStatus.Active, false, true },
        { LicenseStatus.Expired, "2023-01-01", LicenseStatus.Expired, true, false },
        { LicenseStatus.Expired, "2030-01-01", LicenseStatus.Expired, false, false },    // recorded Expired early
        { LicenseStatus.Suspended, "2030-01-01", LicenseStatus.Suspended, false, false },
        { LicenseStatus.Suspended, "2020-01-01", LicenseStatus.Suspended, true, false }, // suspension wins
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Rules_classify_licenses_by_expiration_date_and_recorded_status(
        LicenseStatus recorded, string expiration, LicenseStatus effective, bool expired, bool valid)
    {
        var expirationDate = DateOnly.Parse(expiration);

        Assert.Equal(effective, LicenseRules.GetEffectiveStatus(recorded, expirationDate, Today));
        Assert.Equal(expired, LicenseRules.IsExpiredOn(expirationDate, Today));
        Assert.Equal(valid, LicenseRules.IsCurrentlyValidOn(recorded, expirationDate, Today));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Expression_forms_used_in_SQL_agree_with_in_memory_forms(
        LicenseStatus recorded, string expiration, LicenseStatus effective, bool expired, bool valid)
    {
        _ = effective;
        var license = new License { LicenseStatus = recorded, ExpirationDate = DateOnly.Parse(expiration) };

        Assert.Equal(expired, LicenseRules.IsExpired(Today).Compile()(license));
        Assert.Equal(valid, LicenseRules.IsCurrentlyValid(Today).Compile()(license));
    }
}
