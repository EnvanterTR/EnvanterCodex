using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class MigrationUpgradeAcceptanceTests
{
    private const string ProductionBaselineMigration = "20261010180000_AddAccessoryCategories";

    [Fact]
    public async Task Production_baseline_upgrades_to_current_schema_without_losing_company_assets()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("ENVANTERTR_SECURITY_TEST_CONNECTION");
        Assert.False(string.IsNullOrWhiteSpace(baseConnectionString),
            "Set ENVANTERTR_SECURITY_TEST_CONNECTION to the disposable acceptance-test PostgreSQL database.");

        var schema = $"upgrade_{Guid.NewGuid():N}";
        var baseBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString);
        await using (var connection = new NpgsqlConnection(baseBuilder.ConnectionString))
        {
            await connection.OpenAsync();
            await using var createSchema = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", connection);
            await createSchema.ExecuteNonQueryAsync();
        }

        try
        {
            var isolatedBuilder = new NpgsqlConnectionStringBuilder(baseBuilder.ConnectionString)
            {
                SearchPath = $"{schema},public"
            };
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(isolatedBuilder.ConnectionString)
                .Options;

            await using var db = new AppDbContext(options);
            var migrations = db.Database.GetMigrations().ToArray();
            Assert.Contains(ProductionBaselineMigration, migrations);

            await db.Database.MigrateAsync(ProductionBaselineMigration);
            var pendingAtBaseline = (await db.Database.GetPendingMigrationsAsync()).ToArray();
            Assert.Equal(21, pendingAtBaseline.Length);
            Assert.Equal(ProductionBaselineMigration, await ReadLatestMigrationAsync(db));

            var companyId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var assetId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Companies" ("Id", "Name", "BrandPrimaryColor", "IsActive", "CreatedAt")
                VALUES ({companyId}, 'Upgrade acceptance company', '#2563EB', TRUE, now())
                """);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "AssetCategories" ("Id", "CompanyId", "Name")
                VALUES ({categoryId}, {companyId}, 'Upgrade acceptance category')
                """);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Assets" (
                    "Id", "CompanyId", "AssetTag", "Name", "CategoryId", "Status", "Currency", "CreatedAt", "UpdatedAt")
                VALUES ({assetId}, {companyId}, 'UPGRADE-001', 'Upgrade acceptance asset', {categoryId}, 'Available', 'TRY', now(), now())
                """);

            await db.Database.MigrateAsync();

            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.Equal(migrations.Length, (await db.Database.GetAppliedMigrationsAsync()).Count());

            await using var verify = new NpgsqlConnection(isolatedBuilder.ConnectionString);
            await verify.OpenAsync();
            await using var verifyCommand = new NpgsqlCommand("""
                SELECT company."AssetCount", asset."AssetTag", asset."Name"
                FROM "Companies" company
                JOIN "Assets" asset ON asset."CompanyId" = company."Id"
                WHERE company."Id" = @companyId AND asset."Id" = @assetId
                """, verify);
            verifyCommand.Parameters.AddWithValue("companyId", companyId);
            verifyCommand.Parameters.AddWithValue("assetId", assetId);
            await using var reader = await verifyCommand.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync(), "The pre-upgrade company and asset must remain present.");
            Assert.Equal(1, reader.GetInt32(0));
            Assert.Equal("UPGRADE-001", reader.GetString(1));
            Assert.Equal("Upgrade acceptance asset", reader.GetString(2));
            Assert.False(await reader.ReadAsync(), "The seeded asset must not be duplicated by upgrade.");
            await reader.DisposeAsync();

            await using var indexCommand = new NpgsqlCommand("""
                SELECT indexState.indisvalid, indexState.indisready, pg_get_indexdef(indexClass.oid)
                FROM pg_class indexClass
                JOIN pg_namespace indexSchema ON indexSchema.oid = indexClass.relnamespace
                JOIN pg_index indexState ON indexState.indexrelid = indexClass.oid
                WHERE indexSchema.nspname = @schema AND indexClass.relname = 'IX_AuditEntries_OccurredAt'
                """, verify);
            indexCommand.Parameters.AddWithValue("schema", schema);
            await using var indexReader = await indexCommand.ExecuteReaderAsync();
            Assert.True(await indexReader.ReadAsync(), "The audit timeline index must exist after upgrade.");
            Assert.True(indexReader.GetBoolean(0), "The concurrent audit index build must leave a valid index.");
            Assert.True(indexReader.GetBoolean(1), "The audit index must be ready for queries and writes.");
            Assert.Contains("(\"OccurredAt\")", indexReader.GetString(2), StringComparison.Ordinal);
            Assert.False(await indexReader.ReadAsync(), "The audit timeline index name must be unique in the schema.");
        }
        finally
        {
            await using var connection = new NpgsqlConnection(baseBuilder.ConnectionString);
            await connection.OpenAsync();
            await using var dropSchema = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE", connection);
            await dropSchema.ExecuteNonQueryAsync();
        }
    }

    private static async Task<string?> ReadLatestMigrationAsync(AppDbContext db)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 1";
        if (command.Connection!.State != System.Data.ConnectionState.Open)
            await command.Connection.OpenAsync();
        return (string?)await command.ExecuteScalarAsync();
    }
}
