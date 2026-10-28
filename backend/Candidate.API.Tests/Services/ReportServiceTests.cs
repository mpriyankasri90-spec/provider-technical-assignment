using Candidate.API.Domain;
using Candidate.API.Services;
using Candidate.API.Tests.Infrastructure;
using static Candidate.API.Tests.Data.TestData;

namespace Candidate.API.Tests.Services;

/// <summary>The required license scenarios with a fixed "today" (2026-06-15).</summary>
public sealed class ReportServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public ReportServiceTests()
    {
        var today = _database.Today;
        using var db = _database.CreateContext();

        db.Providers.AddRange(
            NewProvider("Mixed Care", ProviderStatus.Active, licenses:
            [
                NewLicense("EXPIRES-TODAY", today),
                NewLicense("EXPIRED-YESTERDAY", today.AddDays(-1)),   // still recorded Active
                NewLicense("SUSPENDED", today.AddYears(1), LicenseStatus.Suspended)
            ]),
            NewProvider("Inactive Care", ProviderStatus.Inactive, licenses:
            [
                NewLicense("INACTIVE-VALID", today.AddYears(1)),
                NewLicense("INACTIVE-EXPIRED", today.AddYears(-1))
            ]),
            NewProvider("Deleted Care", ProviderStatus.Active, licenses:
            [
                NewLicense("DELETED-VALID", today.AddYears(1)),
                NewLicense("DELETED-EXPIRED", today.AddYears(-1))
            ]));
        db.SaveChanges();

        db.Providers.Remove(db.Providers.Single(p => p.ProviderName == "Deleted Care"));
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Active_providers_with_active_licenses_returns_only_currently_valid_licenses()
    {
        await using var db = _database.CreateContext();

        var result = await new ReportService(db, _database.Clock).GetActiveProvidersWithActiveLicensesAsync(default);

        var provider = Assert.Single(result);
        Assert.Equal("Mixed Care", provider.ProviderName);
        var license = Assert.Single(provider.Licenses);
        Assert.Equal("EXPIRES-TODAY", license.LicenseNumber);
        Assert.True(license.IsCurrentlyValid);
    }

    [Fact]
    public async Task Active_providers_with_expired_licenses_uses_the_expiration_date_not_the_recorded_status()
    {
        await using var db = _database.CreateContext();

        var result = await new ReportService(db, _database.Clock).GetActiveProvidersWithExpiredLicensesAsync(default);

        var provider = Assert.Single(result);
        Assert.Equal("Mixed Care", provider.ProviderName);
        var license = Assert.Single(provider.Licenses);
        Assert.Equal("EXPIRED-YESTERDAY", license.LicenseNumber);
        Assert.Equal(LicenseStatus.Active, license.LicenseStatus);
        Assert.Equal(LicenseStatus.Expired, license.EffectiveStatus);
    }
}
