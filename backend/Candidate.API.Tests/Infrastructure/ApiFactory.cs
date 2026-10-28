using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Candidate.API.Tests.Infrastructure;

/// <summary>
/// Hosts the real API in memory against its own temporary SQLite file, migrated and seeded
/// at startup exactly as in Development. One database per test class (xUnit class fixture).
/// The real clock is used so API results can be compared with the SQL views (date('now')).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"providers-tests-{Guid.NewGuid():N}.db");

    public string ConnectionString => $"Data Source={_databasePath}";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ProvidersDb", ConnectionString);
        builder.UseSetting("Database:MigrateAndSeedOnStartup", "true");
        builder.UseSetting("Audit:DefaultUser", "api.test");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // Pooled connections keep the file open on Windows.
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            File.Delete(_databasePath + suffix);
        }
    }
}
