using Candidate.API.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static Candidate.API.Tests.Data.TestData;

namespace Candidate.API.Tests.Data;

public sealed class AuditAndConcurrencyTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Insert_stamps_creation_audit_fields_on_the_server()
    {
        await using var db = _database.CreateContext();

        var provider = await AddAsync(db, NewProvider("New Care"));

        Assert.Equal(SqliteTestDatabase.FixedNow.UtcDateTime, provider.CreatedAtUtc);
        Assert.Equal("test.user", provider.CreatedBy);
        Assert.Null(provider.ModifiedAtUtc);
        Assert.Equal(1, provider.Version);
    }

    [Fact]
    public async Task Update_stamps_modification_fields_increments_version_and_protects_creation_fields()
    {
        int id;
        await using (var db = _database.CreateContext())
        {
            id = (await AddAsync(db, NewProvider("Old Name"))).ProviderId;
        }

        _database.Clock.Advance(TimeSpan.FromHours(2));
        _database.User.UserName = "editor";

        await using (var db = _database.CreateContext())
        {
            var provider = await db.Providers.SingleAsync(p => p.ProviderId == id);
            provider.ProviderName = "New Name";
            provider.CreatedBy = "tampered";
            provider.CreatedAtUtc = DateTime.UnixEpoch;
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var provider = await db.Providers.SingleAsync(p => p.ProviderId == id);
            Assert.Equal("New Name", provider.ProviderName);
            Assert.Equal(2, provider.Version);
            Assert.Equal("editor", provider.ModifiedBy);
            Assert.Equal(SqliteTestDatabase.FixedNow.UtcDateTime.AddHours(2), provider.ModifiedAtUtc);
            Assert.Equal("test.user", provider.CreatedBy);
            Assert.Equal(SqliteTestDatabase.FixedNow.UtcDateTime, provider.CreatedAtUtc);
        }
    }

    [Fact]
    public async Task Concurrent_updates_are_detected_by_the_version_token()
    {
        int id;
        await using (var db = _database.CreateContext())
        {
            id = (await AddAsync(db, NewProvider("Shared Care"))).ProviderId;
        }

        await using var firstUser = _database.CreateContext();
        await using var secondUser = _database.CreateContext();
        var first = await firstUser.Providers.SingleAsync(p => p.ProviderId == id);
        var second = await secondUser.Providers.SingleAsync(p => p.ProviderId == id);

        first.County = "Cobb";
        await firstUser.SaveChangesAsync();

        second.County = "DeKalb";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondUser.SaveChangesAsync());
    }
}
