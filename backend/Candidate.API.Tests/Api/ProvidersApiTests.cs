using System.Net;
using System.Net.Http.Json;
using Candidate.API.Contracts;
using Candidate.API.Domain;
using Candidate.API.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Candidate.API.Tests.Api;

public sealed class ProvidersApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task List_excludes_soft_deleted_providers()
    {
        var names = (await ListAllAsync()).Select(p => p.ProviderName).ToList();

        Assert.Contains("Peachtree Health Services", names);
        Assert.DoesNotContain("Old Town Clinic", names);
        Assert.DoesNotContain("Riverside Care", names);
    }

    [Fact]
    public async Task Soft_deleted_providers_and_their_licenses_return_404()
    {
        var id = await SeededDeletedProviderIdAsync("Old Town Clinic");

        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/providers/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/providers/{id}/licenses")).StatusCode);
    }

    [Fact]
    public async Task List_reports_license_counts_by_validity()
    {
        var dekalb = (await ListAllAsync("&search=DeKalb Family")).Single();

        Assert.Equal(2, dekalb.LicenseCount);
        Assert.Equal(1, dekalb.ValidLicenseCount);
        Assert.Equal(1, dekalb.ExpiredLicenseCount);
    }

    [Fact]
    public async Task Search_treats_wildcard_characters_literally()
    {
        Assert.Empty(await ListAllAsync("&search=%25"));
        Assert.Empty(await ListAllAsync("&search=_"));
    }

    [Fact]
    public async Task Create_normalizes_input_and_stamps_audit_fields()
    {
        var name = Unique("Buckhead Clinic");

        var created = await CreateProviderAsync($"  {name.Replace(" ", "   ")} ", county: " Fulton ", status: "Pending");

        Assert.Equal(name, created.ProviderName);
        Assert.Equal("Fulton", created.County);
        Assert.Equal(ProviderStatus.Pending, created.Status);
        Assert.Equal("api.test", created.CreatedBy);
        Assert.Equal(1, created.Version);
        Assert.Empty(created.Licenses);
    }

    [Fact]
    public async Task Create_with_missing_or_invalid_fields_returns_validation_problem()
    {
        var response = await Client.PostAsJsonAsync("/api/providers", new { providerName = "   ", county = "Fulton" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ApiFactory.Json))!;
        Assert.Contains("ProviderName", problem.Errors.Keys);
        Assert.Contains("Status", problem.Errors.Keys);

        var badStatus = await Client.PostAsJsonAsync("/api/providers", new { providerName = "X", county = "Fulton", status = "Closed" });
        Assert.Equal(HttpStatusCode.BadRequest, badStatus.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_name_in_the_same_county_case_insensitively()
    {
        var name = Unique("Duplicate Care");
        await CreateProviderAsync(name, county: "Cobb");

        var duplicate = await Client.PostAsJsonAsync("/api/providers",
            new { providerName = name.ToUpperInvariant(), county = "cobb", status = "Active" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        // The same name in another county is a different provider.
        await CreateProviderAsync(name, county: "Henry");
    }

    [Fact]
    public async Task Update_requires_the_current_version()
    {
        var created = await CreateProviderAsync(Unique("Versioned Care"));
        var update = new { providerName = created.ProviderName, county = "Clayton", status = "Inactive", version = created.Version };

        var first = await Client.PutAsJsonAsync($"/api/providers/{created.ProviderId}", update);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var updated = (await first.Content.ReadFromJsonAsync<ProviderDetailDto>(ApiFactory.Json))!;
        Assert.Equal(2, updated.Version);
        Assert.Equal("Clayton", updated.County);
        Assert.Equal("api.test", updated.ModifiedBy);

        var stale = await Client.PutAsJsonAsync($"/api/providers/{created.ProviderId}", update);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
    }

    [Fact]
    public async Task Update_of_a_soft_deleted_provider_returns_404()
    {
        var id = await SeededDeletedProviderIdAsync("Riverside Care");

        var response = await Client.PutAsJsonAsync($"/api/providers/{id}",
            new { providerName = "Riverside Care", county = "DeKalb", status = "Active", version = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_soft_deletes_hides_the_provider_and_keeps_an_audit_record()
    {
        var created = await CreateProviderAsync(Unique("Short Lived Care"));

        var delete = await Client.DeleteAsync($"/api/providers/{created.ProviderId}?reason=Created%20in%20error");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/providers/{created.ProviderId}")).StatusCode);
        Assert.DoesNotContain(await ListAllAsync(), p => p.ProviderId == created.ProviderId);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.DeleteAsync($"/api/providers/{created.ProviderId}")).StatusCode);

        var deleted = await GetAsync<List<DeletedProviderDto>>("/api/audit/providers/deleted");
        var record = Assert.Single(deleted, p => p.ProviderId == created.ProviderId);
        Assert.Equal("Created in error", record.DeleteReason);
        Assert.Equal("api.test", record.DeletedBy);
        Assert.NotNull(record.DeletedAtUtc);

        var audit = await GetAsync<ProviderAuditDto>($"/api/audit/providers/{created.ProviderId}");
        Assert.True(audit.IsDeleted);
    }

    [Fact]
    public async Task Delete_with_a_stale_version_returns_409()
    {
        var created = await CreateProviderAsync(Unique("Guarded Care"));

        var response = await Client.DeleteAsync($"/api/providers/{created.ProviderId}?version={created.Version + 1}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync($"/api/providers/{created.ProviderId}")).StatusCode);
    }

    [Fact]
    public async Task Restore_brings_a_provider_back_unless_a_live_duplicate_exists()
    {
        var restorable = await CreateProviderAsync(Unique("Restorable Care"));
        await Client.DeleteAsync($"/api/providers/{restorable.ProviderId}");

        var restore = await Client.PostAsync($"/api/audit/providers/{restorable.ProviderId}/restore", null);
        Assert.Equal(HttpStatusCode.NoContent, restore.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync($"/api/providers/{restorable.ProviderId}")).StatusCode);

        var replaced = await CreateProviderAsync(Unique("Replaced Care"));
        await Client.DeleteAsync($"/api/providers/{replaced.ProviderId}");
        await CreateProviderAsync(replaced.ProviderName);

        var conflict = await Client.PostAsync($"/api/audit/providers/{replaced.ProviderId}/restore", null);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Contains("already exists", (await ReadProblemAsync(conflict)).Detail);
    }

    [Fact]
    public async Task Lookups_come_from_the_backend()
    {
        var lookups = await GetAsync<LookupsDto>("/api/lookups");

        Assert.Equal(["Active", "Inactive", "Pending"], lookups.ProviderStatuses);
        Assert.Equal(["Active", "Expired", "Suspended"], lookups.LicenseStatuses);
        Assert.Contains("Fulton", lookups.Counties);
    }
}
