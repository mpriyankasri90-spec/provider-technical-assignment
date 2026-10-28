-- 01_create_tables.sql
-- Creates the Provider and License tables: audit columns, soft-delete columns, Version
-- (optimistic concurrency), CHECK constraints, the foreign key (no cascade) and indexes,
-- including the partial unique index on (ProviderId, LicenseNumber) for non-deleted licenses.
--
-- Generated from EF Core migration InitialSchema:
--   dotnet ef migrations script 0 InitialSchema --project backend/Candidate.API
-- Run order on an empty database: 01 -> 02 -> 03 -> 04 (sample data) -> 05 (queries).
-- The API applies the same migrations automatically at startup in Development.

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "Provider" (
    "ProviderId" INTEGER NOT NULL CONSTRAINT "PK_Provider" PRIMARY KEY AUTOINCREMENT,
    "ProviderName" TEXT COLLATE NOCASE NOT NULL,
    "County" TEXT COLLATE NOCASE NOT NULL,
    "Status" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    "CreatedBy" TEXT NOT NULL,
    "ModifiedAtUtc" TEXT NULL,
    "ModifiedBy" TEXT NULL,
    "Version" INTEGER NOT NULL DEFAULT 1,
    "IsDeleted" INTEGER NOT NULL DEFAULT 0,
    "DeletedAtUtc" TEXT NULL,
    "DeletedBy" TEXT NULL,
    "DeleteReason" TEXT NULL,
    CONSTRAINT "CK_Provider_County_NotBlank" CHECK (length(trim(County)) > 0),
    CONSTRAINT "CK_Provider_IsDeleted" CHECK (IsDeleted IN (0, 1)),
    CONSTRAINT "CK_Provider_ProviderName_NotBlank" CHECK (length(trim(ProviderName)) > 0),
    CONSTRAINT "CK_Provider_SoftDeleteConsistency" CHECK ((IsDeleted = 0 AND DeletedAtUtc IS NULL) OR (IsDeleted = 1 AND DeletedAtUtc IS NOT NULL)),
    CONSTRAINT "CK_Provider_Status" CHECK (Status IN ('Active', 'Inactive', 'Pending'))
);

CREATE TABLE "License" (
    "LicenseId" INTEGER NOT NULL CONSTRAINT "PK_License" PRIMARY KEY AUTOINCREMENT,
    "ProviderId" INTEGER NOT NULL,
    "LicenseNumber" TEXT COLLATE NOCASE NOT NULL,
    "LicenseStatus" TEXT NOT NULL,
    "IssuedDate" TEXT NULL,
    "ExpirationDate" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    "CreatedBy" TEXT NOT NULL,
    "ModifiedAtUtc" TEXT NULL,
    "ModifiedBy" TEXT NULL,
    "Version" INTEGER NOT NULL DEFAULT 1,
    "IsDeleted" INTEGER NOT NULL DEFAULT 0,
    "DeletedAtUtc" TEXT NULL,
    "DeletedBy" TEXT NULL,
    "DeleteReason" TEXT NULL,
    CONSTRAINT "CK_License_IsDeleted" CHECK (IsDeleted IN (0, 1)),
    CONSTRAINT "CK_License_IssuedBeforeExpiration" CHECK (IssuedDate IS NULL OR IssuedDate <= ExpirationDate),
    CONSTRAINT "CK_License_LicenseNumber_NotBlank" CHECK (length(trim(LicenseNumber)) > 0),
    CONSTRAINT "CK_License_LicenseStatus" CHECK (LicenseStatus IN ('Active', 'Expired', 'Suspended')),
    CONSTRAINT "CK_License_SoftDeleteConsistency" CHECK ((IsDeleted = 0 AND DeletedAtUtc IS NULL) OR (IsDeleted = 1 AND DeletedAtUtc IS NOT NULL)),
    CONSTRAINT "FK_License_Provider_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES "Provider" ("ProviderId") ON DELETE RESTRICT
);

CREATE INDEX "IX_License_ExpirationDate" ON "License" ("ExpirationDate");

CREATE INDEX "IX_License_ProviderId" ON "License" ("ProviderId");

CREATE UNIQUE INDEX "UX_License_ProviderId_LicenseNumber" ON "License" ("ProviderId", "LicenseNumber") WHERE IsDeleted = 0;

CREATE INDEX "IX_Provider_IsDeleted_Status" ON "Provider" ("IsDeleted", "Status");

CREATE INDEX "IX_Provider_ProviderName" ON "Provider" ("ProviderName");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261028152143_InitialSchema', '8.0.32');

COMMIT;
