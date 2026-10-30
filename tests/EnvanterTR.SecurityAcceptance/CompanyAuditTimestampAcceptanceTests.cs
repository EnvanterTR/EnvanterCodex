using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class CompanyAuditTimestampAcceptanceTests
{
    [Fact]
    public async Task Company_audit_upgrade_preserves_unknown_history_and_stamps_new_writes()
    {
        var connection = Environment.GetEnvironmentVariable("ENVANTERTR_SECURITY_TEST_CONNECTION");
        Assert.False(string.IsNullOrWhiteSpace(connection));
        var schema = "auditcheck_" + Guid.NewGuid().ToString("N");
        await using var control = new NpgsqlConnection(connection);
        await control.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA {schema}", control))
            await create.ExecuteNonQueryAsync();
        try
        {
            var isolated = new NpgsqlConnectionStringBuilder(connection) { SearchPath = schema };
            var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(isolated.ConnectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", schema)).Options;
            await using var db = new AppDbContext(options);
            await db.Database.MigrateAsync("20261026083058_AddHandoverEmailAttempts");
            var company = Guid.NewGuid();
            var oldCategory = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Companies" ("Id", "Name", "IsActive", "CreatedAt", "BrandPrimaryColor",
                    "SecurityPasswordMinimumLength", "SecuritySessionDurationHours", "SecurityRequireMfa", "CustomerLifecycleStage", "AssetCount")
                VALUES ({company}, 'Audit preservation company', TRUE, now(), '#1769E0', 12, 720, FALSE, 'Unclassified', 0)
                """);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "AssetCategories" ("Id", "CompanyId", "Name")
                VALUES ({oldCategory}, {company}, 'Historical category')
                """);
            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();
            Assert.False(db.Database.HasPendingModelChanges());
            foreach (var entity in db.Model.GetEntityTypes().Where(e => e.BaseType is null &&
                         (e.ClrType == typeof(Company) || e.FindProperty("CompanyId")?.ClrType is { } type &&
                             (type == typeof(Guid) || type == typeof(Guid?)))))
            {
                Assert.NotNull(entity.FindProperty("CreatedAt"));
                Assert.NotNull(entity.FindProperty("UpdatedAt"));
            }
            db.UseCompanyQueryScope(company);
            var historical = await db.AssetCategories.SingleAsync(c => c.Id == oldCategory);
            Assert.Equal("Historical category", historical.Name);
            Assert.Null(db.Entry(historical).Property("CreatedAt").CurrentValue);
            Assert.Null(db.Entry(historical).Property("UpdatedAt").CurrentValue);
            historical.Name = "Historical category edited";
            var before = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            Assert.Null(db.Entry(historical).Property("CreatedAt").CurrentValue);
            Assert.InRange((DateTimeOffset)db.Entry(historical).Property("UpdatedAt").CurrentValue!, before, DateTimeOffset.UtcNow);
            var added = new AssetCategory { CompanyId = company, Name = "New category" };
            db.AssetCategories.Add(added);
            db.SaveChanges();
            await db.Entry(added).ReloadAsync();
            var created = (DateTimeOffset)db.Entry(added).Property("CreatedAt").CurrentValue!;
            Assert.InRange(created, before, DateTimeOffset.UtcNow);
            db.ChangeTracker.Clear();
            var loaded = await db.AssetCategories.SingleAsync(c => c.Id == added.Id);
            loaded.Name = "New category edited";
            await db.SaveChangesAsync();
            Assert.Equal(created, (DateTimeOffset)db.Entry(loaded).Property("CreatedAt").CurrentValue!);
            Assert.True((DateTimeOffset)db.Entry(loaded).Property("UpdatedAt").CurrentValue! >= created);
            var rawId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "AssetCategories" ("Id", "CompanyId", "Name")
                VALUES ({rawId}, {company}, 'Direct SQL category')
                """);
            var raw = await db.AssetCategories.SingleAsync(c => c.Id == rawId);
            var rawCreated = (DateTimeOffset)db.Entry(raw).Property("CreatedAt").CurrentValue!;
            Assert.InRange(rawCreated, before, DateTimeOffset.UtcNow);
            var beforeBulk = DateTimeOffset.UtcNow;
            await db.AssetCategories.Where(c => c.Id == rawId).ExecuteUpdateAsync(
                setters => setters.SetProperty(c => c.Name, "Bulk edited category"));
            await db.Entry(raw).ReloadAsync();
            Assert.Equal(rawCreated, (DateTimeOffset)db.Entry(raw).Property("CreatedAt").CurrentValue!);
            Assert.InRange((DateTimeOffset)db.Entry(raw).Property("UpdatedAt").CurrentValue!, beforeBulk, DateTimeOffset.UtcNow);
            db.AssetCategories.Add(new AssetCategory { CompanyId = Guid.NewGuid(), Name = "Foreign write" });
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP SCHEMA {schema} CASCADE", control);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
