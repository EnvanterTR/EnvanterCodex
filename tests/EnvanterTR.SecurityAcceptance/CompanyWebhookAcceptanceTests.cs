using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Company_webhooks_are_tenant_scoped_validated_and_never_expose_targets()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var token = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrfResponse.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        const string slackSecretPath = "/services/T0WEBHOOK/B0WEBHOOK/SLACKSECRETPATHSENTINEL";
        var events = new[] { "asset.assigned", "asset.returned" };

        // CSRF olmadan yazma reddedilir.
        using (var noCsrf = await SendAsUserAsync(HttpMethod.Post, "/api/integrations/webhooks", _userId, _companyId,
            JsonContent.Create(new { name = "Kanal", kind = "slack", url = "https://hooks.slack.com" + slackSecretPath, events })))
            Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);

        // Ayar yetkisi olmayan rol göremez ve yazamaz.
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var member = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId);
            member.RoleKey = "viewer";
            await db.SaveChangesAsync();
        }
        using (var viewer = await SendAsUserAsync(HttpMethod.Get, "/api/integrations/webhooks", _sameCompanyOtherUserId, _companyId, role: "viewer"))
            Assert.Equal(HttpStatusCode.Forbidden, viewer.StatusCode);
        using (var viewerWrite = await SendAsUserAsync(HttpMethod.Post, "/api/integrations/webhooks", _sameCompanyOtherUserId, _companyId,
            JsonContent.Create(new { name = "Kanal", kind = "slack", url = "https://hooks.slack.com" + slackSecretPath, events }), token, cookie, role: "viewer"))
            Assert.Equal(HttpStatusCode.Forbidden, viewerWrite.StatusCode);

        // Adres kuralları: yalnız https, iç ağ/IP yok, Slack ve Teams için doğru ana makine.
        foreach (var (kind, url) in new[]
        {
            ("generic", "http://example.com/hook"), ("generic", "https://localhost/hook"), ("generic", "https://10.0.0.5/hook"),
            ("generic", "https://user:pass@example.com/hook"), ("generic", "https://example.com:8443/hook"), ("generic", "https://intranet/hook"),
            ("slack", "https://evil.example.com/services/x"), ("teams", "https://hooks.slack.com/services/x")
        })
        {
            using var rejected = await SendAsUserAsync(HttpMethod.Post, "/api/integrations/webhooks", _userId, _companyId,
                JsonContent.Create(new { name = "Kanal", kind, url, events }), token, cookie);
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        }
        using (var badEvent = await SendAsUserAsync(HttpMethod.Post, "/api/integrations/webhooks", _userId, _companyId,
            JsonContent.Create(new { name = "Kanal", kind = "slack", url = "https://hooks.slack.com" + slackSecretPath, events = new[] { "asset.deleted-forever" } }), token, cookie))
            Assert.Equal(HttpStatusCode.BadRequest, badEvent.StatusCode);

        Guid slackId;
        using (var created = await SendAsUserAsync(HttpMethod.Post, "/api/integrations/webhooks", _userId, _companyId,
            JsonContent.Create(new { name = "BT kanalı", kind = "slack", url = "https://hooks.slack.com" + slackSecretPath, events }), token, cookie))
        {
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var body = await created.Content.ReadAsStringAsync();
            Assert.DoesNotContain("SLACKSECRETPATH", body, StringComparison.Ordinal);
            using var payload = JsonDocument.Parse(body);
            slackId = payload.RootElement.GetProperty("webhook").GetProperty("id").GetGuid();
            Assert.Equal(JsonValueKind.Null, payload.RootElement.GetProperty("signingSecret").ValueKind);
        }

        string signingSecret;
        Guid genericId;
        using (var created = await SendAsUserAsync(HttpMethod.Post, "/api/integrations/webhooks", _userId, _companyId,
            JsonContent.Create(new { name = "ITSM", kind = "generic", url = "https://envantertr-webhook-test.invalid/hook", events }), token, cookie))
        {
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var payload = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            genericId = payload.RootElement.GetProperty("webhook").GetProperty("id").GetGuid();
            signingSecret = payload.RootElement.GetProperty("signingSecret").GetString()!;
            Assert.StartsWith("whsec_", signingSecret, StringComparison.Ordinal);
        }

        Guid foreignId;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await db.CompanyWebhooks.AsNoTracking().SingleAsync(x => x.Id == slackId);
            Assert.Equal(_companyId, stored.CompanyId);
            Assert.StartsWith("enc:v1:", stored.ProtectedUrl, StringComparison.Ordinal);
            Assert.DoesNotContain("SLACKSECRETPATH", stored.ProtectedUrl, StringComparison.Ordinal);
            var generic = await db.CompanyWebhooks.AsNoTracking().SingleAsync(x => x.Id == genericId);
            Assert.DoesNotContain(signingSecret, generic.ProtectedSigningSecret!, StringComparison.Ordinal);
            var foreign = new CompanyWebhook
            {
                CompanyId = _otherCompanyId, Name = "Company B private channel", Kind = "slack", ProtectedUrl = "enc:v1:x", UrlHint = "hooks.slack.com/…B000",
                EventsJson = "[\"asset.assigned\"]"
            };
            db.CompanyWebhooks.Add(foreign);
            await db.SaveChangesAsync();
            foreignId = foreign.Id;
        }

        using (var list = await SendAsUserAsync(HttpMethod.Get, "/api/integrations/webhooks", _userId, _companyId))
        {
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            var body = await list.Content.ReadAsStringAsync();
            Assert.Contains("BT kanalı", body, StringComparison.Ordinal);
            Assert.DoesNotContain("Company B private channel", body, StringComparison.Ordinal);
            Assert.DoesNotContain("SLACKSECRETPATH", body, StringComparison.Ordinal);
            Assert.DoesNotContain(signingSecret, body, StringComparison.Ordinal);
            Assert.DoesNotContain("enc:v1:", body, StringComparison.Ordinal);
        }

        // Başka şirketin webhook'u bu şirketin oturumundan değiştirilemez, denenemez, silinemez.
        using (var put = await SendAsUserAsync(HttpMethod.Put, $"/api/integrations/webhooks/{foreignId}", _userId, _companyId,
            JsonContent.Create(new { isActive = false }), token, cookie))
            Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        using (var test = await SendAsUserAsync(HttpMethod.Post, $"/api/integrations/webhooks/{foreignId}/test", _userId, _companyId,
            JsonContent.Create(new { }), token, cookie))
            Assert.Equal(HttpStatusCode.NotFound, test.StatusCode);
        using (var delete = await SendAsUserAsync(HttpMethod.Delete, $"/api/integrations/webhooks/{foreignId}", _userId, _companyId,
            requestToken: token, antiforgeryCookie: cookie))
            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        // Ulaşılamayan hedefe test: istisna yok, hata kaydedilir.
        using (var test = await SendAsUserAsync(HttpMethod.Post, $"/api/integrations/webhooks/{genericId}/test", _userId, _companyId,
            JsonContent.Create(new { }), token, cookie))
        {
            Assert.Equal(HttpStatusCode.OK, test.StatusCode);
            using var payload = JsonDocument.Parse(await test.Content.ReadAsStringAsync());
            Assert.False(payload.RootElement.GetProperty("delivered").GetBoolean());
        }

        // Türü sonradan değiştirilemez; duraklatma ve silme çalışır.
        using (var kindChange = await SendAsUserAsync(HttpMethod.Put, $"/api/integrations/webhooks/{slackId}", _userId, _companyId,
            JsonContent.Create(new { kind = "generic" }), token, cookie))
            Assert.Equal(HttpStatusCode.BadRequest, kindChange.StatusCode);
        using (var pause = await SendAsUserAsync(HttpMethod.Put, $"/api/integrations/webhooks/{slackId}", _userId, _companyId,
            JsonContent.Create(new { isActive = false }), token, cookie))
            Assert.Equal(HttpStatusCode.OK, pause.StatusCode);
        using (var delete = await SendAsUserAsync(HttpMethod.Delete, $"/api/integrations/webhooks/{slackId}", _userId, _companyId,
            requestToken: token, antiforgeryCookie: cookie))
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.CompanyWebhooks.AnyAsync(x => x.Id == slackId));
            Assert.True(await db.CompanyWebhooks.AnyAsync(x => x.Id == foreignId));
            var failed = await db.CompanyWebhooks.AsNoTracking().SingleAsync(x => x.Id == genericId);
            Assert.False(string.IsNullOrEmpty(failed.LastError));
            Assert.Equal(1, failed.ConsecutiveFailures);
            Assert.True(await db.AuditEntries.AnyAsync(x => x.CompanyId == _companyId && x.Action == "webhook.created"));
        }
    }

    [Fact]
    public async Task Company_webhook_worker_delivers_only_own_company_events_once_with_valid_signature()
    {
        Guid webhookId;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var protection = scope.ServiceProvider.GetRequiredService<CompanyWebhookProtection>();
            var webhook = new CompanyWebhook
            {
                CompanyId = _companyId, Name = "Worker test", Kind = "generic", ProtectedUrl = "pending", UrlHint = "receiver.example.invalid/…hook",
                EventsJson = "[\"asset.assigned\"]", CursorOccurredAt = DateTimeOffset.UtcNow.AddMinutes(-1)
            };
            webhook.ProtectedUrl = protection.Protect(_companyId, webhook.Id, "https://receiver.example.invalid/hook");
            webhook.ProtectedSigningSecret = protection.Protect(_companyId, webhook.Id, "whsec_worker_test_secret");
            db.CompanyWebhooks.Add(webhook);
            db.AssetEvents.AddRange(
                new AssetEvent { CompanyId = _companyId, AssetId = _ownAssetId, EventType = "Assigned", EmployeeId = _ownEmployeeId, Details = "own assignment" },
                new AssetEvent { CompanyId = _companyId, AssetId = _ownAssetId, EventType = "NoteAdded", Details = "not subscribed" },
                new AssetEvent { CompanyId = _otherCompanyId, AssetId = _otherCompanyAssetId, EventType = "Assigned", Details = "FOREIGN-COMPANY-EVENT" });
            await db.SaveChangesAsync();
            webhookId = webhook.Id;
        }

        var handler = new RecordingWebhookHandler();
        for (var run = 0; run < 2; run++)
        {
            await using var scope = _factory!.Services.CreateAsyncScope();
            var service = new CompanyWebhookDeliveryService(scope.ServiceProvider.GetRequiredService<AppDbContext>(), new SingleClientFactory(handler),
                scope.ServiceProvider.GetRequiredService<CompanyWebhookProtection>(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<CompanyWebhookDeliveryService>.Instance);
            await service.RunOnceAsync(CancellationToken.None);
        }

        var delivered = Assert.Single(handler.Requests.Where(x => x.Body.Contains("\"asset.assigned\"", StringComparison.Ordinal)));
        Assert.DoesNotContain(handler.Requests, x => x.Body.Contains("FOREIGN-COMPANY-EVENT", StringComparison.Ordinal) || x.Body.Contains("not subscribed", StringComparison.Ordinal));
        Assert.Equal("https://receiver.example.invalid/hook", delivered.Url);
        var expected = "sha256=" + Convert.ToHexString(System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("whsec_worker_test_secret"),
            System.Text.Encoding.UTF8.GetBytes($"{delivered.Timestamp}.{delivered.Body}"))).ToLowerInvariant();
        Assert.Equal(expected, delivered.Signature);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await db.CompanyWebhooks.AsNoTracking().SingleAsync(x => x.Id == webhookId);
            Assert.Equal(0, stored.ConsecutiveFailures);
            Assert.Equal(200, stored.LastStatusCode);
        }
    }

    private sealed record RecordedWebhook(string Url, string Body, string? Signature, string? Timestamp);

    private sealed class RecordingWebhookHandler : HttpMessageHandler
    {
        public List<RecordedWebhook> Requests { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new RecordedWebhook(request.RequestUri!.AbsoluteUri, await request.Content!.ReadAsStringAsync(cancellationToken),
                request.Headers.TryGetValues("X-EnvanterTR-Signature", out var signature) ? signature.Single() : null,
                request.Headers.TryGetValues("X-EnvanterTR-Timestamp", out var timestamp) ? timestamp.Single() : null));
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    [Fact]
    public void Company_webhook_target_policy_blocks_private_and_reserved_addresses()
    {
        foreach (var address in new[] { "127.0.0.1", "10.1.2.3", "172.16.0.1", "192.168.1.1", "169.254.169.254", "100.64.0.1", "0.0.0.0", "::1", "fd00::1", "fe80::1", "::ffff:127.0.0.1" })
            Assert.False(CompanyWebhookUrlPolicy.IsPublic(IPAddress.Parse(address)), address);
        foreach (var address in new[] { "8.8.8.8", "52.96.0.1", "2606:4700:4700::1111" })
            Assert.True(CompanyWebhookUrlPolicy.IsPublic(IPAddress.Parse(address)), address);
        Assert.True(CompanyWebhookUrlPolicy.TryValidate("teams", "https://prod-12.westeurope.logic.azure.com/workflows/abc", out _, out _));
        Assert.True(CompanyWebhookUrlPolicy.TryValidate("teams", "https://contoso.webhook.office.com/webhookb2/abc", out _, out _));
        Assert.False(CompanyWebhookUrlPolicy.TryValidate("teams", "https://webhook.office.com.evil.example/abc", out _, out _));
        Assert.False(CompanyWebhookUrlPolicy.TryValidate("slack", "https://hooks.slack.com.evil.example/services/x", out _, out _));
    }
}
