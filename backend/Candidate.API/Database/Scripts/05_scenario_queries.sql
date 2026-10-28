-- 05_scenario_queries.sql
-- Queries for the required data scenarios, plus audit and maintenance queries for production
-- support. Each scenario is shown through its view and as the equivalent query on the base
-- tables. The API returns the same rows:
--   GET /api/reports/active-providers-active-licenses    (scenario 1)
--   GET /api/reports/active-providers-expired-licenses   (scenario 2)
--   GET /api/providers, GET /api/audit/providers/deleted (scenario 3)
--
-- Rules (same as backend/Candidate.API/Domain/LicenseRules.cs):
--   expired         = ExpirationDate < today, whatever the recorded LicenseStatus says
--   currently valid = LicenseStatus = 'Active' AND ExpirationDate >= today
--   provider status never decides license validity
-- "Today" is date('now') (UTC). Dates are stored as ISO text, so text comparison is date order.


-- =============================================================================================
-- Scenario 1: active providers and their active (currently valid) licenses
-- =============================================================================================

SELECT ProviderName, County, LicenseNumber, LicenseStatus, ExpirationDate
FROM vw_ActiveProviders_ActiveLicenses
ORDER BY ProviderName, ExpirationDate;

-- Equivalent on the base tables:
SELECT p.ProviderName, p.County, l.LicenseNumber, l.LicenseStatus, l.ExpirationDate
FROM Provider p
JOIN License l ON l.ProviderId = p.ProviderId
WHERE p.IsDeleted = 0
  AND l.IsDeleted = 0
  AND p.Status = 'Active'
  AND l.LicenseStatus = 'Active'
  AND l.ExpirationDate >= date('now')
ORDER BY p.ProviderName, l.ExpirationDate;


-- =============================================================================================
-- Scenario 2: providers appearing active but having expired licenses
-- =============================================================================================

SELECT ProviderName, County, LicenseNumber, LicenseStatus AS RecordedStatus, ExpirationDate
FROM vw_ActiveProviders_ExpiredLicenses
ORDER BY ProviderName, ExpirationDate;

-- Equivalent on the base tables (expiration is decided by the date, not by LicenseStatus):
SELECT p.ProviderName, p.County, l.LicenseNumber, l.LicenseStatus AS RecordedStatus, l.ExpirationDate,
       CAST(julianday(date('now')) - julianday(l.ExpirationDate) AS INTEGER) AS DaysSinceExpiry
FROM Provider p
JOIN License l ON l.ProviderId = p.ProviderId
WHERE p.IsDeleted = 0
  AND l.IsDeleted = 0
  AND p.Status = 'Active'
  AND l.ExpirationDate < date('now')
ORDER BY p.ProviderName, l.ExpirationDate;

-- One row per provider, for follow-up lists:
SELECT p.ProviderId, p.ProviderName, p.County, COUNT(*) AS ExpiredLicenses, MIN(l.ExpirationDate) AS OldestExpiry
FROM vw_Provider_Current p
JOIN vw_License_Current l ON l.ProviderId = p.ProviderId
WHERE p.Status = 'Active' AND l.IsExpired = 1
GROUP BY p.ProviderId, p.ProviderName, p.County
ORDER BY OldestExpiry;


-- =============================================================================================
-- Scenario 3: soft-deleted providers are excluded from standard results
-- =============================================================================================

-- Standard result: only non-deleted providers (Old Town Clinic and Riverside Care are absent).
SELECT ProviderId, ProviderName, County, Status
FROM vw_Provider_Current
ORDER BY ProviderName;

-- The deleted rows are still in the table, with who deleted them, when and why (audit).
SELECT ProviderId, ProviderName, County, Status, DeletedAtUtc, DeletedBy, DeleteReason
FROM vw_Provider_Deleted
ORDER BY DeletedAtUtc DESC;

-- Their licenses are retained unchanged; they are hidden because the provider is deleted.
SELECT p.ProviderName, l.LicenseNumber, l.LicenseStatus, l.ExpirationDate, l.IsDeleted AS LicenseIsDeleted
FROM Provider p
JOIN License l ON l.ProviderId = p.ProviderId
WHERE p.IsDeleted = 1;

-- Reconciliation: every provider is either current or deleted, never missing.
SELECT (SELECT COUNT(*) FROM Provider)            AS AllRows,
       (SELECT COUNT(*) FROM vw_Provider_Current) AS CurrentRows,
       (SELECT COUNT(*) FROM vw_Provider_Deleted) AS DeletedRows;


-- =============================================================================================
-- Production support
-- =============================================================================================

-- Data quality: licenses still recorded as 'Active' after their expiration date.
SELECT p.ProviderName, l.LicenseNumber, l.LicenseStatus AS RecordedStatus, l.ExpirationDate
FROM vw_License_Current l
JOIN vw_Provider_Current p ON p.ProviderId = l.ProviderId
WHERE l.LicenseStatus = 'Active' AND l.IsExpired = 1;

-- Licenses expiring in the next 60 days.
SELECT p.ProviderName, p.Status AS ProviderStatus, l.LicenseNumber, l.ExpirationDate
FROM vw_License_Current l
JOIN vw_Provider_Current p ON p.ProviderId = l.ProviderId
WHERE l.IsCurrentlyValid = 1 AND l.ExpirationDate <= date('now', '+60 days')
ORDER BY l.ExpirationDate;

-- Recently changed providers (audit columns are maintained by the API).
SELECT ProviderId, ProviderName, Version, ModifiedAtUtc, ModifiedBy
FROM Provider
WHERE ModifiedAtUtc IS NOT NULL
ORDER BY ModifiedAtUtc DESC;


-- =============================================================================================
-- Maintenance templates (commented out; replace :ProviderId and :User before running)
-- =============================================================================================

-- Soft delete from SQL. Hard deletes are blocked by TR_Provider_PreventHardDelete, and the
-- CK_Provider_SoftDeleteConsistency constraint requires DeletedAtUtc whenever IsDeleted = 1.
-- UPDATE Provider
-- SET IsDeleted = 1,
--     DeletedAtUtc = strftime('%Y-%m-%d %H:%M:%f', 'now'),
--     DeletedBy = :User,
--     DeleteReason = 'Reason for deletion',
--     Version = Version + 1
-- WHERE ProviderId = :ProviderId AND IsDeleted = 0;

-- Restore a soft-deleted provider (the API equivalent is POST /api/audit/providers/{id}/restore).
-- UPDATE Provider
-- SET IsDeleted = 0, DeletedAtUtc = NULL, DeletedBy = NULL, DeleteReason = NULL,
--     ModifiedAtUtc = strftime('%Y-%m-%d %H:%M:%f', 'now'), ModifiedBy = :User,
--     Version = Version + 1
-- WHERE ProviderId = :ProviderId AND IsDeleted = 1;

-- This fails by design:
-- DELETE FROM Provider WHERE ProviderId = :ProviderId;
--   -> Hard delete of Provider is not allowed. Use soft delete (IsDeleted = 1).
