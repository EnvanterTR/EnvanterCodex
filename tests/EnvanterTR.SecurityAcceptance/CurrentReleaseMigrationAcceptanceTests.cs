using System.Security.Cryptography;
using System.Text;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
using Xunit.Abstractions;

namespace EnvanterTR.SecurityAcceptance;

public sealed class CurrentReleaseMigrationAcceptanceTests(ITestOutputHelper output)
{
    private const string Baseline = "20261020174959_AddSubscriptionCoupons";

    [Fact]
    public async Task Current_release_preserves_all_original_fields_from_118_and_repeats_idempotently()
    {
        var connectionString = Environment.GetEnvironmentVariable("ENVANTERTR_SECURITY_TEST_CONNECTION");
        Assert.False(string.IsNullOrWhiteSpace(connectionString));
        var schema = "releasecheck_" + Guid.NewGuid().ToString("N");
        await using var control = new NpgsqlConnection(connectionString);
        await control.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA {Quote(schema)}", control))
            await create.ExecuteNonQueryAsync();
        try
        {
            // No public fallback: neither migration commands nor projections may
            // touch a pre-existing acceptance database's application tables.
            var isolated = new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = schema };
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(isolated.ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", schema)).Options;
            await using var db = new AppDbContext(options);
            var migrations = db.Database.GetMigrations().ToArray();
            Assert.Equal(118, Array.IndexOf(migrations, Baseline) + 1);
            Assert.Equal("c8694e4a153c5f81d5cface1e0c9f366", Convert.ToHexStringLower(
                MD5.HashData(Encoding.UTF8.GetBytes(string.Join('\n', migrations.Take(118))))));
            Assert.Equal(new[] { "20261023190000_AddExistingCompanyCheckoutContext", "20261024010000_AddPlatformImpersonationSessions" }, migrations.Skip(118));
            await db.Database.MigrateAsync(Baseline);
            Assert.Equal(118, (await db.Database.GetAppliedMigrationsAsync()).Count());

            var company = new Company { Name = "Synthetic preserved company", Slug = "release-preserved" };
            var user = new EnvanterUser { DisplayName = "Synthetic preserved owner", UserName = "release@example.invalid", Email = "release@example.invalid", EmailConfirmed = true };
            var plan = new SubscriptionPlan { Code = "synthetic-release", Name = "Synthetic release plan", DeploymentKind = "cloud", AnnualPrice = 2400 };
            db.Companies.Add(company); db.Users.Add(user); db.SubscriptionPlans.Add(plan);
            await db.SaveChangesAsync();
            var sessionId = Guid.NewGuid(); var orderId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "UserSessions" ("Id", "UserId", "CompanyId", "PlatformAdmin", "CreatedAt", "LastSeenAt", "ExpiresAt", "UserAgent")
                VALUES ({sessionId}, {user.Id}, {company.Id}, FALSE, now(), now(), now() + interval '1 day', 'Synthetic preserved session')
                """);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "CompanyCheckoutOrders" ("Id", "CompanyName", "Slug", "ContactEmail", "OwnerName", "OwnerEmail", "PlanId",
                    "TermDays", "Amount", "SubtotalAmount", "Currency", "Status", "CompanyId", "CreatedAt", "ExpiresAt")
                VALUES ({orderId}, 'Synthetic preserved checkout', 'release-checkout', 'release@example.invalid', 'Synthetic owner',
                    'release@example.invalid', {plan.Id}, 365, 2400, 2400, 'TRY', 'Pending', {company.Id}, now(), now() + interval '1 hour')
                """);
            var categoryId = Guid.NewGuid(); var assetId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "AssetCategories" ("Id", "CompanyId", "Name") VALUES ({categoryId}, {company.Id}, 'Synthetic preserved category')
                """);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Assets" ("Id", "CompanyId", "AssetTag", "Name", "CategoryId", "Status", "Currency", "CreatedAt", "UpdatedAt")
                VALUES ({assetId}, {company.Id}, 'RELEASE-001', 'Synthetic preserved asset', {categoryId}, 'Available', 'TRY', now(), now())
                """);

