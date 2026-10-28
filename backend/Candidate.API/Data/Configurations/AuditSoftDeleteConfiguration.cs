using Candidate.API.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Candidate.API.Data.Configurations;

/// <summary>Column rules shared by every audited, soft-deletable table.</summary>
internal static class AuditSoftDeleteConfiguration
{
    public static void ConfigureAuditAndSoftDelete<T>(this EntityTypeBuilder<T> builder, string tableName)
        where T : class, IAuditable, ISoftDeletable
    {
        builder.Property(e => e.CreatedAtUtc).IsRequired();
        builder.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ModifiedBy).HasMaxLength(100);
        builder.Property(e => e.Version).IsRequired().IsConcurrencyToken().HasDefaultValue(1);

        // DEFAULT 0 lets a raw SQL insert (e.g. a support script) create a visible row.
        builder.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.DeletedBy).HasMaxLength(100);
        builder.Property(e => e.DeleteReason).HasMaxLength(500);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint($"CK_{tableName}_IsDeleted", "IsDeleted IN (0, 1)");
            // Flag and timestamp must agree, so "deleted" has exactly one meaning.
            t.HasCheckConstraint(
                $"CK_{tableName}_SoftDeleteConsistency",
                "(IsDeleted = 0 AND DeletedAtUtc IS NULL) OR (IsDeleted = 1 AND DeletedAtUtc IS NOT NULL)");
            // Created by the migration; declared so EF knows the table has triggers.
            t.HasTrigger($"TR_{tableName}_PreventHardDelete");
        });
    }

    /// <summary>Builds a CHECK constraint from the enum, so C# and the database cannot drift.</summary>
    public static string EnumCheck<TEnum>(string column) where TEnum : struct, Enum =>
        $"{column} IN ({string.Join(", ", Enum.GetNames<TEnum>().Select(n => $"'{n}'"))})";
}
