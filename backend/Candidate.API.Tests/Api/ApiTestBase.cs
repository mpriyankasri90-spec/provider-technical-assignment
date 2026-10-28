using System.Net;
using System.Net.Http.Json;
using Candidate.API.Contracts;
using Candidate.API.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Candidate.API.Tests.Api;

public abstract class ApiTestBase(ApiFactory factory) : IClassFixture<ApiFactory>
{
    protected ApiFactory Factory { get; } = factory;
    protected HttpClient Client { get; } = factory.CreateClient();

    protected async Task<T> GetAsync<T>(string url)
    {
        var response = await Client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>(ApiFactory.Json))!;
    }

    protected async Task<ProviderDetailDto> CreateProviderAsync(string name, string county = "Fulton", string status = "Active")
    {
        var response = await Client.PostAsJsonAsync("/api/providers", new { providerName = name, county, status });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProviderDetailDto>(ApiFactory.Json))!;
    }

    protected async Task<IReadOnlyList<ProviderListItemDto>> ListAllAsync(string query = "")
    {
        var page = await GetAsync<PagedResult<ProviderListItemDto>>($"/api/providers?pageSize=100{query}");
        return page.Items;
    }

    protected async Task<int> SeededDeletedProviderIdAsync(string name)
    {
        var deleted = await GetAsync<List<DeletedProviderDto>>("/api/audit/providers/deleted");
        return deleted.Single(p => p.ProviderName == name).ProviderId;
    }

    protected static async Task<ProblemDetails> ReadProblemAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiFactory.Json))!;

    protected static string Unique(string name) => $"{name} {Guid.NewGuid():N}"[..(name.Length + 9)];
}
