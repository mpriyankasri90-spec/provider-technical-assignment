-- 02_hard_delete_triggers.sql
-- Database-level guard for "hard deletion is not allowed": any DELETE on Provider or License
-- is aborted, whether it comes from the API, raw SQL, a DB tool or another application.
--
-- Generated from EF Core migration AddHardDeleteTriggers:
--   dotnet ef migrations script InitialSchema AddHardDeleteTriggers --project backend/Candidate.API

BEGIN TRANSACTION;

CREATE TRIGGER TR_Provider_PreventHardDelete
BEFORE DELETE ON Provider
BEGIN
    SELECT RAISE(ABORT, 'Hard delete of Provider is not allowed. Use soft delete (IsDeleted = 1).');
END;

CREATE TRIGGER TR_License_PreventHardDelete
BEFORE DELETE ON License
BEGIN
    SELECT RAISE(ABORT, 'Hard delete of License is not allowed. Use soft delete (IsDeleted = 1).');
END;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261028152204_AddHardDeleteTriggers', '8.0.32');

COMMIT;
