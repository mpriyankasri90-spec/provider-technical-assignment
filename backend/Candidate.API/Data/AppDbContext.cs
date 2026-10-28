using Candidate.API.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Candidate.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<License> Licenses => Set<License>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Soft-delete enforcement for every standard query. Audit/maintenance code opts out
        // explicitly with IgnoreQueryFilters(), which is confined to AuditService.
        // A license is hidden when it is deleted itself OR its provider is deleted, so restoring
        // a provider brings its licenses back unchanged.
        modelBuilder.Entity<Provider>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<License>().HasQueryFilter(l => !l.IsDeleted && !l.Provider.IsDeleted);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite has no datetime type; values come back as DateTimeKind.Unspecified.
        // All timestamps are written as UTC, so mark them as UTC when read.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}
