-- 04_seed_data.sql
-- Sample data that demonstrates every required scenario (same data as Data/DbSeeder.cs and the
-- committed Data/providers.db). Run after 01-03 on an empty database.
--
--   Scenario 1  active providers with valid licenses     Peachtree Health Services, DeKalb Family Care, ...
--   Scenario 2  active providers with expired licenses   Cobb Senior Living; DeKalb Family Care
--                                                        (GA-FC-200411 is still recorded 'Active' but expired
--                                                        on 2024-06-30); Marietta Behavioral Health
--   Scenario 3  soft-deleted providers (IsDeleted = 1)   Old Town Clinic, Riverside Care - kept with their
--                                                        licenses, hidden from standard views
--   Other cases                                          Gwinnett Pediatrics (no licenses), Clayton Home Health
--                                                        (inactive provider, valid license), Henry Therapy Group
--                                                        (suspended license), Forsyth Wellness Center (license
--                                                        expiring 20 days after this script runs)
--
-- Expired licenses use fixed past dates and valid ones use dates years ahead, so results stay
-- stable over time. Explicit ids keep the queries in 05_scenario_queries.sql reproducible.

BEGIN TRANSACTION;

INSERT INTO "Provider" ("ProviderId", "ProviderName", "County", "Status", "CreatedAtUtc", "CreatedBy", "ModifiedAtUtc", "ModifiedBy", "Version", "IsDeleted", "DeletedAtUtc", "DeletedBy", "DeleteReason") VALUES
    (1, 'Peachtree Health Services', 'Fulton', 'Active', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (2, 'DeKalb Family Care', 'DeKalb', 'Active', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (3, 'Cobb Senior Living', 'Cobb', 'Active', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (4, 'Gwinnett Pediatrics', 'Gwinnett', 'Active', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (5, 'Clayton Home Health', 'Clayton', 'Inactive', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (6, 'Henry Therapy Group', 'Henry', 'Pending', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (7, 'Forsyth Wellness Center', 'Forsyth', 'Active', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (8, 'Old Town Clinic', 'Fulton', 'Active', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 1, '2026-09-28 16:01:55.1844031', 'seed', 'Duplicate record of Peachtree Health Services'),
    (9, 'Riverside Care', 'DeKalb', 'Inactive', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 1, '2026-07-30 16:01:55.1844031', 'seed', 'Provider closed; record retained for audit'),
    (10, 'Marietta Behavioral Health', 'Cobb', 'Active', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (11, 'Decatur Day Services', 'DeKalb', 'Pending', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (12, 'Alpharetta Rehab Partners', 'Fulton', 'Active', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (13, 'Lawrenceville Adult Care', 'Gwinnett', 'Inactive', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (14, 'Jonesboro Community Clinic', 'Clayton', 'Active', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL);

INSERT INTO "License" ("LicenseId", "ProviderId", "LicenseNumber", "LicenseStatus", "IssuedDate", "ExpirationDate", "CreatedAtUtc", "CreatedBy", "ModifiedAtUtc", "ModifiedBy", "Version", "IsDeleted", "DeletedAtUtc", "DeletedBy", "DeleteReason") VALUES
    (1, 1, 'GA-HS-100231', 'Active', '2023-01-15', '2028-01-14', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (2, 1, 'GA-HS-100232', 'Active', '2024-06-01', '2029-05-31', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (3, 2, 'GA-FC-200410', 'Active', '2023-03-01', '2028-02-29', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (4, 2, 'GA-FC-200411', 'Active', '2021-07-01', '2024-06-30', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (5, 3, 'GA-SL-300077', 'Expired', '2020-09-01', '2023-08-31', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (6, 5, 'GA-HH-400120', 'Active', '2024-02-01', '2029-01-31', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (7, 6, 'GA-TG-500300', 'Suspended', '2024-04-01', '2028-03-31', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (8, 7, 'GA-WC-600015', 'Active', date('now', '-3 years'), date('now', '+20 days'), '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (9, 8, 'GA-OT-700001', 'Active', '2023-05-01', '2028-04-30', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (10, 9, 'GA-RC-700002', 'Expired', '2019-01-01', '2022-12-31', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (11, 10, 'GA-BH-800100', 'Active', '2024-01-01', '2028-12-31', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (12, 10, 'GA-BH-800101', 'Active', '2024-01-01', '2027-12-31', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (13, 10, 'GA-BH-800102', 'Expired', '2020-01-01', '2022-12-31', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (14, 12, 'GA-RP-800200', 'Active', '2025-03-01', '2030-02-28', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (15, 13, 'GA-AC-800300', 'Expired', '2018-05-01', '2021-04-30', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (16, 14, 'GA-CC-800400', 'Suspended', '2023-08-01', '2027-07-31', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL),
    (17, 14, 'GA-CC-800401', 'Active', '2023-08-01', '2028-07-31', '2026-10-28 16:01:55.3706912', 'seed', NULL, NULL, 1, 0, NULL, NULL, NULL);

COMMIT;
