using Candidate.API.Data.Interceptors;
using Candidate.API.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Candidate.API.Data;

public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddProvidersDatabase(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = ResolveConnectionString(
            configuration.GetConnectionString("ProvidersDb")
                ?? throw new InvalidOperationException("Connection string 'ProvidersDb' is not configured."),
            environment.ContentRootPath);

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICurrentUser, ConfiguredCurrentUser>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<AuditInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseSqlite(connectionString)
            // Order matters: deletes become updates first, then the update is audited.
            .AddInterceptors(sp.GetRequiredService<SoftDeleteInterceptor>(), sp.GetRequiredService<AuditInterceptor>()));

        return services;
    }

    /// <summary>
    /// Applies pending migrations and seeds sample data. Enabled by configuration for local
    /// development; in production, migrations would run as a controlled deployment step.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:MigrateAndSeedOnStartup"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<TimeProvider>());
    }

    // A relative Data Source is resolved against the project folder, so Visual Studio,
    // `dotnet run --project` and `dotnet ef` all use the same database file.
    private static string ResolveConnectionString(string connectionString, string contentRoot)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.DataSource)
            && builder.DataSource != ":memory:"
            && !Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.GetFullPath(Path.Combine(contentRoot, builder.DataSource));
        }

        return builder.ToString();
    }
}
