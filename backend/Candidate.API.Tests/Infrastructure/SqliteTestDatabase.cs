using Candidate.API.Data;
using Candidate.API.Data.Interceptors;
using Candidate.API.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace Candidate.API.Tests.Infrastructure;

/// <summary>
/// A private in-memory SQLite database built from the real migrations, so CHECK constraints,
/// triggers, indexes and views are all present. Uses a fixed clock and a known user.
/// The database lives as long as the open connection, i.e. one instance per test.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    public static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public SqliteTestDatabase()
    {
        _connection.Open();
        using var db = CreateContext();
        db.Database.Migrate();
    }

    public FakeTimeProvider Clock { get; } = new(FixedNow);

    public TestCurrentUser User { get; } = new("test.user");

    public DateOnly Today => DateOnly.FromDateTime(Clock.GetUtcNow().UtcDateTime);

    /// <summary>A new context per unit of work, like a web request.</summary>
    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new SoftDeleteInterceptor(Clock, User), new AuditInterceptor(Clock, User))
            .Options);

    public SqliteConnection Connection => _connection;

    public void Dispose() => _connection.Dispose();
}

public sealed class TestCurrentUser(string userName) : ICurrentUser
{
    public string UserName { get; set; } = userName;
}
