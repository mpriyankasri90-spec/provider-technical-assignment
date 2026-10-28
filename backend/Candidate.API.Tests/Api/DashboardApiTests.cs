using System.Net;
using Candidate.API.Contracts;
using Candidate.API.Tests.Infrastructure;

namespace Candidate.API.Tests.Api;

/// <summary>Dashboard figures must agree with the listing, reports and audit endpoints.</summary>
public sealed class DashboardApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Summary_is_consistent_with_the_other_endpoints()
    {
        var summary = await GetAsync<DashboardSummaryDto>("/api/dashboard/summary");
        var providers = await ListAllAsync();
        var expiredReport = await GetAsync<List<ProviderLicensesDto>>("/api/reports/active-providers-expired-licenses");
        var deleted = await GetAsync<List<DeletedProviderDto>>("/api/audit/providers/deleted");

        Assert.Equal(providers.Count, summary.Providers.Total);
        foreach (var status in summary.Providers.ByStatus)
        {
            Assert.Equal(providers.Count(p => p.Status.ToString() == status.Status), status.Count);
        }

        Assert.Equal(expiredReport.Count, summary.Providers.ActiveWithExpiredLicenses);
        Assert.Equal(deleted.Count, summary.Providers.Deleted);

        var licenses = summary.Licenses;
        Assert.Equal(providers.Sum(p => p.LicenseCount), licenses.Total);
        Assert.Equal(providers.Sum(p => p.ValidLicenseCount), licenses.CurrentlyValid);
        Assert.Equal(licenses.Total, licenses.CurrentlyValid + licenses.Expired + licenses.Suspended);
    }

    [Fact]
    public async Task Expiring_licenses_are_valid_licenses_inside_the_window()
    {
        var summary = await GetAsync<DashboardSummaryDto>("/api/dashboard/summary?expiringWithinDays=30");

        Assert.Equal(30, summary.ExpiringWithinDays);
        Assert.Contains(summary.ExpiringLicenses, l => l.LicenseNumber == "GA-WC-600015"); // seeded 20 days out
        Assert.All(summary.ExpiringLicenses, l => Assert.InRange(l.DaysUntilExpiration, 0, 30));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public async Task Expiring_window_outside_the_allowed_range_is_rejected(int days)
    {
        var response = await Client.GetAsync($"/api/dashboard/summary?expiringWithinDays={days}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
