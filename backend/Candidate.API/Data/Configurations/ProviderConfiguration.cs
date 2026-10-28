using Candidate.API.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Candidate.API.Data.Configurations;

public class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("Provider", t =>
        {
            t.HasCheckConstraint("CK_Provider_Status", AuditSoftDeleteConfiguration.EnumCheck<ProviderStatus>("Status"));
            t.HasCheckConstraint("CK_Provider_ProviderName_NotBlank", "length(trim(ProviderName)) > 0");
            t.HasCheckConstraint("CK_Provider_County_NotBlank", "length(trim(County)) > 0");
        });

        builder.HasKey(p => p.ProviderId);

        builder.Property(p => p.ProviderName).IsRequired().HasMaxLength(200).UseCollation("NOCASE");
        builder.Property(p => p.County).IsRequired().HasMaxLength(100).UseCollation("NOCASE");
        builder.Property(p => p.Status).IsRequired().HasMaxLength(20).HasConversion<string>();

        builder.ConfigureAuditAndSoftDelete("Provider");

        // Standard listing filters on IsDeleted (+ Status); search and sort use ProviderName.
        builder.HasIndex(p => new { p.IsDeleted, p.Status }).HasDatabaseName("IX_Provider_IsDeleted_Status");
        builder.HasIndex(p => p.ProviderName).HasDatabaseName("IX_Provider_ProviderName");

        // Restrict: hard deletes are forbidden, so nothing may cascade.
        builder.HasMany(p => p.Licenses)
            .WithOne(l => l.Provider)
            .HasForeignKey(l => l.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
