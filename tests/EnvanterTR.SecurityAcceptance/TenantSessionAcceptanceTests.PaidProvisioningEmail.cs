using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Paid_new_company_mail_failure_rolls_back_tenant_and_preserves_verified_payment_for_retry(bool existingOwner)
    {
        var plan = await SeedTrialPlan();
        var provider = new ExistingCheckoutProvider();
        using var factory = ExistingCheckoutFactory(provider);
        using var client = factory.CreateClient();
        await ConfigureTrialCsrf(client);
        var request = AnonymousVerifiedPurchase(plan) with { OwnerEmail = $"paid-owner-{Guid.NewGuid():N}@example.invalid" };
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (existingOwner) request = request with { OwnerEmail = (await db.Users.SingleAsync(u => u.Id == _userId)).Email! };
        }
        request = request with { EmailVerificationToken = await VerifyCheckoutEmailAsync(client, request.OwnerEmail, plan.Id) };
        using var initialized = await client.PostAsJsonAsync("/api/checkout/initialize", request);
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var oldPort = configuration["Email:Smtp:Port"];
        var unavailable = new TcpListener(IPAddress.Loopback, 0); unavailable.Start();
        var failedPort = ((IPEndPoint)unavailable.LocalEndpoint).Port; unavailable.Stop();
        var beforeMessages = _smtpCapture.Messages.Count;
        string? beforeHash; string? beforeStamp; int beforeMemberships; int beforeCompanies;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var owner = await db.Users.SingleAsync(u => u.Id == _userId);
            beforeHash = owner.PasswordHash; beforeStamp = owner.SecurityStamp;
            beforeMemberships = await db.CompanyMemberships.CountAsync();
            beforeCompanies = await db.Companies.CountAsync();
        }
        try
        {
            configuration["Email:Smtp:Port"] = failedPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
            using var failed = await client.PostAsync("/api/checkout/callback", new FormUrlEncodedContent(new Dictionary<string,string> { ["token"] = provider.Token }));
            Assert.Equal(HttpStatusCode.OK, failed.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var order = await db.CompanyCheckoutOrders.SingleAsync(o => o.Id == request.CheckoutId);
            Assert.Equal("ProvisioningFailed", order.Status);
            Assert.Equal(provider.PaymentId, order.PaymentReference); Assert.NotNull(order.PaidAt); Assert.Null(order.CompanyId);
            Assert.False(await db.Companies.AnyAsync(c => c.Slug == request.Slug));
            Assert.Equal(beforeCompanies, await db.Companies.CountAsync());
            Assert.Equal(beforeMemberships, await db.CompanyMemberships.CountAsync());
            if (!existingOwner) Assert.False(await db.Users.AnyAsync(u => u.Email == request.OwnerEmail));
            var owner = await db.Users.SingleAsync(u => u.Id == _userId);
            Assert.Equal(beforeHash, owner.PasswordHash); Assert.Equal(beforeStamp, owner.SecurityStamp);
            Assert.DoesNotContain("Şirket portalınız:", await failed.Content.ReadAsStringAsync());
            Assert.Equal(beforeMessages, _smtpCapture.Messages.Count);
        }
        finally { configuration["Email:Smtp:Port"] = oldPort; }

        using var retried = await client.PostAsync("/api/checkout/callback", new FormUrlEncodedContent(new Dictionary<string,string> { ["token"] = provider.Token }));
        Assert.Equal(HttpStatusCode.OK, retried.StatusCode);
        Assert.Contains("Şirket portalınız:", await retried.Content.ReadAsStringAsync());
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var order = await db.CompanyCheckoutOrders.SingleAsync(o => o.Id == request.CheckoutId);
            Assert.Equal("Provisioned", order.Status); Assert.Equal(provider.PaymentId, order.PaymentReference);
            var company = await db.Companies.SingleAsync(c => c.Slug == request.Slug);
            Assert.Equal(company.Id, order.CompanyId);
            Assert.Equal(1, await db.CompanyMemberships.CountAsync(m => m.CompanyId == company.Id));
            Assert.Equal(1, await db.TenantSubscriptions.CountAsync(s => s.CompanyId == company.Id));
            Assert.Equal(1, await db.AuditEntries.CountAsync(a => a.CompanyId == company.Id && a.Action == "PaidCompanyProvisioned"));
            Assert.Equal(beforeCompanies + 1, await db.Companies.CountAsync());
            Assert.Equal(beforeMemberships + 1, await db.CompanyMemberships.CountAsync());
            var owner = await db.Users.SingleAsync(u => u.Id == _userId);
            Assert.Equal(beforeHash, owner.PasswordHash); Assert.Equal(beforeStamp, owner.SecurityStamp);
        }
        Assert.Equal(beforeMessages + 1, _smtpCapture.Messages.Count);
        using var replay = await client.PostAsync("/api/checkout/callback", new FormUrlEncodedContent(new Dictionary<string,string> { ["token"] = provider.Token }));
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(beforeMessages + 1, _smtpCapture.Messages.Count);
    }
}
