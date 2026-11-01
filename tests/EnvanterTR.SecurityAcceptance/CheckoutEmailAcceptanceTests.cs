using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    private async Task<string> VerifyCheckoutEmailAsync(HttpClient client, string email, Guid planId)
    {
        if (!client.DefaultRequestHeaders.Contains("X-CSRF-TOKEN")) await ConfigureTrialCsrf(client);
        using var sent = await client.PostAsJsonAsync("/api/checkout/email/request", new RequestCheckoutEmail(email, planId));
        Assert.Equal(HttpStatusCode.Accepted, sent.StatusCode);
        var body = WebUtility.HtmlDecode(DecodeMimeBody(_smtpCapture.Messages.Last()));
        var match = Regex.Match(body, "href=\"([^\"]*checkout\\.html[^\"]*)\"");
        Assert.True(match.Success);
        var link = new Uri(match.Groups[1].Value);
        Assert.Equal("?planId=" + planId, link.Query);
        var token = Uri.UnescapeDataString(link.Fragment["#emailToken=".Length..]);
        Assert.DoesNotContain(token, await sent.Content.ReadAsStringAsync());
        using var confirmed = await client.PostAsJsonAsync("/api/checkout/email/confirm", new ConfirmCheckoutEmail(token));
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        Assert.True(confirmed.Headers.CacheControl?.NoStore);
        var payload = await confirmed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email, payload.GetProperty("email").GetString());
        Assert.Equal(planId, payload.GetProperty("planId").GetGuid());
        return token;
    }

    private static InitializeCompanyCheckoutRequest AnonymousVerifiedPurchase(SubscriptionPlan plan) => new(
        Guid.NewGuid(), "Verified purchase company", "verify-" + Guid.NewGuid().ToString("N")[..12],
        "owner@example.invalid", "Synthetic", "Owner", "owner@example.invalid", "+905550000002", "10000000146",
        "Synthetic Street 2", "Istanbul", "Turkey", "34000", plan.Id, "annual", true, "cloud-checkout-data-v1");

    [Fact]
    public async Task Direct_checkout_email_verification_precedes_provider_initialization()
    {
        var plan = await SeedTrialPlan();
        var provider = new ExistingCheckoutProvider();
        using var factory = ExistingCheckoutFactory(provider);
        using var client = factory.CreateClient();
        await ConfigureTrialCsrf(client);
        var request = AnonymousVerifiedPurchase(plan);
        using var noProof = await client.PostAsJsonAsync("/api/checkout/initialize", request);
        Assert.Equal((HttpStatusCode)428, noProof.StatusCode);
        Assert.Equal(0, provider.Initializations);
        await using (var scope = factory.Services.CreateAsyncScope())
            Assert.False(await scope.ServiceProvider.GetRequiredService<AppDbContext>().CompanyCheckoutOrders.AnyAsync(order => order.Id == request.CheckoutId));
        request = request with { EmailVerificationToken = await VerifyCheckoutEmailAsync(client, request.OwnerEmail, plan.Id) };
        using var paid = await client.PostAsJsonAsync("/api/checkout/initialize", request);
        Assert.Equal(HttpStatusCode.OK, paid.StatusCode);
        using var replay = await client.PostAsJsonAsync("/api/checkout/initialize", request);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(1, provider.Initializations);
        Assert.Equal(2400m, provider.Amount);
        await using var verify = factory.Services.CreateAsyncScope();
        var order = await verify.ServiceProvider.GetRequiredService<AppDbContext>().CompanyCheckoutOrders.SingleAsync(order => order.Id == request.CheckoutId);
        Assert.Equal(request.OwnerEmail, order.OwnerEmail);
        Assert.Equal("PaymentStarted", order.Status);
    }

    [Theory]
    [InlineData("email")]
    [InlineData("plan")]
    [InlineData("tampered")]
    [InlineData("expired")]
    [InlineData("purpose")]
    public async Task Direct_checkout_rejects_invalid_email_proof_without_payment_or_order(string kind)
    {
        var plan = await SeedTrialPlan();
        var provider = new ExistingCheckoutProvider();
        using var factory = ExistingCheckoutFactory(provider);
        using var client = factory.CreateClient();
        await ConfigureTrialCsrf(client);
        var request = AnonymousVerifiedPurchase(plan);
        var token = await VerifyCheckoutEmailAsync(client, request.OwnerEmail, plan.Id);
        if (kind is "expired" or "purpose")
        {
            using var scope = factory.Services.CreateScope();
            var protector = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(kind == "purpose" ? "EnvanterTR.CloudTrial.Email.v1" : "EnvanterTR.Checkout.Email.v1")
                .ToTimeLimitedDataProtector();
            token = protector.Protect(JsonSerializer.Serialize(new { Email = request.OwnerEmail, PlanId = plan.Id }),
                kind == "expired" ? DateTimeOffset.UtcNow.AddMinutes(-1) : DateTimeOffset.UtcNow.AddMinutes(30));
        }
        request = request with { EmailVerificationToken = kind == "tampered" ? token[..^1] + "!" : token,
            OwnerEmail = kind == "email" ? "another@example.invalid" : request.OwnerEmail,
            PlanId = kind == "plan" ? Guid.NewGuid() : plan.Id };
        using var denied = await client.PostAsJsonAsync("/api/checkout/initialize", request);
        Assert.Equal((HttpStatusCode)428, denied.StatusCode);
        Assert.Equal(0, provider.Initializations);
        await using var verification = factory.Services.CreateAsyncScope();
        Assert.False(await verification.ServiceProvider.GetRequiredService<AppDbContext>().CompanyCheckoutOrders.AnyAsync(order => order.Id == request.CheckoutId));
    }

    [Fact]
    public async Task Checkout_email_endpoints_require_csrf_and_fail_closed_without_smtp()
    {
        var plan = await SeedTrialPlan();
        using var factory = ExistingCheckoutFactory(new ExistingCheckoutProvider());
        using var client = factory.CreateClient();
        using var missing = await client.PostAsJsonAsync("/api/checkout/email/request", new RequestCheckoutEmail("owner@example.invalid", plan.Id));
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        using var unconfirmed = await client.PostAsJsonAsync("/api/checkout/email/confirm", new ConfirmCheckoutEmail("bad"));
        Assert.Equal(HttpStatusCode.BadRequest, unconfirmed.StatusCode);
        Assert.Empty(_smtpCapture.Messages);
        using var disabledFactory = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Email:Smtp:Host"] = "" })));
        using var disabled = disabledFactory.CreateClient();
        await ConfigureTrialCsrf(disabled);
        using var status = await disabled.GetAsync("/api/checkout/status");
        Assert.False((await status.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("available").GetBoolean());
        using var unavailable = await disabled.PostAsJsonAsync("/api/checkout/email/request", new RequestCheckoutEmail("owner@example.invalid", plan.Id));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
        Assert.Empty(_smtpCapture.Messages);
        using var failedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Email:Smtp:Host"] = "127.0.0.1", ["Email:Smtp:Port"] = "1" })));
        using var failed = failedFactory.CreateClient();
        await ConfigureTrialCsrf(failed);
        using var deliveryFailure = await failed.PostAsJsonAsync("/api/checkout/email/request", new RequestCheckoutEmail("owner@example.invalid", plan.Id));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, deliveryFailure.StatusCode);
        Assert.Empty(_smtpCapture.Messages);
    }

    [Fact]
    public async Task Checkout_email_rate_limit_is_separate_from_payment_attempts()
    {
        var plan = await SeedTrialPlan();
        var provider = new ExistingCheckoutProvider();
        using var factory = ExistingCheckoutFactory(provider);
        using var client = factory.CreateClient();
        await ConfigureTrialCsrf(client);
        for (var index = 0; index < 5; index++)
        {
            using var sent = await client.PostAsJsonAsync("/api/checkout/email/request", new RequestCheckoutEmail("owner@example.invalid", plan.Id));
            Assert.Equal(HttpStatusCode.Accepted, sent.StatusCode);
        }
        using var throttled = await client.PostAsJsonAsync("/api/checkout/email/request", new RequestCheckoutEmail("owner@example.invalid", plan.Id));
        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.Equal(5, _smtpCapture.Messages.Count);
        using var payment = await client.PostAsJsonAsync("/api/checkout/initialize", AnonymousVerifiedPurchase(plan));
        Assert.Equal((HttpStatusCode)428, payment.StatusCode);
        Assert.Equal(0, provider.Initializations);
    }

    [Fact]
    public async Task Direct_checkout_stops_valid_proof_purchase_when_smtp_configuration_is_removed()
    {
        var plan = await SeedTrialPlan();
        var provider = new ExistingCheckoutProvider();
        using var factory = ExistingCheckoutFactory(provider);
        using var client = factory.CreateClient();
        await ConfigureTrialCsrf(client);
        var request = AnonymousVerifiedPurchase(plan);
        var token = await VerifyCheckoutEmailAsync(client, request.OwnerEmail, plan.Id);
        using var disabledFactory = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Email:Smtp:Host"] = "" })));
        using var disabled = disabledFactory.CreateClient();
        await ConfigureTrialCsrf(disabled);
        using var validProof = await disabled.PostAsJsonAsync("/api/checkout/email/confirm", new ConfirmCheckoutEmail(token));
        Assert.Equal(HttpStatusCode.OK, validProof.StatusCode);
        using var stopped = await disabled.PostAsJsonAsync("/api/checkout/initialize", request with { EmailVerificationToken = token });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, stopped.StatusCode);
        Assert.Equal(0, provider.Initializations);
        await using var scope = disabledFactory.Services.CreateAsyncScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<AppDbContext>().CompanyCheckoutOrders.AnyAsync(order => order.Id == request.CheckoutId));
    }
}
