using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class CatalogForeignKeyAcceptanceTests
{
    [Theory]
    [InlineData("asset-brand")]
    [InlineData("asset-model")]
    [InlineData("model-brand")]
    public async Task Catalog_constraints_reject_legacy_corruption_atomically_and_protect_new_links(string kind)
    {
        var connection = Environment.GetEnvironmentVariable("ENVANTERTR_SECURITY_TEST_CONNECTION");
        Assert.False(string.IsNullOrWhiteSpace(connection));
        var schema = "catalogfk_" + Guid.NewGuid().ToString("N");
        await using var control = new NpgsqlConnection(connection);
        await control.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA {schema}", control)) await create.ExecuteNonQueryAsync();
        try
        {
            var scoped = new NpgsqlConnectionStringBuilder(connection) { SearchPath = schema };
            await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(scoped.ConnectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", schema)).Options);
            const string previous = "20261030210807_AddMissingCompanyAuditTimestamps";
            await db.Database.MigrateAsync(previous);
            var own = new Company { Name = "Own catalog constraint company" };
            var other = new Company { Name = "Other catalog constraint company" };
            var category = new AssetCategory { CompanyId = own.Id, Name = "Own category" };
            var brand = new AssetBrand { CompanyId = own.Id, Name = "Own brand" };
            var foreignBrand = new AssetBrand { CompanyId = other.Id, Name = "Other brand" };
            var model = new AssetModel { CompanyId = own.Id, Name = "Own model" };
            var foreignModel = new AssetModel { CompanyId = other.Id, Name = "Other model" };
            db.AddRange(own, other, category, brand, foreignBrand, model, foreignModel);
            await db.SaveChangesAsync();
            var bad = NewRecord(kind, kind == "asset-model" ? foreignModel.Id : foreignBrand.Id);
            db.Add(bad);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var migrationFailure = await Assert.ThrowsAsync<PostgresException>(() => db.Database.MigrateAsync());
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, migrationFailure.SqlState);
            Assert.Equal(previous, (await db.Database.GetAppliedMigrationsAsync()).Last());
            var badId = (Guid)db.Entry(bad).Property("Id").CurrentValue!;
            Assert.True(kind.StartsWith("asset-") ? await db.Assets.IgnoreQueryFilters().AnyAsync(a => a.Id == badId)
                : await db.AssetModels.AnyAsync(m => m.Id == badId));
            // Repair only this synthetic fixture's explicitly introduced invalid reference.
            db.Attach(bad);
            db.Entry(bad).Property(kind.EndsWith("brand") ? "BrandId" : "ModelId").CurrentValue = null;
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            await db.Database.MigrateAsync();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.False(db.Database.HasPendingModelChanges());
            foreach (var target in new[] { kind == "asset-model" ? foreignModel.Id : foreignBrand.Id, Guid.NewGuid() })
            {
                db.Add(NewRecord(kind, target));
                var rejection = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
                Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<PostgresException>(rejection.InnerException).SqlState);
                db.ChangeTracker.Clear();
            }
            db.Add(NewRecord(kind, kind == "asset-model" ? model.Id : brand.Id));
            await db.SaveChangesAsync();

            object NewRecord(string path, Guid target) => path.StartsWith("asset-")
                ? new Asset { CompanyId = own.Id, CategoryId = category.Id, Name = "Recoverable FK asset", AssetTag = Guid.NewGuid().ToString("N"),
                    Status = "Available", Currency = "TRY", DeletedAt = DateTimeOffset.UtcNow,
                    BrandId = path == "asset-brand" ? target : null, ModelId = path == "asset-model" ? target : null }
                : new AssetModel { CompanyId = own.Id, Name = Guid.NewGuid().ToString("N"),
                    BrandId = target };
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP SCHEMA {schema} CASCADE", control);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
