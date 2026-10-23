using System.Data;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed record RequestCloudTrial(string CompanyName, string Slug, string OwnerName, string Email, Guid PlanId,
    bool DataProcessingAcknowledged, string NoticeVersion);
public sealed record ConfirmCloudTrial(string Token);

public static class CloudTrialEndpoints
{
    public const string NoticeVersion = "cloud-trial-data-v1";
    private const string Purpose = "EnvanterTR.CloudTrial.Email.v1";
    private sealed record VerifiedRequest(Guid RegistrationId, string CompanyName, string Slug, string OwnerName, string Email, Guid PlanId);

    public static IEndpointRouteBuilder MapCloudTrialEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/trials/status", async (IConfiguration configuration, AppDbContext db, CancellationToken ct) => Results.Ok(new
        {
            available = Configured(configuration) && await EligiblePlans(db).AnyAsync(ct), trialDays = AnnualCloudCatalog.TrialDays,
            billingPeriod = "annual", noticeVersion = NoticeVersion
        })).AllowAnonymous();
        endpoints.MapPost("/api/trials/request", RequestAsync).AllowAnonymous().RequireRateLimiting("checkout")
            .WithMetadata(new RequestSizeLimitAttribute(8192));
        endpoints.MapPost("/api/trials/confirm", ConfirmAsync).AllowAnonymous().RequireRateLimiting("checkout")
            .WithMetadata(new RequestSizeLimitAttribute(8192));
        return endpoints;
    }

    private static bool Configured(IConfiguration configuration) => configuration.GetValue<bool>("CloudTrials:Enabled") &&
        !string.IsNullOrWhiteSpace(configuration["Email:Smtp:Host"]) &&
        !string.IsNullOrWhiteSpace(configuration["Email:FromAddress"]) &&
        Uri.TryCreate(configuration["Email:PublicBaseUrl"], UriKind.Absolute, out var site) &&
        site.Scheme == Uri.UriSchemeHttps && site.UserInfo.Length == 0 && site.Query.Length == 0 && site.Fragment.Length == 0;

    private static async Task<IResult> RequestAsync([FromBody] RequestCloudTrial request, HttpContext context,
        IAntiforgery antiforgery, IConfiguration configuration, IDataProtectionProvider protection,
        AppDbContext db, ILoggerFactory logs, CancellationToken ct)
    {
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
        if (!Configured(configuration)) return Results.Problem("Deneme kaydı şu anda kullanılamıyor.", statusCode: 503);
        var name = request.CompanyName?.Trim(); var slug = request.Slug?.Trim().ToLowerInvariant();
        var owner = request.OwnerName?.Trim(); var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200 || !TenantHostResolver.IsValidSlug(slug) ||
            string.IsNullOrWhiteSpace(owner) || owner.Length > 160 || string.IsNullOrWhiteSpace(email) || email.Length > 256 ||
            !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email ||
            !request.DataProcessingAcknowledged || request.NoticeVersion != NoticeVersion)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["trial"] = ["Şirket, portal, yetkili, e-posta ve bilgilendirme onayını kontrol edin."] });
        if (!await EligiblePlan(db, request.PlanId).AnyAsync(ct))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["planId"] = ["Denemeye uygun yıllık Cloud paketi bulunamadı."] });
        var payload = new VerifiedRequest(Guid.NewGuid(), name, slug!, owner, email, request.PlanId);
        var token = protection.CreateProtector(Purpose).ToTimeLimitedDataProtector()
            .Protect(JsonSerializer.Serialize(payload), TimeSpan.FromMinutes(30));
        var link = new UriBuilder(new Uri(new Uri(configuration["Email:PublicBaseUrl"]!), "/trial.html"))
        { Fragment = "token=" + Uri.EscapeDataString(token) }.Uri.AbsoluteUri;
        try { await SendVerificationAsync(email, link, configuration, ct); }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logs.CreateLogger("CloudTrialEmail").LogError("Trial verification delivery failed. Error type: {ErrorType}", error.GetType().Name);
            return Results.Problem("Doğrulama e-postası gönderilemedi. Daha sonra yeniden deneyin.", statusCode: 503);
        }
        return Results.Accepted(value: new { message = "E-posta adresinize doğrulama bağlantısı gönderildi. Bağlantı 30 dakika geçerlidir; doğrulamadan şirket hesabı açılmaz." });
    }

    private static IQueryable<SubscriptionPlan> EligiblePlans(AppDbContext db) =>
        db.SubscriptionPlans.Where(plan => plan.IsActive && plan.IsPublic &&
            plan.DeploymentKind == "cloud" && plan.MaxLoginUsers != null && plan.MonthlyPrice == 0 && plan.AnnualPrice > 0 && plan.Currency == "TRY");

    private static IQueryable<SubscriptionPlan> EligiblePlan(AppDbContext db, Guid planId) => EligiblePlans(db).Where(plan => plan.Id == planId);

    private static async Task<IResult> ConfirmAsync([FromBody] ConfirmCloudTrial request, HttpContext context,
        IAntiforgery antiforgery, IConfiguration configuration, IDataProtectionProvider protection,
        AppDbContext db, UserManager<EnvanterUser> users, TenantProvisioningService provisioning, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
        if (!Configured(configuration)) return Results.Problem("Deneme kaydı şu anda kullanılamıyor.", statusCode: 503);
        if (string.IsNullOrWhiteSpace(request.Token) || request.Token.Length > 7000) return InvalidLink();
        VerifiedRequest? payload;
        try { payload = JsonSerializer.Deserialize<VerifiedRequest>(protection.CreateProtector(Purpose).ToTimeLimitedDataProtector().Unprotect(request.Token)); }
        catch (Exception error) when (error is CryptographicException or JsonException or FormatException) { return InvalidLink(); }
        if (payload is null || payload.RegistrationId == Guid.Empty) return InvalidLink();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var normalizedEmail = users.NormalizeEmail(payload.Email);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({normalizedEmail}, 0))", ct);
        var owner = await users.FindByEmailAsync(payload.Email);
        var existing = await db.Companies.SingleOrDefaultAsync(company => company.Id == payload.RegistrationId, ct);
        if (existing is not null && owner is not null && await db.CompanyMemberships.AnyAsync(member => member.CompanyId == existing.Id && member.UserId == owner.Id && member.RoleKey == "owner", ct))
        {
            await transaction.CommitAsync(ct);
            return await ConfirmationResult(existing, owner, db, users, configuration, ct);
        }
        if (owner is not null && await db.AuditEntries.AnyAsync(entry => entry.ActorUserId == owner.Id && entry.Action == "CloudTrialStarted", ct))
            return Results.Conflict(new { message = "Bu hesap deneme hakkını kullanmış. Mevcut şirket portalınızdan devam edin." });
        var plan = await EligiblePlan(db, payload.PlanId).SingleOrDefaultAsync(ct);
        if (plan is null) return Results.Conflict(new { message = "Seçilen paket artık denemeye açık değil." });
        var now = DateTimeOffset.UtcNow;
        var reserved = new[] { "Pending", "PaymentStarted", "PaymentReview", "ProvisioningFailed" };
        if (await db.Companies.AnyAsync(company => company.Slug == payload.Slug, ct) ||
            await db.CompanyCheckoutOrders.AnyAsync(order => order.Slug == payload.Slug && reserved.Contains(order.Status) && order.ExpiresAt > now, ct))
            return Results.Conflict(new { message = "Bu portal adresi kullanılıyor. Farklı bir adresle yeniden kayıt isteyin." });
        var provisioned = await provisioning.ProvisionCloudAsync(new(payload.CompanyName, payload.Slug, payload.Email,
            payload.OwnerName, payload.Email, plan.Id, now, now.AddDays(AnnualCloudCatalog.TrialDays),
            IsTrial: true, CompanyId: payload.RegistrationId), ct);
        if (provisioned is null) return Results.Conflict(new { message = "Hesap oluşturulamadı. Yeniden kayıt isteyin." });
        var company = provisioned.Company;
        owner = provisioned.Owner;
        db.AuditEntries.Add(new AuditEntry { CompanyId = company.Id, ActorUserId = owner.Id, Action = "CloudTrialStarted",
            TargetType = "Company", TargetId = company.Id, Details = JsonSerializer.Serialize(new { noticeVersion = NoticeVersion, trialDays = AnnualCloudCatalog.TrialDays, planId = plan.Id }) });
        try { await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); }
        catch (DbUpdateException) { return Results.Conflict(new { message = "Deneme kaydı başka bir işlemle çakıştı. Portal adresini kontrol edin." }); }
        return await ConfirmationResult(company, owner, db, users, configuration, ct);
    }

    private static IResult InvalidLink() => Results.BadRequest(new { message = "Doğrulama bağlantısı geçersiz veya süresi dolmuş. Yeniden kayıt isteyin." });

    private static async Task<IResult> ConfirmationResult(Company company, EnvanterUser owner, AppDbContext db,
        UserManager<EnvanterUser> users, IConfiguration configuration, CancellationToken ct)
    {
        var endsAt = await db.TenantSubscriptions.Where(subscription => subscription.CompanyId == company.Id)
            .OrderByDescending(subscription => subscription.StartsAt).Select(subscription => subscription.EndsAt).FirstAsync(ct);
        string? setupUrl = null;
        if (!owner.EmailConfirmed)
        {
            var passwordToken = await users.GeneratePasswordResetTokenAsync(owner);
            setupUrl = new UriBuilder(new Uri(new Uri(configuration["Email:PublicBaseUrl"]!), "/reset-password.html"))
            { Fragment = $"email={Uri.EscapeDataString(owner.Email!)}&token={Uri.EscapeDataString(passwordToken)}" }.Uri.AbsoluteUri;
        }
        return Results.Ok(new { companyId = company.Id, portalUrl = TenantHostResolver.BuildPortalUrl(company.Slug, configuration["TenantRouting:RootDomain"]), setupUrl, endsAt, mfaRequired = true });
    }

    private static async Task SendVerificationAsync(string recipient, string link, IConfiguration configuration, CancellationToken ct)
    {
        if (!int.TryParse(configuration["Email:Smtp:Port"], out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException("SMTP port is invalid.");
        using var message = new MailMessage(new MailAddress(configuration["Email:FromAddress"]!, configuration["Email:FromName"] ?? "EnvanterTR"), new MailAddress(recipient))
        {
            Subject = "EnvanterTR · 15 günlük deneme kaydınızı doğrulayın", SubjectEncoding = Encoding.UTF8, BodyEncoding = Encoding.UTF8, IsBodyHtml = true,
            Body = $"<p>15 günlük Cloud denemenizi başlatmak için e-posta adresinizi doğrulayın.</p><p><a href=\"{WebUtility.HtmlEncode(link)}\">Deneme kaydını doğrula</a></p><p>Bağlantı 30 dakika geçerlidir. Deneme sonunda yıllık tek ödeme seçilir; bu istek ödeme almaz. İsteği siz yapmadıysanız yok sayın.</p>"
        };
        using var client = new SmtpClient(configuration["Email:Smtp:Host"], port)
        { EnableSsl = configuration.GetValue("Email:Smtp:EnableSsl", true) };
        if (!string.IsNullOrWhiteSpace(configuration["Email:Smtp:Username"]))
            client.Credentials = new NetworkCredential(configuration["Email:Smtp:Username"], configuration["Email:Smtp:Password"]);
        await client.SendMailAsync(message, ct);
    }
}
