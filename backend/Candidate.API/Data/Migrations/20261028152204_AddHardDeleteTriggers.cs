using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Candidate.API.Data.Migrations
{
    /// <summary>
    /// Database-level guard for the "no hard deletes" rule. EF deletes are already converted by
    /// SoftDeleteInterceptor; these triggers also stop raw SQL, DB tools and other applications.
    /// </summary>
    public partial class AddHardDeleteTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_Provider_PreventHardDelete
                BEFORE DELETE ON Provider
                BEGIN
                    SELECT RAISE(ABORT, 'Hard delete of Provider is not allowed. Use soft delete (IsDeleted = 1).');
                END;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER TR_License_PreventHardDelete
                BEFORE DELETE ON License
                BEGIN
                    SELECT RAISE(ABORT, 'Hard delete of License is not allowed. Use soft delete (IsDeleted = 1).');
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_License_PreventHardDelete;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Provider_PreventHardDelete;");
        }
    }
}
