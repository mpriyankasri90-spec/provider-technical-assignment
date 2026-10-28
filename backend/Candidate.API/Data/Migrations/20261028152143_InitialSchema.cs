using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Candidate.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Provider",
                columns: table => new
                {
                    ProviderId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProviderName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    County = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE"),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DeleteReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provider", x => x.ProviderId);
                    table.CheckConstraint("CK_Provider_County_NotBlank", "length(trim(County)) > 0");
                    table.CheckConstraint("CK_Provider_IsDeleted", "IsDeleted IN (0, 1)");
                    table.CheckConstraint("CK_Provider_ProviderName_NotBlank", "length(trim(ProviderName)) > 0");
                    table.CheckConstraint("CK_Provider_SoftDeleteConsistency", "(IsDeleted = 0 AND DeletedAtUtc IS NULL) OR (IsDeleted = 1 AND DeletedAtUtc IS NOT NULL)");
                    table.CheckConstraint("CK_Provider_Status", "Status IN ('Active', 'Inactive', 'Pending')");
                });

            migrationBuilder.CreateTable(
                name: "License",
                columns: table => new
                {
                    LicenseId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProviderId = table.Column<int>(type: "INTEGER", nullable: false),
                    LicenseNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    LicenseStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IssuedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ExpirationDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DeleteReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_License", x => x.LicenseId);
                    table.CheckConstraint("CK_License_IsDeleted", "IsDeleted IN (0, 1)");
                    table.CheckConstraint("CK_License_IssuedBeforeExpiration", "IssuedDate IS NULL OR IssuedDate <= ExpirationDate");
                    table.CheckConstraint("CK_License_LicenseNumber_NotBlank", "length(trim(LicenseNumber)) > 0");
                    table.CheckConstraint("CK_License_LicenseStatus", "LicenseStatus IN ('Active', 'Expired', 'Suspended')");
                    table.CheckConstraint("CK_License_SoftDeleteConsistency", "(IsDeleted = 0 AND DeletedAtUtc IS NULL) OR (IsDeleted = 1 AND DeletedAtUtc IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_License_Provider_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Provider",
                        principalColumn: "ProviderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_License_ExpirationDate",
                table: "License",
                column: "ExpirationDate");

            migrationBuilder.CreateIndex(
                name: "IX_License_ProviderId",
                table: "License",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "UX_License_ProviderId_LicenseNumber",
                table: "License",
                columns: new[] { "ProviderId", "LicenseNumber" },
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Provider_IsDeleted_Status",
                table: "Provider",
                columns: new[] { "IsDeleted", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Provider_ProviderName",
                table: "Provider",
                column: "ProviderName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "License");

            migrationBuilder.DropTable(
                name: "Provider");
        }
    }
}
