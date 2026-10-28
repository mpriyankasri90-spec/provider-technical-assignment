-- 03_reporting_views.sql
-- Views for SQL-level consumers (support staff, reports). They apply the same rules as the API:
--   vw_Provider_Current                 non-deleted providers (standard results)
--   vw_License_Current                  non-deleted licenses of non-deleted providers, with
--                                       IsExpired / IsCurrentlyValid / EffectiveStatus
--   vw_ActiveProviders_ActiveLicenses   scenario 1
--   vw_ActiveProviders_ExpiredLicenses  scenario 2
--   vw_Provider_Deleted                 soft-deleted providers, for audit
-- "Today" is date('now'), the UTC date, matching the API.
--
-- Generated from EF Core migration AddReportingViews:
--   dotnet ef migrations script AddHardDeleteTriggers AddReportingViews --project backend/Candidate.API

BEGIN TRANSACTION;

CREATE VIEW vw_Provider_Current AS
SELECT ProviderId, ProviderName, County, Status,
       CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy, Version
FROM Provider
WHERE IsDeleted = 0;

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

CREATE VIEW vw_ActiveProviders_ActiveLicenses AS
SELECT p.ProviderId, p.ProviderName, p.County, p.Status AS ProviderStatus,
       l.LicenseId, l.LicenseNumber, l.LicenseStatus, l.ExpirationDate
FROM vw_Provider_Current p
JOIN vw_License_Current l ON l.ProviderId = p.ProviderId
WHERE p.Status = 'Active' AND l.IsCurrentlyValid = 1;

CREATE VIEW vw_ActiveProviders_ExpiredLicenses AS
SELECT p.ProviderId, p.ProviderName, p.County, p.Status AS ProviderStatus,
       l.LicenseId, l.LicenseNumber, l.LicenseStatus, l.ExpirationDate
FROM vw_Provider_Current p
JOIN vw_License_Current l ON l.ProviderId = p.ProviderId
WHERE p.Status = 'Active' AND l.IsExpired = 1;

CREATE VIEW vw_Provider_Deleted AS
SELECT ProviderId, ProviderName, County, Status,
       CreatedAtUtc, CreatedBy, DeletedAtUtc, DeletedBy, DeleteReason
FROM Provider
WHERE IsDeleted = 1;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261028152210_AddReportingViews', '8.0.32');

COMMIT;