            await using var connection = new NpgsqlConnection(isolated.ConnectionString);
            await connection.OpenAsync();
            var columns = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            await using (var command = new NpgsqlCommand("""
                SELECT table_name, column_name FROM information_schema.columns
                WHERE table_schema = @schema AND table_name <> '__EFMigrationsHistory'
                ORDER BY table_name COLLATE "C", ordinal_position
                """, connection))
            {
                command.Parameters.AddWithValue("schema", schema);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var table = reader.GetString(0);
                    if (!columns.TryGetValue(table, out var fields)) columns[table] = fields = [];
                    fields.Add(reader.GetString(1));
                }
            }
            Assert.True(columns.Count > 90);
            var before = await HashProjections(connection, schema, columns);
            var artifact = Environment.GetEnvironmentVariable("CURRENT_RELEASE_SQL_PATH") ?? Path.Combine(AppContext.BaseDirectory, "release-migration.sql");
            var bytes = await File.ReadAllBytesAsync(artifact);
            var expectedHash = (await File.ReadAllTextAsync(artifact + ".sha256")).Trim().Split(' ')[0];
            Assert.Equal(expectedHash, Convert.ToHexStringLower(SHA256.HashData(bytes)));
            var sql = Encoding.UTF8.GetString(bytes);
            var generated = db.GetService<IMigrator>().GenerateScript(Baseline, migrations[^1], MigrationsSqlGenerationOptions.Idempotent)
                .Replace(Quote(schema) + "." + Quote("__EFMigrationsHistory"), Quote("__EFMigrationsHistory"), StringComparison.Ordinal)
                .Replace(schema + "." + Quote("__EFMigrationsHistory"), Quote("__EFMigrationsHistory"), StringComparison.Ordinal);
            Assert.Equal(generated.Replace("\r\n", "\n").TrimEnd(), sql.Replace("\r\n", "\n").TrimEnd());
            output.WriteLine("Release SQL SHA-256: " + expectedHash);
            Assert.DoesNotContain("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DROP COLUMN", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE FROM", sql, StringComparison.OrdinalIgnoreCase);
            await using (var apply = new NpgsqlCommand(sql, connection)) await apply.ExecuteNonQueryAsync();
            Assert.Equal(120, (await db.Database.GetAppliedMigrationsAsync()).Count());
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.Equal(before, await HashProjections(connection, schema, columns));
            await using (var repeat = new NpgsqlCommand(sql, connection)) await repeat.ExecuteNonQueryAsync();
            Assert.Equal(120, (await db.Database.GetAppliedMigrationsAsync()).Count());
            Assert.Equal(before, await HashProjections(connection, schema, columns));
            db.ChangeTracker.Clear();
            var preservedSession = await db.UserSessions.SingleAsync(session => session.Id == sessionId);
            Assert.Null(preservedSession.ImpersonatorUserId); Assert.Null(preservedSession.ImpersonationReturnTokenHash);
            var preservedOrder = await db.CompanyCheckoutOrders.SingleAsync(order => order.Id == orderId);
            Assert.Null(preservedOrder.TargetCompanyId); Assert.Null(preservedOrder.RequestedByUserId);
            Assert.Equal(company.Id, (await db.Assets.SingleAsync(asset => asset.Id == assetId)).CompanyId);
            output.WriteLine($"PRESERVED_RELEASE_118_TO_120_OK: {columns.Count} original tables; original fields/rows identical after first and repeated SQL; existing session/order/assets preserved.");
            output.WriteLine("Synthetic isolated schema only; not production backup, staging, or live acceptance.");
        }
        finally
        {
            // Only the random schema created above is removed.
            await using var drop = new NpgsqlCommand($"DROP SCHEMA {Quote(schema)} CASCADE", control);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static async Task<string> HashProjections(NpgsqlConnection connection, string schema, Dictionary<string, List<string>> columns)
    {
        var projection = new StringBuilder();
        foreach (var (table, fields) in columns)
        {
            await using var command = new NpgsqlCommand($"""
                SELECT COALESCE(string_agg(row_to_json(t)::text, E'\n' ORDER BY row_to_json(t)::text COLLATE "C"), '')
                FROM (SELECT {string.Join(", ", fields.Select(Quote))} FROM {Quote(schema)}.{Quote(table)}) AS t
                """, connection);
            projection.Append(table).Append('\n').Append((string)(await command.ExecuteScalarAsync())!).Append('\n');
        }
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(projection.ToString())));
    }
}
