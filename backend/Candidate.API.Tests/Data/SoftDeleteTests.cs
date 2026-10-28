using Candidate.API.Domain;
using Candidate.API.Tests.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using static Candidate.API.Tests.Data.TestData;

namespace Candidate.API.Tests.Data;

/// <summary>Each layer of soft-delete enforcement, tested against real SQLite.</summary>
public sealed class SoftDeleteTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Removing_a_provider_soft_deletes_it_and_keeps_the_row()
    {
        int id;
        await using (var db = _database.CreateContext())
        {
            id = (await AddAsync(db, NewProvider("Acme Care"))).ProviderId;
        }

        await using (var db = _database.CreateContext())
        {
            var provider = await db.Providers.SingleAsync(p => p.ProviderId == id);
            db.Providers.Remove(provider);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var row = await db.Providers.IgnoreQueryFilters().SingleAsync(p => p.ProviderId == id);
            Assert.True(row.IsDeleted);
            Assert.Equal(_database.Clock.GetUtcNow().UtcDateTime, row.DeletedAtUtc);
            Assert.Equal("test.user", row.DeletedBy);
            Assert.Equal(2, row.Version);
        }
    }

    [Fact]
    public async Task Soft_deleted_providers_are_hidden_from_standard_queries_but_remain_queryable()
    {
        await using (var db = _database.CreateContext())
        {
            await AddAsync(db, NewProvider("Visible Care"));
            var hidden = await AddAsync(db, NewProvider("Hidden Care"));
            db.Providers.Remove(hidden);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            Assert.Equal(["Visible Care"], await db.Providers.Select(p => p.ProviderName).ToListAsync());
            Assert.Equal(2, await db.Providers.IgnoreQueryFilters().CountAsync());
        }
    }

    [Fact]
    public async Task Licenses_of_a_soft_deleted_provider_are_hidden_without_being_modified()
    {
        await using (var db = _database.CreateContext())
        {
            var provider = await AddAsync(db, NewProvider("Closing Care", licenses: NewLicense("L-1", new(2030, 1, 1))));
            db.Providers.Remove(provider);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            Assert.Empty(await db.Licenses.ToListAsync());

            var license = await db.Licenses.IgnoreQueryFilters().SingleAsync();
            Assert.False(license.IsDeleted);
            Assert.Equal(1, license.Version);
        }
    }

    [Theory]
    [InlineData("DELETE FROM Provider")]
    [InlineData("DELETE FROM License")]
    public async Task Raw_SQL_hard_deletes_are_rejected_by_triggers(string sql)
    {
        await using var db = _database.CreateContext();
        await AddAsync(db, NewProvider("Guarded Care", licenses: NewLicense("L-1", new(2030, 1, 1))));

        var error = await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync(sql));

        Assert.Contains("Hard delete", error.Message);
        Assert.Equal(1, await db.Providers.IgnoreQueryFilters().CountAsync());
        Assert.Equal(1, await db.Licenses.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Bulk_ExecuteDelete_which_bypasses_interceptors_is_still_rejected()
    {
        await using var db = _database.CreateContext();
        await AddAsync(db, NewProvider("Bulk Care"));

        await Assert.ThrowsAsync<SqliteException>(() => db.Providers.ExecuteDeleteAsync());
        Assert.Equal(1, await db.Providers.CountAsync());
    }

    [Fact]
    public async Task Marking_deleted_without_a_timestamp_violates_the_consistency_constraint()
    {
        await using var db = _database.CreateContext();
        var provider = await AddAsync(db, NewProvider("Half Deleted Care"));

        var error = await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync(
            "UPDATE Provider SET IsDeleted = 1 WHERE ProviderId = {0}", provider.ProviderId));

        Assert.Contains("CK_Provider_SoftDeleteConsistency", error.Message);
    }

    [Fact]
    public async Task A_soft_deleted_license_number_can_be_reissued_but_live_duplicates_are_rejected()
    {
        await using var db = _database.CreateContext();
        var provider = await AddAsync(db, NewProvider("Reissue Care", licenses: NewLicense("GA-1", new(2024, 1, 1))));

        db.Licenses.Remove(provider.Licenses[0]);
        await db.SaveChangesAsync();

        provider.Licenses.Add(NewLicense("GA-1", new(2030, 1, 1)));
        await db.SaveChangesAsync();

        provider.Licenses.Add(NewLicense("ga-1", new(2031, 1, 1)));
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains("UNIQUE", error.InnerException!.Message);
    }
}
