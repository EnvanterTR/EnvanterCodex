using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    private const string ExistingCheckoutPassword = "Synthetic-Checkout-Owner-43!";

    private async Task<(SubscriptionPlan Plan, TenantSubscription Previous, string Code)> SeedExistingCheckout(string state)
    {
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(db.Database.HasPendingModelChanges());
        var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
        var owner = (await users.FindByIdAsync(_userId.ToString()))!;
        Assert.True((await users.AddPasswordAsync(owner, ExistingCheckoutPassword)).Succeeded);
        Assert.True((await users.ResetAuthenticatorKeyAsync(owner)).Succeeded);
        var secret = (await users.GetAuthenticatorKeyAsync(owner))!;
        Assert.True((await users.SetTwoFactorEnabledAsync(owner, true)).Succeeded);
        await db.CompanyMemberships.Where(member => member.CompanyId == _companyId && member.UserId == _userId)
            .ExecuteUpdateAsync(update => update.SetProperty(member => member.RoleKey, "owner"));
        var plan = new SubscriptionPlan { Code = $"ex-{Guid.NewGuid():N}", Name = "Annual existing company",
            DeploymentKind = "cloud", IsActive = true, IsPublic = true, AnnualPrice = 4200m, Currency = "TRY", MaxLoginUsers = 10 };
        var previous = new TenantSubscription { CompanyId = _companyId, PlanId = plan.Id,
            Status = state == "paid" ? "Active" : "Trial", StartsAt = DateTimeOffset.UtcNow.AddDays(-15),
            EndsAt = DateTimeOffset.UtcNow.AddDays(state == "expired" ? -1 : 10) };
        db.SubscriptionPlans.Add(plan); db.TenantSubscriptions.Add(previous); await db.SaveChangesAsync();
        await db.Entry(previous).ReloadAsync();
        return (plan, previous, GenerateAuthenticatorCode(secret));
    }

    private WebApplicationFactory<Program> ExistingCheckoutFactory(ExistingCheckoutProvider provider) =>
        _factory!.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> {
                ["Iyzipay:ApiKey"] = "synthetic-key", ["Iyzipay:SecretKey"] = "synthetic-secret",
                ["Iyzipay:BaseUrl"] = "https://sandbox-api.iyzipay.com",
                ["Iyzipay:CallbackUrl"] = "https://envantertr.com/api/checkout/callback" }));
            builder.ConfigureTestServices(services => {
                services.RemoveAll<IyzipayCheckoutClient>();
                services.AddSingleton(new IyzipayCheckoutClient(new HttpClient(provider) { BaseAddress = new("https://sandbox-api.iyzipay.com") },
                    Options.Create(new IyzipayOptions { ApiKey = "synthetic-key", SecretKey = "synthetic-secret",
                        BaseUrl = "https://sandbox-api.iyzipay.com", CallbackUrl = "https://envantertr.com/api/checkout/callback" })));
            });
        });

    private HttpClient ExistingCheckoutClient(WebApplicationFactory<Program> factory, Guid? company = null, bool allowAutoRedirect = true)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = allowAutoRedirect });
        client.DefaultRequestHeaders.Add("X-Acceptance-User", _userId.ToString());
        client.DefaultRequestHeaders.Add("X-Acceptance-Company", (company ?? _companyId).ToString());
        client.DefaultRequestHeaders.Add("X-Acceptance-Role", "owner");
        return client;
    }

    private static InitializeCompanyCheckoutRequest ExistingPurchase(SubscriptionPlan plan, string code) => new(
        Guid.NewGuid(), "Ignored customer supplied company", "ignored-slug", null, "Synthetic", "Owner",
        "ignored@example.invalid", "+905550000002", "10000000146", "Synthetic Street 2", "Istanbul", "Turkey", "34000",
        plan.Id, "annual", true, "cloud-checkout-data-v1", CurrentPassword: ExistingCheckoutPassword, TwoFactorCode: code);

    [Fact]
    public async Task Existing_company_checkout_page_is_owner_scoped_and_available_after_trial_expiry()
    {
        var seed = await SeedExistingCheckout("expired");
        var provider = new ExistingCheckoutProvider();
        using var factory = ExistingCheckoutFactory(provider);
        using var owner = ExistingCheckoutClient(factory);

        using var page = await owner.GetAsync("/subscription/checkout?lang=en&theme=dark");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal(true, page.Headers.CacheControl?.NoStore);
        Assert.True(page.Headers.TryGetValues("Referrer-Policy", out var referrerPolicies));
        Assert.Equal("no-referrer", referrerPolicies!.Single());
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains("Annual company subscription", html);
        Assert.Contains("Annual existing company", html);
        Assert.Contains("4,200.00 TRY", html);
        Assert.Contains("350.00 TRY", html);
        Assert.Contains("12-month plan", html);
        Assert.Contains("/subscription/checkout", html);
        Assert.Contains("name=\"currentPassword\"", html);
        Assert.Contains("name=\"twoFactorCode\"", html);
        Assert.DoesNotContain(ExistingCheckoutPassword, html);
        Assert.DoesNotContain(seed.Code, html);

        using var nonOwnerFactory = ExistingCheckoutFactory(new ExistingCheckoutProvider());
        using var nonOwner = ExistingCheckoutClient(nonOwnerFactory);
        await using (var scope = nonOwnerFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.CompanyMemberships.Where(member => member.CompanyId == _companyId && member.UserId == _userId)
                .ExecuteUpdateAsync(update => update.SetProperty(member => member.RoleKey, "admin"));
        }
        using var forbidden = await nonOwner.GetAsync("/subscription/checkout");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(0, provider.Initializations);
    }

    [Fact]
    public async Task Existing_company_checkout_form_posts_verified_annual_purchase_to_provider()
    {
        var seed = await SeedExistingCheckout("expired");
        var provider = new ExistingCheckoutProvider();
        using var factory = ExistingCheckoutFactory(provider);
        using var owner = ExistingCheckoutClient(factory, allowAutoRedirect: false);
        await ConfigureTrialCsrf(owner);
        var checkoutId = Guid.NewGuid();
        using var response = await owner.PostAsync("/subscription/checkout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["checkoutId"] = checkoutId.ToString(), ["planId"] = seed.Plan.Id.ToString(), ["lang"] = "tr", ["theme"] = "dark",
            ["firstName"] = "Farma", ["lastName"] = "Owner", ["phone"] = "+905550000002", ["identityNumber"] = "10000000146",
            ["billingAddress"] = "Synthetic Street 2", ["city"] = "Istanbul", ["country"] = "Turkey", ["zipCode"] = "34000",
            ["acknowledged"] = "true", ["currentPassword"] = ExistingCheckoutPassword, ["twoFactorCode"] = seed.Code
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("https://sandbox-api.iyzipay.com/checkoutform/synthetic-session", response.Headers.Location!.AbsoluteUri);
        Assert.Equal(1, provider.Initializations);
        Assert.Equal(4200m, provider.Amount);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await db.CompanyCheckoutOrders.SingleAsync(item => item.Id == checkoutId);
        Assert.Equal(_companyId, order.TargetCompanyId);
        Assert.Equal(_userId, order.RequestedByUserId);
        Assert.Equal("PaymentStarted", order.Status);
        Assert.Equal(4200m, order.Amount);
        Assert.Equal("TRY", order.Currency);
        Assert.Equal(0, await db.Companies.CountAsync(company => company.Slug == "ignored-slug"));
    }

    [Theory]
    [InlineData("trial")]
    [InlineData("expired")]
    [InlineData("paid")]
    public async Task Existing_company_annual_checkout_preserves_data_periods_and_replay(string state)
    {
        var seed = await SeedExistingCheckout(state);
        var provider = new ExistingCheckoutProvider();
        using var factory = ExistingCheckoutFactory(provider);
        using var client = ExistingCheckoutClient(factory);
        using var noCsrf = await client.PostAsJsonAsync("/api/subscription/checkout/initialize", ExistingPurchase(seed.Plan, seed.Code));
        Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        await ConfigureTrialCsrf(client);
        if (state == "expired")
        {
            using var write = await client.PostAsJsonAsync("/api/brands", new { name = "Must remain read only" });
            Assert.Equal(HttpStatusCode.PaymentRequired, write.StatusCode);
        }
        using var context = await client.GetAsync("/api/subscription/checkout/context");
        Assert.Equal(HttpStatusCode.OK, context.StatusCode);
        using var payload = JsonDocument.Parse(await context.Content.ReadAsStringAsync());
        Assert.Equal(_companyId, payload.RootElement.GetProperty("companyId").GetGuid());
        var request = ExistingPurchase(seed.Plan, seed.Code);
        using var initialized = await client.PostAsJsonAsync("/api/subscription/checkout/initialize", request);
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);
        Assert.Equal(4200m, provider.Amount);
        using var retry = await client.PostAsJsonAsync("/api/subscription/checkout/initialize", request);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode); Assert.Equal(1, provider.Initializations);
        using var trailingSlash = await client.PostAsJsonAsync("/api/subscription/checkout/initialize/", request);
        Assert.Equal(HttpStatusCode.OK, trailingSlash.StatusCode); Assert.Equal(1, provider.Initializations);
        // Public checkout cannot reuse an authenticated company's purchase identity.
        using var anonymous = factory.CreateClient(); await ConfigureTrialCsrf(anonymous);
        using var stolen = await anonymous.PostAsJsonAsync("/api/checkout/initialize", request);
        Assert.Equal(HttpStatusCode.Conflict, stolen.StatusCode);
        using var outside = ExistingCheckoutClient(factory, _otherCompanyId);
        using var outsideOrder = await outside.GetAsync($"/api/subscription/checkout/orders/{request.CheckoutId}");
        Assert.Equal(HttpStatusCode.Unauthorized, outsideOrder.StatusCode);
        for (var replay = 0; replay < 2; replay++)
        {
            using var callback = await client.PostAsync("/api/checkout/callback", new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = provider.Token }));
            Assert.Equal(HttpStatusCode.OK, callback.StatusCode);
            Assert.Contains($"https://acceptance-{_companyId:N}.envantertr.com/", await callback.Content.ReadAsStringAsync());
        }
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await db.CompanyCheckoutOrders.SingleAsync(item => item.Id == request.CheckoutId);
        Assert.Equal(_companyId, order.TargetCompanyId); Assert.Equal(_userId, order.RequestedByUserId);
        Assert.Equal(_companyId, order.CompanyId); Assert.Equal("Provisioned", order.Status);
        Assert.False(await db.Companies.AnyAsync(company => company.Slug == request.Slug));
        Assert.True(await db.Assets.AnyAsync(asset => asset.Id == _ownAssetId && asset.CompanyId == _companyId));
        Assert.True(await db.Assets.AnyAsync(asset => asset.Id == _otherCompanyAssetId && asset.CompanyId == _otherCompanyId));
        var subscriptions = await db.TenantSubscriptions.Where(item => item.CompanyId == _companyId).ToListAsync();
        Assert.Equal(2, subscriptions.Count);
        var previous = subscriptions.Single(item => item.Id == seed.Previous.Id);
        Assert.Equal(seed.Previous.StartsAt, previous.StartsAt); Assert.Equal(seed.Previous.EndsAt, previous.EndsAt);
        Assert.Equal(state == "expired" ? "Expired" : "Cancelled", previous.Status);
        var active = Assert.Single(subscriptions, item => item.Status == "Active");
        Assert.Equal(provider.PaymentId, active.PaymentReference);
        if (state == "paid") Assert.Equal(previous.EndsAt.AddDays(365), active.EndsAt);
        else Assert.Equal(TimeSpan.FromDays(365), active.EndsAt - active.StartsAt);
        Assert.Single(await db.AuditEntries.Where(item => item.CompanyId == _companyId && item.Action == "PaidSubscriptionActivated").ToListAsync());
    }

    [Theory]
    [InlineData("Active", false, true, false)]
    [InlineData("Cancelled", false, true, false)]
    [InlineData("Active", true, true, true)]
    [InlineData("Cancelled", false, false, true)]
    [InlineData("Suspended", false, false, false)]
    public async Task Existing_company_annual_lifecycle_preserves_paid_time_and_plan_boundaries(
        string status, bool expired, bool changePlan, bool allowed)
    {
        var seed = await SeedExistingCheckout("paid");
        DateTimeOffset originalEnd;
        var target = seed.Plan;
        await using (var setup = _factory!.Services.CreateAsyncScope())
        {
            var db = setup.ServiceProvider.GetRequiredService<AppDbContext>();
            var previous = await db.TenantSubscriptions.SingleAsync(item => item.Id == seed.Previous.Id);
            previous.Status = status;
            previous.EndsAt = DateTimeOffset.UtcNow.AddDays(expired ? -1 : 10);
            if (changePlan)
            {
                target = new SubscriptionPlan { Code = $"life-{Guid.NewGuid():N}", Name = "Lifecycle target",
                    DeploymentKind = "cloud", IsActive = true, IsPublic = true, AnnualPrice = 6000m,
                    Currency = "TRY", MaxLoginUsers = 10 };
                db.SubscriptionPlans.Add(target);
            }
            await db.SaveChangesAsync();
            await db.Entry(previous).ReloadAsync();
            originalEnd = previous.EndsAt;
        }
        var provider = new ExistingCheckoutProvider();
        using var factory = ExistingCheckoutFactory(provider);
        using var client = ExistingCheckoutClient(factory);
        await ConfigureTrialCsrf(client);
        var request = ExistingPurchase(target, seed.Code);
        using var initialized = await client.PostAsJsonAsync("/api/subscription/checkout/initialize", request);
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Conflict, initialized.StatusCode);
        if (allowed)
        {
            for (var replay = 0; replay < 2; replay++)
            {
                using var callback = await client.PostAsync("/api/checkout/callback",
                    new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = provider.Token }));
                Assert.Equal(HttpStatusCode.OK, callback.StatusCode);
            }
        }
        await using var check = factory.Services.CreateAsyncScope();
        var verify = check.ServiceProvider.GetRequiredService<AppDbContext>();
        var history = await verify.TenantSubscriptions.Where(item => item.CompanyId == _companyId).ToListAsync();
        var original = Assert.Single(history, item => item.Id == seed.Previous.Id);
        Assert.Equal(originalEnd, original.EndsAt);
        Assert.Equal(seed.Previous.PlanId, original.PlanId);
        Assert.Equal(allowed ? 1 : 0, provider.Initializations);
        Assert.Equal(allowed ? 2 : 1, history.Count);
        if (allowed)
        {
            var active = Assert.Single(history, item => item.Status == "Active");
            Assert.Equal(target.Id, active.PlanId);
            Assert.Equal(provider.PaymentId, active.PaymentReference);
            if (!expired) Assert.Equal(originalEnd.AddDays(365), active.EndsAt);
            else Assert.Equal(TimeSpan.FromDays(365), active.EndsAt - active.StartsAt);
            Assert.Single(await verify.AuditEntries.Where(item => item.CompanyId == _companyId &&
                item.Action == "PaidSubscriptionActivated").ToListAsync());
        }
        else
        {
            Assert.Equal(status, original.Status);
            Assert.False(await verify.CompanyCheckoutOrders.AnyAsync(item => item.Id == request.CheckoutId));
        }
        Assert.True(await verify.Assets.AnyAsync(item => item.Id == _ownAssetId && item.CompanyId == _companyId));
        Assert.True(await verify.Assets.AnyAsync(item => item.Id == _otherCompanyAssetId && item.CompanyId == _otherCompanyId));
    }

    [Fact]
    public async Task Existing_company_concurrent_payment_callbacks_activate_once()
    {
        var seed = await SeedExistingCheckout("trial");
        var provider = new ExistingCheckoutProvider();
        using var factory = ExistingCheckoutFactory(provider);
        using var owner = ExistingCheckoutClient(factory);
        await ConfigureTrialCsrf(owner);
        var request = ExistingPurchase(seed.Plan, seed.Code);
        using var initialized = await owner.PostAsJsonAsync("/api/subscription/checkout/initialize", request);
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);

        using var callbackClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var callbacks = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            callbackClient.PostAsync("/api/checkout/callback", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["token"] = provider.Token
            }))));
        Assert.All(callbacks, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        foreach (var callback in callbacks)
            Assert.Contains("Yıllık aboneliğiniz etkinleştirildi", await callback.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await db.CompanyCheckoutOrders.SingleAsync(item => item.Id == request.CheckoutId);
        Assert.Equal("Provisioned", order.Status);
        Assert.Equal(_companyId, order.TargetCompanyId);
        Assert.Equal(_companyId, order.CompanyId);
        Assert.Single(await db.TenantSubscriptions.Where(item => item.CompanyId == _companyId && item.Status == "Active").ToListAsync());
        Assert.Single(await db.AuditEntries.Where(item => item.CompanyId == _companyId && item.Action == "PaidSubscriptionActivated").ToListAsync());
        Assert.True(await db.Assets.AnyAsync(asset => asset.Id == _ownAssetId && asset.CompanyId == _companyId));
        Assert.True(await db.Assets.AnyAsync(asset => asset.Id == _otherCompanyAssetId && asset.CompanyId == _otherCompanyId));
    }

    [Fact]
    public async Task Existing_company_checkout_requires_owner_mfa_reauthentication_and_capacity()
    {
        var seed = await SeedExistingCheckout("expired");
        var provider = new ExistingCheckoutProvider();
        using var factory = ExistingCheckoutFactory(provider);
        using var client = ExistingCheckoutClient(factory);
        await ConfigureTrialCsrf(client);
        var request = ExistingPurchase(seed.Plan, seed.Code);
        using var missing = await client.PostAsJsonAsync("/api/subscription/checkout/initialize", request with { CurrentPassword = null });
        Assert.Equal((HttpStatusCode)428, missing.StatusCode);
        using var monthly = await client.PostAsJsonAsync("/api/subscription/checkout/initialize", request with { Term = "monthly" });
        Assert.Equal(HttpStatusCode.BadRequest, monthly.StatusCode);
        using var wrong = await client.PostAsJsonAsync("/api/subscription/checkout/initialize", request with { CurrentPassword = "wrong" });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
        var owner = (await users.FindByIdAsync(_userId.ToString()))!;
        await users.SetTwoFactorEnabledAsync(owner, false);
        using var noMfa = await client.PostAsJsonAsync("/api/subscription/checkout/initialize", request);
        Assert.Equal((HttpStatusCode)428, noMfa.StatusCode);
        await users.SetTwoFactorEnabledAsync(owner, true);
        await db.SubscriptionPlans.Where(plan => plan.Id == seed.Plan.Id).ExecuteUpdateAsync(update => update.SetProperty(plan => plan.MaxLoginUsers, 1));
        using var capacity = await client.PostAsJsonAsync("/api/subscription/checkout/initialize", request);
        Assert.Equal(HttpStatusCode.Conflict, capacity.StatusCode);
        await db.CompanyMemberships.Where(member => member.CompanyId == _companyId && member.UserId == _userId)
            .ExecuteUpdateAsync(update => update.SetProperty(member => member.RoleKey, "admin"));
        using var denied = await client.PostAsJsonAsync("/api/subscription/checkout/initialize", request);
        Assert.Equal(HttpStatusCode.TooManyRequests, denied.StatusCode);
        using var ownerOnlyContext = await client.GetAsync("/api/subscription/checkout/context");
        Assert.Equal(HttpStatusCode.Forbidden, ownerOnlyContext.StatusCode);
        // Exercise the owner authorization independently of the exhausted limiter.
        var freshProvider = new ExistingCheckoutProvider();
        using var freshFactory = ExistingCheckoutFactory(freshProvider);
        using var nonOwner = ExistingCheckoutClient(freshFactory);
        await ConfigureTrialCsrf(nonOwner);
        using var nonOwnerPurchase = await nonOwner.PostAsJsonAsync("/api/subscription/checkout/initialize", request);
        Assert.Equal(HttpStatusCode.Forbidden, nonOwnerPurchase.StatusCode);
        Assert.Equal(0, freshProvider.Initializations);
        Assert.Equal(0, provider.Initializations);
        Assert.False(await db.CompanyCheckoutOrders.AnyAsync(order => order.TargetCompanyId == _companyId));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Existing_company_payment_rechecks_capacity_and_owner_before_activation(bool revokeOwner)
    {
        var seed = await SeedExistingCheckout("trial");
        var provider = new ExistingCheckoutProvider();
        using var factory = ExistingCheckoutFactory(provider);
        using var client = ExistingCheckoutClient(factory);
        await ConfigureTrialCsrf(client);
        var request = ExistingPurchase(seed.Plan, seed.Code);
        using var initialized = await client.PostAsJsonAsync("/api/subscription/checkout/initialize", request);
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (revokeOwner)
            await db.CompanyMemberships.Where(member => member.CompanyId == _companyId && member.UserId == _userId)
                .ExecuteUpdateAsync(update => update.SetProperty(member => member.RoleKey, "admin"));
        else
            await db.SubscriptionPlans.Where(plan => plan.Id == seed.Plan.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(plan => plan.MaxLoginUsers, 1));
        using var anonymous = factory.CreateClient();
        using var callback = await anonymous.PostAsync("/api/checkout/callback", new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = provider.Token }));
        Assert.Equal(HttpStatusCode.OK, callback.StatusCode);
        var order = await db.CompanyCheckoutOrders.SingleAsync(item => item.Id == request.CheckoutId);
        Assert.Equal("ProvisioningFailed", order.Status); Assert.Null(order.CompanyId);
        Assert.Equal(_companyId, order.TargetCompanyId); Assert.Equal(provider.PaymentId, order.PaymentReference);
        var previous = Assert.Single(await db.TenantSubscriptions.Where(item => item.CompanyId == _companyId).ToListAsync());
        Assert.Equal(seed.Previous.Id, previous.Id); Assert.Equal("Trial", previous.Status);
        Assert.True(await db.Assets.AnyAsync(asset => asset.Id == _ownAssetId && asset.CompanyId == _companyId));
        var entity = db.Model.FindEntityType(typeof(CompanyCheckoutOrder))!;
        Assert.DoesNotContain(entity.GetForeignKeys(), key => key.Properties.Any(property => property.Name == nameof(CompanyCheckoutOrder.TargetCompanyId)));
    }

    [Fact]
    public async Task Existing_company_checkout_rejects_provider_result_mismatch_without_activation()
    {
        var seed = await SeedExistingCheckout("trial");
        var provider = new ExistingCheckoutProvider { ResultCurrency = "USD" };
        using var factory = ExistingCheckoutFactory(provider);
        using var owner = ExistingCheckoutClient(factory);
        await ConfigureTrialCsrf(owner);
        var request = ExistingPurchase(seed.Plan, seed.Code);
        using var initialized = await owner.PostAsJsonAsync("/api/subscription/checkout/initialize", request);
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);

        using var anonymous = factory.CreateClient();
        using var callback = await anonymous.PostAsync("/api/checkout/callback",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = provider.Token }));

        Assert.Equal(HttpStatusCode.BadRequest, callback.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await db.CompanyCheckoutOrders.SingleAsync(item => item.Id == request.CheckoutId);
        Assert.Equal("PaymentStarted", order.Status);
        Assert.Equal(_companyId, order.TargetCompanyId);
        Assert.Null(order.PaymentReference);
        Assert.Equal(seed.Previous.Id, (await db.TenantSubscriptions.SingleAsync(item => item.CompanyId == _companyId)).Id);
        Assert.False(await db.TenantSubscriptions.AnyAsync(item => item.CompanyId == _companyId && item.Status == "Active"));
    }

    [Fact]
    public async Task Existing_company_checkout_does_not_activate_when_provider_result_is_unavailable()
    {
        var seed = await SeedExistingCheckout("trial");
        var provider = new ExistingCheckoutProvider { ResultStatus = "failure" };
        using var factory = ExistingCheckoutFactory(provider);
        using var owner = ExistingCheckoutClient(factory);
        await ConfigureTrialCsrf(owner);
        var request = ExistingPurchase(seed.Plan, seed.Code);
        using var initialized = await owner.PostAsJsonAsync("/api/subscription/checkout/initialize", request);
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);

        using var anonymous = factory.CreateClient();
        using var callback = await anonymous.PostAsync("/api/checkout/callback",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = provider.Token }));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, callback.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await db.CompanyCheckoutOrders.SingleAsync(item => item.Id == request.CheckoutId);
        Assert.Equal("PaymentStarted", order.Status);
        Assert.Null(order.PaymentReference);
        Assert.Equal(seed.Previous.Id, (await db.TenantSubscriptions.SingleAsync(item => item.CompanyId == _companyId)).Id);
        Assert.False(await db.TenantSubscriptions.AnyAsync(item => item.CompanyId == _companyId && item.Status == "Active"));
    }

    [Fact]
    public async Task Existing_company_checkout_does_not_activate_when_target_company_is_deactivated_before_callback()
    {
        var seed = await SeedExistingCheckout("trial");
        var provider = new ExistingCheckoutProvider();
        using var factory = ExistingCheckoutFactory(provider);
        using var owner = ExistingCheckoutClient(factory);
        await ConfigureTrialCsrf(owner);
        var request = ExistingPurchase(seed.Plan, seed.Code);
        using var initialized = await owner.PostAsJsonAsync("/api/subscription/checkout/initialize", request);
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Companies.Where(company => company.Id == _companyId)
            .ExecuteUpdateAsync(update => update.SetProperty(company => company.IsActive, false));
        using var anonymous = factory.CreateClient();
        using var callback = await anonymous.PostAsync("/api/checkout/callback",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = provider.Token }));

        Assert.Equal(HttpStatusCode.OK, callback.StatusCode);
        var order = await db.CompanyCheckoutOrders.SingleAsync(item => item.Id == request.CheckoutId);
        Assert.Equal("ProvisioningFailed", order.Status);
        Assert.Null(order.CompanyId);
        Assert.Equal(_companyId, order.TargetCompanyId);
        Assert.Equal(provider.PaymentId, order.PaymentReference);
        Assert.Equal(seed.Previous.Id, (await db.TenantSubscriptions.SingleAsync(item => item.CompanyId == _companyId)).Id);
        Assert.False(await db.TenantSubscriptions.AnyAsync(item => item.CompanyId == _companyId && item.Status == "Active"));
    }

    [Fact]
    public async Task Existing_company_checkout_does_not_create_a_replacement_when_target_company_is_deleted()
    {
        var seed = await SeedExistingCheckout("trial");
        var provider = new ExistingCheckoutProvider();
        using var factory = ExistingCheckoutFactory(provider);
        using var owner = ExistingCheckoutClient(factory);
        await ConfigureTrialCsrf(owner);
        var request = ExistingPurchase(seed.Plan, seed.Code);
        using var initialized = await owner.PostAsJsonAsync("/api/subscription/checkout/initialize", request);
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var originalSlug = await db.Companies.Where(company => company.Id == _companyId)
            .Select(company => company.Slug).SingleAsync();
        Assert.Equal(1, await db.Companies.Where(company => company.Id == _companyId).ExecuteDeleteAsync());
        using var anonymous = factory.CreateClient();
        using var callback = await anonymous.PostAsync("/api/checkout/callback",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = provider.Token }));

        Assert.Equal(HttpStatusCode.OK, callback.StatusCode);
        Assert.False(await db.Companies.AnyAsync(company => company.Slug == originalSlug));
        var order = await db.CompanyCheckoutOrders.SingleAsync(item => item.Id == request.CheckoutId);
        Assert.Equal("ProvisioningFailed", order.Status);
        Assert.Null(order.CompanyId);
        Assert.Equal(_companyId, order.TargetCompanyId);
        Assert.Equal(provider.PaymentId, order.PaymentReference);
        Assert.False(await db.TenantSubscriptions.AnyAsync(item => item.CompanyId == _companyId && item.Status == "Active"));
    }

    private sealed class ExistingCheckoutProvider : HttpMessageHandler
    {
        public string Token { get; } = $"existing-token-{Guid.NewGuid():N}";
        public string PaymentId { get; } = $"existing-payment-{Guid.NewGuid():N}";
        public int Initializations { get; private set; }
        public decimal Amount { get; private set; }
        public string ResultStatus { get; init; } = "success";
        public string ResultCurrency { get; init; } = "TRY";
        private string? _conversation;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            object result;
            if (request.RequestUri!.AbsolutePath.EndsWith("initialize/auth/ecom", StringComparison.Ordinal))
            {
                Initializations++; Amount = body.RootElement.GetProperty("price").GetDecimal();
                _conversation = body.RootElement.GetProperty("conversationId").GetString();
                result = new { status = "success", conversationId = _conversation, token = Token,
                    paymentPageUrl = "https://sandbox-api.iyzipay.com/checkoutform/synthetic-session" };
            }
            else
            {
                Assert.Equal(Token, body.RootElement.GetProperty("token").GetString());
                result = new { status = ResultStatus, token = Token, conversationId = _conversation, basketId = _conversation,
                    price = Amount, paidPrice = Amount, currency = ResultCurrency, fraudStatus = 1, paymentStatus = "SUCCESS", paymentId = PaymentId };
            }
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(result), Encoding.UTF8, "application/json") };
        }
    }
}
