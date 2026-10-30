using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class FinalizedTenantModelScopeTests
{
    private const string ModelOnlyConnection = "Host=localhost;Database=model_only;Username=unused;Password=unused";

    [Fact]
    public void Finalized_application_model_preserves_shared_accounts_and_has_no_schema_drift()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ModelOnlyConnection).Options);
        FinalizedTenantModelScope.Validate(db.Model);
        Assert.Null(db.Model.FindEntityType(typeof(EnvanterUser))!.FindProperty("CompanyId"));
        Assert.Null(db.Model.FindEntityType(typeof(SubscriptionPlan))!.FindProperty("CompanyId"));
        Assert.True(db.Model.FindEntityType(typeof(UserSession))!.FindProperty("CompanyId")!.IsNullable);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public void Identity_schema3_passkey_JSON_inherits_its_explicit_shared_owner()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<IdentityOptions>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3);
        services.AddDbContext<PasskeyContext>(options => options.UseNpgsql(ModelOnlyConnection));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PasskeyContext>();
        Assert.NotNull(db.Model.FindEntityType(typeof(IdentityUserPasskey<Guid>)));
        Assert.Contains(db.Model.GetEntityTypes(), e => e.ClrType == typeof(IdentityPasskeyData) && e.FindOwnership() is not null);
        FinalizedTenantModelScope.Validate(db.Model);
    }

    [Theory]
    [InlineData("missing", "Guid CompanyId")]
    [InlineData("nullable", "non-null company")]
    [InlineData("text", "Guid CompanyId")]
    [InlineData("filter", "TenantScope")]
    [InlineData("view", "Guid CompanyId")]
    [InlineData("owned", "Guid CompanyId")]
    public void Persisted_unscoped_entities_are_rejected_after_finalization(string kind, string expected)
    {
        using var db = Fixture(kind);
        var failure = Assert.Throws<InvalidOperationException>(() => FinalizedTenantModelScope.Validate(db.Model));
        Assert.Contains(expected, failure.Message);
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("valid-view")]
    [InlineData("valid-owned")]
    public void Required_company_and_filter_are_accepted_for_tables_views_and_owned_values(string kind)
    {
        using var db = Fixture(kind);
        FinalizedTenantModelScope.Validate(db.Model);
    }

    private static DbContext Fixture(string kind) => kind switch
    {
        "missing" => new ScopeContext<Missing>(), "nullable" => new ScopeContext<Nullable>(),
        "text" => new ScopeContext<Text>(), "filter" => new ScopeContext<NoFilter>(),
        "view" => new ScopeContext<View>(), "owned" => new ScopeContext<Owned>(),
        "valid" => new ScopeContext<Valid>(), "valid-view" => new ScopeContext<ValidView>(),
        "valid-owned" => new ScopeContext<ValidOwned>(), _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private sealed class ScopeContext<T> : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseNpgsql(ModelOnlyConnection);
        protected override void OnModelCreating(ModelBuilder model)
        {
            var row = model.Entity<FutureCompanyRecord>();
            var kind = typeof(T);
            if (kind == typeof(View) || kind == typeof(ValidView)) row.HasNoKey().ToView("FutureCompanyView");
            else row.HasKey(r => r.Id);
            if (kind == typeof(Nullable)) row.Property<Guid?>("CompanyId");
            else if (kind == typeof(Text)) row.Property<string>("CompanyId");
            else if (kind == typeof(Valid) || kind == typeof(ValidView) || kind == typeof(ValidOwned) || kind == typeof(NoFilter))
            {
                row.Property<Guid>("CompanyId").IsRequired();
                if (kind != typeof(NoFilter)) row.HasQueryFilter("TenantScope", r => EF.Property<Guid>(r, "CompanyId") == Guid.Empty);
            }
            if (kind == typeof(Owned) || kind == typeof(ValidOwned)) row.OwnsOne(r => r.Details).ToJson();
            else row.Ignore(r => r.Details);
        }
    }

    public sealed class PasskeyContext(DbContextOptions<PasskeyContext> options)
        : IdentityDbContext<EnvanterUser, IdentityRole<Guid>, Guid>(options);
    private sealed class FutureCompanyRecord { public Guid Id { get; set; } public OwnedDetails Details { get; set; } = new(); }
    private sealed class OwnedDetails { public string Value { get; set; } = ""; }
    private sealed class Missing { } private sealed class Nullable { } private sealed class Text { }
    private sealed class NoFilter { } private sealed class View { } private sealed class Owned { }
    private sealed class Valid { } private sealed class ValidView { } private sealed class ValidOwned { }
}
