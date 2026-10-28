using Candidate.API.Contracts;
using Candidate.API.Domain;
using Candidate.API.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace Candidate.API.Tests.Api;

/// <summary>The required data scenarios against the seeded database.</summary>
public sealed class ReportsApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    private const string ActiveLicensesUrl = "/api/reports/active-providers-active-licenses";
    private const string ExpiredLicensesUrl = "/api/reports/active-providers-expired-licenses";

    [Fact]
    public async Task Scenario_1_active_providers_with_their_currently_valid_licenses()
    {
        var report = await GetAsync<List<ProviderLicensesDto>>(ActiveLicensesUrl);
        var names = report.Select(p => p.ProviderName).ToList();

        Assert.Contains("Peachtree Health Services", names);
        Assert.DoesNotContain("Clayton Home Health", names);   // valid license, but provider inactive
        Assert.DoesNotContain("Old Town Clinic", names);       // soft-deleted
        Assert.All(report, p => Assert.Equal(ProviderStatus.Active, p.Status));
        Assert.All(report.SelectMany(p => p.Licenses), l => Assert.True(l.IsCurrentlyValid));
    }

    [Fact]
    public async Task Scenario_2_providers_appearing_active_with_expired_licenses()
    {
        var report = await GetAsync<List<ProviderLicensesDto>>(ExpiredLicensesUrl);

        var dekalb = Assert.Single(report, p => p.ProviderName == "DeKalb Family Care");
        var license = Assert.Single(dekalb.Licenses);
        Assert.Equal("GA-FC-200411", license.LicenseNumber);
        Assert.Equal(LicenseStatus.Active, license.LicenseStatus);    // recorded status is stale
        Assert.Equal(LicenseStatus.Expired, license.EffectiveStatus); // the date decides

        var names = report.Select(p => p.ProviderName).ToList();
        Assert.DoesNotContain("Riverside Care", names);           // soft-deleted
        Assert.DoesNotContain("Lawrenceville Adult Care", names); // inactive
        Assert.All(report.SelectMany(p => p.Licenses), l => Assert.True(l.IsExpired));
    }

    [Theory]
    [InlineData(ActiveLicensesUrl, "vw_ActiveProviders_ActiveLicenses")]
    [InlineData(ExpiredLicensesUrl, "vw_ActiveProviders_ExpiredLicenses")]
    public async Task API_reports_match_the_SQL_views(string url, string view)
    {
        var report = await GetAsync<List<ProviderLicensesDto>>(url);
        var fromApi = report.SelectMany(p => p.Licenses.Select(l => (p.ProviderId, l.LicenseId))).Order().ToList();

        var fromView = new List<(int, int)>();
        await using (var connection = new SqliteConnection(Factory.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT ProviderId, LicenseId FROM {view}";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                fromView.Add((reader.GetInt32(0), reader.GetInt32(1)));
            }
        }

        Assert.NotEmpty(fromApi);
        Assert.Equal(fromView.Order().ToList(), fromApi);
    }
}
