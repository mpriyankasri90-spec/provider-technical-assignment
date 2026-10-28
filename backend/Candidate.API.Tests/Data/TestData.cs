using Candidate.API.Data;
using Candidate.API.Domain;

namespace Candidate.API.Tests.Data;

internal static class TestData
{
    public static Provider NewProvider(string name, ProviderStatus status = ProviderStatus.Active, string county = "Fulton",
        params License[] licenses) =>
        new() { ProviderName = name, County = county, Status = status, Licenses = licenses.ToList() };

    public static License NewLicense(string number, DateOnly expires, LicenseStatus status = LicenseStatus.Active) =>
        new() { LicenseNumber = number, LicenseStatus = status, ExpirationDate = expires };

    public static async Task<Provider> AddAsync(AppDbContext db, Provider provider)
    {
        db.Providers.Add(provider);
        await db.SaveChangesAsync();
        return provider;
    }
}
