using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PdfSharp.Pdf.IO;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    private sealed record EmailHandoverFixture(Guid EmployeeId, Guid DocumentId, string Recipient, string Hash,
        string Token, string Cookie)
    {
        public string Url => $"/api/employees/{EmployeeId}/handover-documents/{DocumentId}/email";
        public object Request(Guid key, Guid? resendOf = null) => new
        { idempotencyKey = key, expectedRecipient = Recipient, snapshotSha256 = Hash, resendOfAttemptId = resendOf };
    }

    private async Task<EmailHandoverFixture> CreateEmailHandoverAsync()
    {
        var employeeId = Guid.NewGuid();
        var recipient = $"handover-{employeeId:N}@example.test";
        const string employeeName = "Sentetik E-posta Test Çalışanı";
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Employees.Add(new Employee { Id = employeeId, CompanyId = _companyId, EmployeeNumber = "EMAIL-" + employeeId.ToString("N"),
                DisplayName = employeeName, Email = recipient, IsActive = true });
            if (!await db.CompanyMemberships.AnyAsync(x => x.CompanyId == _otherCompanyId && x.UserId == _userId))
                db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _otherCompanyId, UserId = _userId, RoleKey = "admin" });
            await db.SaveChangesAsync();
        }
        using var csrf = await SendAsAdminAsync("/api/session/csrf");
        using var csrfBody = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var token = csrfBody.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(x => x.Split(';', 2)[0])
            .First(x => x.Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        using var create = await SendAsRoleAsync(HttpMethod.Post, $"/api/employees/{employeeId}/handover-documents", "admin",
            JsonContent.Create(new { }), token, cookie);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var body = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = body.RootElement.GetProperty("id").GetGuid();
        var hash = body.RootElement.GetProperty("snapshotSha256").GetString()!;
        foreach (var signer in new[] { "recipient", "issuer" })
        {
            using var signature = await SendAsRoleAsync(HttpMethod.Post, $"/api/employee-handover-documents/{id}/sign", "admin",
                JsonContent.Create(new { signerType = signer, signerName = employeeName,
                    signaturePngDataUrl = "data:image/png;base64," + HandoverPdfTestSignaturePng }), token, cookie);
            Assert.Equal(HttpStatusCode.OK, signature.StatusCode);
        }
        return new(employeeId, id, recipient, hash, token, cookie);
    }

    [Fact]
    public async Task Handover_email_sends_pdf_once_under_parallel_and_replayed_requests()
    {
        var fixture = await CreateEmailHandoverAsync();
        var key = Guid.NewGuid();
        using (var metadata = await SendAsRoleAsync(HttpMethod.Get, fixture.Url, "admin"))
        {
            Assert.Equal(HttpStatusCode.OK, metadata.StatusCode);
            using var body = JsonDocument.Parse(await metadata.Content.ReadAsStringAsync());
            Assert.True(body.RootElement.GetProperty("ready").GetBoolean());
            Assert.Equal(fixture.Recipient, body.RootElement.GetProperty("recipient").GetString());
        }
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => SendAsRoleAsync(HttpMethod.Post, fixture.Url,
            "admin", JsonContent.Create(fixture.Request(key)), fixture.Token, fixture.Cookie)));
        Guid? deliveryId = null;
        foreach (var response in responses)
        {
            using (response)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var id = body.RootElement.GetProperty("id").GetGuid();
                Assert.True(deliveryId is null || deliveryId == id);
                deliveryId = id;
            }
        }
        using (var replay = await SendAsRoleAsync(HttpMethod.Post, fixture.Url, "admin", JsonContent.Create(fixture.Request(key)), fixture.Token, fixture.Cookie))
        {
            Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
            using var body = JsonDocument.Parse(await replay.Content.ReadAsStringAsync());
            Assert.Equal("Accepted", body.RootElement.GetProperty("status").GetString());
            Assert.Contains("SMTP", body.RootElement.GetProperty("message").GetString());
        }
        using (var changedReplay = await SendAsRoleAsync(HttpMethod.Post, fixture.Url, "admin",
                   JsonContent.Create(new { idempotencyKey = key, expectedRecipient = "other@example.test", snapshotSha256 = fixture.Hash }), fixture.Token, fixture.Cookie))
            Assert.Equal(HttpStatusCode.Conflict, changedReplay.StatusCode);
        using (var unconfirmedResend = await SendAsRoleAsync(HttpMethod.Post, fixture.Url, "admin",
                   JsonContent.Create(fixture.Request(Guid.NewGuid())), fixture.Token, fixture.Cookie))
            Assert.Equal(HttpStatusCode.Conflict, unconfirmedResend.StatusCode);
        var wire = Assert.Single(_smtpCapture.Messages.Where(x => x.Contains(deliveryId!.Value.ToString("D"), StringComparison.Ordinal)));
        Assert.Contains(fixture.Recipient, wire, StringComparison.Ordinal);
        var attachment = Regex.Match(wire, @"(?ms)^Content-Type:\s*application/pdf[^\n]*(?:\n[^\n]+)*?\n\n(?<body>[A-Za-z0-9+/=\n\r]+)");
        Assert.True(attachment.Success, "SMTP message must contain the PDF attachment MIME part.");
        var bytes = Convert.FromBase64String(attachment.Groups["body"].Value);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(bytes, 0, 8));
        using var input = new MemoryStream(bytes);
        using var parsed = PdfReader.Open(input, PdfDocumentOpenMode.Import);
        Assert.True(parsed.PageCount >= 1);
        await using var verifyScope = _factory!.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = Assert.Single(await db.EmployeeHandoverEmailAttempts.Where(x => x.CompanyId == _companyId && x.DocumentId == fixture.DocumentId).ToListAsync());
        Assert.Equal("Accepted", stored.Status);
        Assert.NotNull(stored.AcceptedAt);
        Assert.Equal(fixture.Hash, stored.SnapshotSha256);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), stored.PdfSha256);
        Assert.Equal(2, await db.AuditEntries.CountAsync(x => x.CompanyId == _companyId && x.TargetId == fixture.DocumentId && x.Action.StartsWith("HandoverEmail")));
        var evidence = Environment.GetEnvironmentVariable("ENVANTERTR_HANDOVER_EMAIL_EVIDENCE");
        if (!string.IsNullOrWhiteSpace(evidence)) await File.WriteAllTextAsync(evidence, wire);
    }

    [Fact]
    public async Task Handover_email_rejects_unauthorized_stale_or_unsigned_delivery_and_insecure_production_transport()
    {
        var fixture = await CreateEmailHandoverAsync();
        using (var noCsrf = await SendAsRoleAsync(HttpMethod.Post, fixture.Url, "admin", JsonContent.Create(fixture.Request(Guid.NewGuid()))))
            Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        await SetCompanyRoleAsync("viewer");
        try
        {
            using var viewer = await SendAsRoleAsync(HttpMethod.Post, fixture.Url, "viewer", JsonContent.Create(fixture.Request(Guid.NewGuid())), fixture.Token, fixture.Cookie);
            Assert.Equal(HttpStatusCode.Forbidden, viewer.StatusCode);
        }
        finally { await SetCompanyRoleAsync("admin"); }
        using (var foreign = await SendAsUserAsync(HttpMethod.Post, fixture.Url, _userId, _otherCompanyId,
                   JsonContent.Create(fixture.Request(Guid.NewGuid())), fixture.Token, fixture.Cookie))
            Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        using (var arbitraryRecipient = await SendAsRoleAsync(HttpMethod.Post, fixture.Url, "admin",
                   JsonContent.Create(new { idempotencyKey = Guid.NewGuid(), expectedRecipient = "arbitrary@example.test", snapshotSha256 = fixture.Hash }), fixture.Token, fixture.Cookie))
            Assert.Equal(HttpStatusCode.Conflict, arbitraryRecipient.StatusCode);
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var employee = await db.Employees.SingleAsync(x => x.Id == fixture.EmployeeId && x.CompanyId == _companyId);
        employee.Email = "changed@example.test";
        await db.SaveChangesAsync();
        using (var stale = await SendAsRoleAsync(HttpMethod.Post, fixture.Url, "admin", JsonContent.Create(fixture.Request(Guid.NewGuid())), fixture.Token, fixture.Cookie))
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        employee.Email = fixture.Recipient;
        var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _userId);
        membership.LocationAccessMode = CompanyLocationAccess.Restricted;
        await db.SaveChangesAsync();
        using (var scoped = await SendAsRoleAsync(HttpMethod.Post, fixture.Url, "admin", JsonContent.Create(fixture.Request(Guid.NewGuid())), fixture.Token, fixture.Cookie))
            Assert.Equal(HttpStatusCode.NotFound, scoped.StatusCode);
        membership.LocationAccessMode = CompanyLocationAccess.All;
        var record = await db.EmployeeHandoverDocuments.SingleAsync(x => x.Id == fixture.DocumentId && x.CompanyId == _companyId);
        var originalHash = record.IssuerSignatureSha256;
        record.IssuerSignatureSha256 = new string('0', 64);
        await db.SaveChangesAsync();
        using (var unsigned = await SendAsRoleAsync(HttpMethod.Post, fixture.Url, "admin", JsonContent.Create(fixture.Request(Guid.NewGuid())), fixture.Token, fixture.Cookie))
            Assert.Equal(HttpStatusCode.Conflict, unsigned.StatusCode);
        record.IssuerSignatureSha256 = originalHash;
        await db.SaveChangesAsync();
        var environment = _factory.Services.GetRequiredService<IWebHostEnvironment>();
        var originalEnvironment = environment.EnvironmentName;
        environment.EnvironmentName = "Production";
        try
        {
            using var insecure = await SendAsRoleAsync(HttpMethod.Post, fixture.Url, "admin", JsonContent.Create(fixture.Request(Guid.NewGuid())), fixture.Token, fixture.Cookie);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, insecure.StatusCode);
        }
        finally { environment.EnvironmentName = originalEnvironment; }
        Assert.Empty(await db.EmployeeHandoverEmailAttempts.Where(x => x.CompanyId == _companyId && x.DocumentId == fixture.DocumentId).ToListAsync());
        Assert.Empty(_smtpCapture.Messages.Where(x => x.Contains("X-EnvanterTR-Delivery-Id", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Handover_email_uncertain_delivery_never_retries_without_new_explicit_resend()
    {
        var fixture = await CreateEmailHandoverAsync();
        var configuration = _factory!.Services.GetRequiredService<IConfiguration>();
        var originalPort = configuration["Email:Smtp:Port"];
        var closed = new TcpListener(IPAddress.Loopback, 0); closed.Start();
        var closedPort = ((IPEndPoint)closed.LocalEndpoint).Port; closed.Stop();
        var key = Guid.NewGuid();
        Guid failedId;
        configuration["Email:Smtp:Port"] = closedPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
        try
        {
            using var uncertain = await SendAsRoleAsync(HttpMethod.Post, fixture.Url, "admin", JsonContent.Create(fixture.Request(key)), fixture.Token, fixture.Cookie);
            Assert.Equal(HttpStatusCode.OK, uncertain.StatusCode);
            using var body = JsonDocument.Parse(await uncertain.Content.ReadAsStringAsync());
            Assert.Equal("Uncertain", body.RootElement.GetProperty("status").GetString());
            failedId = body.RootElement.GetProperty("id").GetGuid();
        }
        finally { configuration["Email:Smtp:Port"] = originalPort; }
        using (var replay = await SendAsRoleAsync(HttpMethod.Post, fixture.Url, "admin", JsonContent.Create(fixture.Request(key)), fixture.Token, fixture.Cookie))
        {
            using var body = JsonDocument.Parse(await replay.Content.ReadAsStringAsync());
            Assert.Equal("Uncertain", body.RootElement.GetProperty("status").GetString());
        }
        Assert.Empty(_smtpCapture.Messages.Where(x => x.Contains("X-EnvanterTR-Delivery-Id", StringComparison.Ordinal)));
        using (var unconfirmed = await SendAsRoleAsync(HttpMethod.Post, fixture.Url, "admin",
                   JsonContent.Create(fixture.Request(Guid.NewGuid())), fixture.Token, fixture.Cookie))
            Assert.Equal(HttpStatusCode.Conflict, unconfirmed.StatusCode);
        using (var resend = await SendAsRoleAsync(HttpMethod.Post, fixture.Url, "admin",
                   JsonContent.Create(fixture.Request(Guid.NewGuid(), failedId)), fixture.Token, fixture.Cookie))
        {
            Assert.Equal(HttpStatusCode.OK, resend.StatusCode);
            using var body = JsonDocument.Parse(await resend.Content.ReadAsStringAsync());
            Assert.Equal("Accepted", body.RootElement.GetProperty("status").GetString());
        }
        Assert.Single(_smtpCapture.Messages.Where(x => x.Contains("X-EnvanterTR-Delivery-Id", StringComparison.Ordinal)));
    }
}
