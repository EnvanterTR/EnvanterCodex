using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace EnvanterTR.CheckoutBrowser;

internal static class BrowserAcceptance
{
    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    public static async Task Main()
    {
        var connection = Environment.GetEnvironmentVariable("ENVANTERTR_SECURITY_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Disposable acceptance connection required.");
        var parsed = new NpgsqlConnectionStringBuilder(connection);
        Require(parsed.Host == "database" && parsed.Database == "envantertr_security_acceptance",
            "This harness only permits the isolated Compose acceptance database.");
        Environment.SetEnvironmentVariable("ConnectionStrings__EnvanterTR", connection);
        var provider = new SyntheticProvider();
        using var factory = new WebApplicationFactory<global::Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseContentRoot("/source/src/EnvanterTR.Web");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EnvanterTR"] = connection,
                ["TenantRouting:RootDomain"] = "example.test",
                ["Iyzipay:ApiKey"] = "synthetic-key", ["Iyzipay:SecretKey"] = "synthetic-secret",
                ["Iyzipay:BaseUrl"] = "https://sandbox-api.iyzipay.com",
                ["Iyzipay:CallbackUrl"] = "https://envantertr.com/api/checkout/callback",
                ["Logging:LogLevel:Default"] = "Warning"
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IyzipayCheckoutClient>();
                services.AddSingleton(new IyzipayCheckoutClient(new HttpClient(provider)
                { BaseAddress = new("https://sandbox-api.iyzipay.com") }, Options.Create(new IyzipayOptions
                {
                    ApiKey = "synthetic-key", SecretKey = "synthetic-secret", BaseUrl = "https://sandbox-api.iyzipay.com",
                    CallbackUrl = "https://envantertr.com/api/checkout/callback"
                })));
            });
        });
        factory.UseKestrel(0);
        factory.StartServer();
        using var client = factory.CreateClient();
        var company = new Company { Name = "Synthetic Farma", Slug = "synthetic-farma", IsActive = true };
        var other = new Company { Name = "Synthetic Santa", Slug = "synthetic-santa", IsActive = true };
        var plan = new SubscriptionPlan { Code = "browser-annual", Name = "Browser annual plan", IsActive = true,
            IsPublic = true, DeploymentKind = "cloud", AnnualPrice = 4200m, MonthlyPrice = 0, Currency = "TRY", MaxLoginUsers = 4 };
        var owner = new EnvanterUser { UserName = "owner@example.invalid", Email = "owner@example.invalid",
            DisplayName = "Synthetic Owner", EmailConfirmed = true };
        var viewer = new EnvanterUser { UserName = "viewer@example.invalid", Email = "viewer@example.invalid",
            DisplayName = "Synthetic Viewer", EmailConfirmed = true };
        const string password = "Synthetic-Browser-Checkout-64!";
        string secret;
        Guid ownAssetId, foreignAssetId, previousId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            Require((await users.CreateAsync(owner, password)).Succeeded, "Owner seed failed.");
            Require((await users.CreateAsync(viewer, password)).Succeeded, "Viewer seed failed.");
            Require((await users.ResetAuthenticatorKeyAsync(owner)).Succeeded, "MFA seed failed.");
            secret = (await users.GetAuthenticatorKeyAsync(owner))!;
            Require((await users.SetTwoFactorEnabledAsync(owner, true)).Succeeded, "MFA enable failed.");
            db.Companies.AddRange(company, other); db.SubscriptionPlans.Add(plan);
            db.CompanyMemberships.AddRange(new CompanyMembership { CompanyId = company.Id, UserId = owner.Id, RoleKey = "owner" },
                new CompanyMembership { CompanyId = company.Id, UserId = viewer.Id, RoleKey = "viewer" });
            var previous = new TenantSubscription { CompanyId = company.Id, PlanId = plan.Id, Status = "Trial",
                StartsAt = DateTimeOffset.UtcNow.AddDays(-16), EndsAt = DateTimeOffset.UtcNow.AddDays(-1) };
            previousId = previous.Id; db.TenantSubscriptions.Add(previous);
            var category = new AssetCategory { CompanyId = company.Id, Name = "Farma devices" };
            var foreignCategory = new AssetCategory { CompanyId = other.Id, Name = "Santa devices" };
            db.AssetCategories.AddRange(category, foreignCategory);
            var ownAsset = new Asset { CompanyId = company.Id, CategoryId = category.Id, Name = "Retained Farma asset", AssetTag = "FARMA-001" };
            var foreignAsset = new Asset { CompanyId = other.Id, CategoryId = foreignCategory.Id, Name = "Retained Santa asset", AssetTag = "SANTA-001" };
            ownAssetId = ownAsset.Id; foreignAssetId = foreignAsset.Id;
            db.Assets.AddRange(ownAsset, foreignAsset);
            await db.SaveChangesAsync();
        }
        var start = new ProcessStartInfo("node", "/browser/existing-company-checkout.mjs")
        { RedirectStandardInput = true, UseShellExecute = false };
        using var browser = Process.Start(start) ?? throw new InvalidOperationException("Chromium runner did not start.");
        await browser.StandardInput.WriteAsync(JsonSerializer.Serialize(new { baseUrl = client.BaseAddress!.AbsoluteUri.TrimEnd('/'),
            companyId = company.Id, ownerEmail = owner.Email, viewerEmail = viewer.Email, password, secret, token = SyntheticProvider.Token }));
        browser.StandardInput.Close();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try { await browser.WaitForExitAsync(timeout.Token); }
        catch { browser.Kill(entireProcessTree: true); throw; }
        Require(browser.ExitCode == 0, "Chromium checkout acceptance failed.");
        await using var verification = factory.Services.CreateAsyncScope();
        var finalDb = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        Require(provider.Initializations == 1, "Purchase retry initialized a second provider session.");
        Require(await finalDb.Companies.CountAsync() == 2, "Checkout changed the company count.");
        Require(await finalDb.CompanyMemberships.CountAsync(member => member.CompanyId == company.Id) == 2, "Memberships changed.");
        Require(await finalDb.Assets.AnyAsync(asset => asset.Id == ownAssetId && asset.CompanyId == company.Id), "Own asset lost.");
        Require(await finalDb.Assets.AnyAsync(asset => asset.Id == foreignAssetId && asset.CompanyId == other.Id), "Other tenant asset changed.");
        Require(await finalDb.TenantSubscriptions.AnyAsync(subscription => subscription.Id == previousId), "Trial history lost.");
        Require(await finalDb.TenantSubscriptions.CountAsync(subscription => subscription.CompanyId == company.Id && subscription.Status == "Active") == 1,
            "Expected exactly one paid subscription after callback replay.");
        var order = await finalDb.CompanyCheckoutOrders.SingleAsync();
        Require(order.TargetCompanyId == company.Id && order.RequestedByUserId == owner.Id && order.Status == "Provisioned" && order.Amount == 4200m,
            "Order tenant, owner, annual amount or state is incorrect.");
        Console.WriteLine("PASS: PostgreSQL confirms one provider initialization and one active paid period; original company, trial history, owner/viewer memberships and both tenants' assets are preserved.");
    }

    private sealed class SyntheticProvider : HttpMessageHandler
    {
        internal const string Token = "isolated-browser-payment-token";
        internal int Initializations { get; private set; }
        private string? conversation;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            object response;
            if (request.RequestUri!.AbsolutePath.EndsWith("initialize/auth/ecom", StringComparison.Ordinal))
            {
                Initializations++; conversation = payload.RootElement.GetProperty("conversationId").GetString();
                Require(payload.RootElement.GetProperty("price").GetDecimal() == 4200m, "Incorrect provider price.");
                response = new { status = "success", conversationId = conversation, token = Token,
                    paymentPageUrl = "https://sandbox-api.iyzipay.com/checkoutform/isolated-browser" };
            }
            else
            {
                Require(payload.RootElement.GetProperty("token").GetString() == Token, "Unexpected provider token.");
                response = new { status = "success", token = Token, conversationId = conversation, basketId = conversation,
                    price = 4200m, paidPrice = 4200m, currency = "TRY", fraudStatus = 1, paymentStatus = "SUCCESS", paymentId = "isolated-browser-payment" };
            }
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json") };
        }
    }
}
