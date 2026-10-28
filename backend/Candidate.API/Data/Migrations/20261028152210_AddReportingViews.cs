using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Candidate.API.Data.Migrations
{
    /// <summary>
    /// Views for SQL-level consumers (support staff, reports). The API applies the same rules
    /// through EF global query filters and Domain/LicenseRules.cs; keep the two in sync.
    /// "Today" is date('now'), the UTC date, matching the API's TimeProvider.GetUtcNow().
    /// </summary>
    public partial class AddReportingViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Standard (non-deleted) providers: the soft-delete rule in one place for SQL users.
            migrationBuilder.Sql("""
                CREATE VIEW vw_Provider_Current AS
                SELECT ProviderId, ProviderName, County, Status,
                       CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy, Version
                FROM Provider
                WHERE IsDeleted = 0;
                """);

            // Standard licenses (license and its provider not deleted) with computed validity.
            migrationBuilder.Sql("""
                CREATE VIEW vw_License_Current AS
                SELECT l.LicenseId, l.ProviderId, l.LicenseNumber, l.LicenseStatus,
                       l.IssuedDate, l.ExpirationDate,
                       CASE WHEN l.ExpirationDate < date('now') THEN 1 ELSE 0 END AS IsExpired,
                       CASE WHEN l.LicenseStatus = 'Active' AND l.ExpirationDate >= date('now')
                            THEN 1 ELSE 0 END AS IsCurrentlyValid,
                       CASE WHEN l.LicenseStatus = 'Suspended' THEN 'Suspended'
                            WHEN l.ExpirationDate < date('now') THEN 'Expired'
                            ELSE l.LicenseStatus END AS EffectiveStatus
                FROM License l
                JOIN Provider p ON p.ProviderId = l.ProviderId
                WHERE l.IsDeleted = 0 AND p.IsDeleted = 0;
                """);

            // Scenario 1: active providers and their currently valid licenses.
            migrationBuilder.Sql("""
                CREATE VIEW vw_ActiveProviders_ActiveLicenses AS
                SELECT p.ProviderId, p.ProviderName, p.County, p.Status AS ProviderStatus,
                       l.LicenseId, l.LicenseNumber, l.LicenseStatus, l.ExpirationDate
                FROM vw_Provider_Current p
                JOIN vw_License_Current l ON l.ProviderId = p.ProviderId
                WHERE p.Status = 'Active' AND l.IsCurrentlyValid = 1;
                """);

            // Scenario 2: providers that appear active but hold expired licenses (by date).
            migrationBuilder.Sql("""
                CREATE VIEW vw_ActiveProviders_ExpiredLicenses AS
                SELECT p.ProviderId, p.ProviderName, p.County, p.Status AS ProviderStatus,
                       l.LicenseId, l.LicenseNumber, l.LicenseStatus, l.ExpirationDate
                FROM vw_Provider_Current p
                JOIN vw_License_Current l ON l.ProviderId = p.ProviderId
                WHERE p.Status = 'Active' AND l.IsExpired = 1;
                """);

            // Scenario 3 (audit side): soft-deleted providers remain queryable.
            migrationBuilder.Sql("""
                CREATE VIEW vw_Provider_Deleted AS
                SELECT ProviderId, ProviderName, County, Status,
                       CreatedAtUtc, CreatedBy, DeletedAtUtc, DeletedBy, DeleteReason
                FROM Provider
                WHERE IsDeleted = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_Provider_Deleted;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ActiveProviders_ExpiredLicenses;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ActiveProviders_ActiveLicenses;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_License_Current;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_Provider_Current;");
        }
    }
}
