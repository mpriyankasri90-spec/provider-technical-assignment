using Candidate.API.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Candidate.API.Data.Configurations;

public class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> builder)
    {
        builder.ToTable("License", t =>
        {
            t.HasCheckConstraint("CK_License_LicenseStatus", AuditSoftDeleteConfiguration.EnumCheck<LicenseStatus>("LicenseStatus"));
            t.HasCheckConstraint("CK_License_LicenseNumber_NotBlank", "length(trim(LicenseNumber)) > 0");
            t.HasCheckConstraint("CK_License_IssuedBeforeExpiration", "IssuedDate IS NULL OR IssuedDate <= ExpirationDate");
        });

        builder.HasKey(l => l.LicenseId);

        builder.Property(l => l.LicenseNumber).IsRequired().HasMaxLength(50).UseCollation("NOCASE");
        builder.Property(l => l.LicenseStatus).IsRequired().HasMaxLength(20).HasConversion<string>();
        builder.Property(l => l.ExpirationDate).IsRequired();

        builder.ConfigureAuditAndSoftDelete("License");

        // Unique per provider among live rows only: a soft-deleted license number can be reissued
        // without the deleted row (kept for audit) blocking it.
        builder.HasIndex(l => new { l.ProviderId, l.LicenseNumber })
            .IsUnique()
            .HasFilter("IsDeleted = 0")
            .HasDatabaseName("UX_License_ProviderId_LicenseNumber");

        builder.HasIndex(l => l.ProviderId).HasDatabaseName("IX_License_ProviderId");
        builder.HasIndex(l => l.ExpirationDate).HasDatabaseName("IX_License_ExpirationDate");
    }
}
