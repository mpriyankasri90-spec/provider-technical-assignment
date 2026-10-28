using Candidate.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace Candidate.API.Data;

/// <summary>
/// Inserts sample data that demonstrates every required scenario. Runs only when the database
/// has no providers at all (deleted ones included), so it never duplicates or "revives" data.
///
/// Expired licenses use fixed past dates and valid ones use dates years ahead, so the committed
/// database stays correct over time. The "expiring soon" license is relative to the seed date.
/// </summary>
public static class DbSeeder
{
    private const string SeedUser = "seed";

    public static async Task SeedAsync(AppDbContext db, TimeProvider clock, CancellationToken cancellationToken = default)
    {
        if (await db.Providers.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        db.Providers.AddRange(
            // Scenario 1: active provider, all licenses currently valid.
            NewProvider("Peachtree Health Services", "Fulton", ProviderStatus.Active,
                NewLicense("GA-HS-100231", LicenseStatus.Active, new(2023, 1, 15), new(2028, 1, 14)),
                NewLicense("GA-HS-100232", LicenseStatus.Active, new(2024, 6, 1), new(2029, 5, 31))),

            // Scenarios 1 + 2: one valid license, one still RECORDED as Active but past its date.
            NewProvider("DeKalb Family Care", "DeKalb", ProviderStatus.Active,
                NewLicense("GA-FC-200410", LicenseStatus.Active, new(2023, 3, 1), new(2028, 2, 29)),
                NewLicense("GA-FC-200411", LicenseStatus.Active, new(2021, 7, 1), new(2024, 6, 30))),

            // Scenario 2: provider looks active, its only license has expired.
            NewProvider("Cobb Senior Living", "Cobb", ProviderStatus.Active,
                NewLicense("GA-SL-300077", LicenseStatus.Expired, new(2020, 9, 1), new(2023, 8, 31))),

            // Active provider with zero licenses.
            NewProvider("Gwinnett Pediatrics", "Gwinnett", ProviderStatus.Active),

            // Provider status does not decide license validity: inactive provider, valid license.
            NewProvider("Clayton Home Health", "Clayton", ProviderStatus.Inactive,
                NewLicense("GA-HH-400120", LicenseStatus.Active, new(2024, 2, 1), new(2029, 1, 31))),

            // Suspended license with a future expiration date is still not valid.
            NewProvider("Henry Therapy Group", "Henry", ProviderStatus.Pending,
                NewLicense("GA-TG-500300", LicenseStatus.Suspended, new(2024, 4, 1), new(2028, 3, 31))),

            // Dashboard: license expiring within 30 days of the seed date.
            NewProvider("Forsyth Wellness Center", "Forsyth", ProviderStatus.Active,
                NewLicense("GA-WC-600015", LicenseStatus.Active, today.AddYears(-3), today.AddDays(20))),

            // Scenario 3: soft-deleted providers stay in the table, hidden from standard results.
            Deleted(NewProvider("Old Town Clinic", "Fulton", ProviderStatus.Active,
                    NewLicense("GA-OT-700001", LicenseStatus.Active, new(2023, 5, 1), new(2028, 4, 30))),
                now.AddDays(-30), "Duplicate record of Peachtree Health Services"),
            Deleted(NewProvider("Riverside Care", "DeKalb", ProviderStatus.Inactive,
                    NewLicense("GA-RC-700002", LicenseStatus.Expired, new(2019, 1, 1), new(2022, 12, 31))),
                now.AddDays(-90), "Provider closed; record retained for audit"),

            // Additional realistic data for listing, search and dashboard.
            NewProvider("Marietta Behavioral Health", "Cobb", ProviderStatus.Active,
                NewLicense("GA-BH-800100", LicenseStatus.Active, new(2024, 1, 1), new(2028, 12, 31)),
                NewLicense("GA-BH-800101", LicenseStatus.Active, new(2024, 1, 1), new(2027, 12, 31)),
                NewLicense("GA-BH-800102", LicenseStatus.Expired, new(2020, 1, 1), new(2022, 12, 31))),
            NewProvider("Decatur Day Services", "DeKalb", ProviderStatus.Pending),
            NewProvider("Alpharetta Rehab Partners", "Fulton", ProviderStatus.Active,
                NewLicense("GA-RP-800200", LicenseStatus.Active, new(2025, 3, 1), new(2030, 2, 28))),
            NewProvider("Lawrenceville Adult Care", "Gwinnett", ProviderStatus.Inactive,
                NewLicense("GA-AC-800300", LicenseStatus.Expired, new(2018, 5, 1), new(2021, 4, 30))),
            NewProvider("Jonesboro Community Clinic", "Clayton", ProviderStatus.Active,
                NewLicense("GA-CC-800400", LicenseStatus.Suspended, new(2023, 8, 1), new(2027, 7, 31)),
                NewLicense("GA-CC-800401", LicenseStatus.Active, new(2023, 8, 1), new(2028, 7, 31))));

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Provider NewProvider(string name, string county, ProviderStatus status, params License[] licenses) =>
        new()
        {
            ProviderName = name,
            County = county,
            Status = status,
            CreatedBy = SeedUser,
            Licenses = licenses.ToList()
        };

    private static License NewLicense(string number, LicenseStatus status, DateOnly issued, DateOnly expires) =>
        new()
        {
            LicenseNumber = number,
            LicenseStatus = status,
            IssuedDate = issued,
            ExpirationDate = expires,
            CreatedBy = SeedUser
        };

    private static Provider Deleted(Provider provider, DateTime deletedAtUtc, string reason)
    {
        provider.IsDeleted = true;
        provider.DeletedAtUtc = deletedAtUtc;
        provider.DeletedBy = SeedUser;
        provider.DeleteReason = reason;
        return provider;
    }
}
