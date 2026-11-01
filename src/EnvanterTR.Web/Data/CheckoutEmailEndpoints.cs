using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed record RequestCheckoutEmail(string Email, Guid PlanId);
public sealed record ConfirmCheckoutEmail(string Token);

public static partial class CheckoutEndpoints
{
    private const string CheckoutEmailPurpose = "EnvanterTR.Checkout.Email.v1";
    private sealed record CheckoutEmailProof(string Email, Guid PlanId);

    private static void MapCheckoutEmailEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/checkout/email/request", async (RequestCheckoutEmail request, HttpContext context,
            IAntiforgery antiforgery, AppDbContext db, IConfiguration configuration,
            IDataProtectionProvider protection, ILoggerFactory logs, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            try { await antiforgery.ValidateRequestAsync(context); }
            catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
            if (!EmailConfigurationReadiness.Evaluate(configuration).ConfigurationReady)
                return Results.Problem("E-posta doğrulaması şu anda kullanılamıyor.", statusCode: 503);
            var email = request.Email?.Trim();
            if (email is null || email.Length > 256 || !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email ||
                !await db.SubscriptionPlans.AsNoTracking().AnyAsync(plan => plan.Id == request.PlanId && plan.IsActive &&
                    plan.IsPublic && plan.DeploymentKind == "cloud" && (plan.AnnualPrice > 0 || plan.MonthlyPrice > 0), ct))
                return Results.BadRequest(new { message = "E-posta adresini ve satın alınabilir paketi kontrol edin." });
            var token = protection.CreateProtector(CheckoutEmailPurpose).ToTimeLimitedDataProtector()
                .Protect(JsonSerializer.Serialize(new CheckoutEmailProof(email, request.PlanId)), TimeSpan.FromMinutes(30));
            var link = new UriBuilder(new Uri(new Uri(configuration["Email:PublicBaseUrl"]!), "/checkout.html"))
            { Query = "planId=" + request.PlanId, Fragment = "emailToken=" + Uri.EscapeDataString(token) }.Uri.AbsoluteUri;
            try
            {
                using var message = new MailMessage(new MailAddress(configuration["Email:FromAddress"]!,
                    configuration["Email:FromName"] ?? "EnvanterTR"), new MailAddress(email))
                {
                    Subject = "EnvanterTR · Satın alma e-postanızı doğrulayın", SubjectEncoding = Encoding.UTF8,
                    BodyEncoding = Encoding.UTF8, IsBodyHtml = true,
                    Body = $"<p>Cloud satın alma işlemine devam etmek için e-posta adresinizi doğrulayın.</p><p><a href=\"{WebUtility.HtmlEncode(link)}\">E-posta adresimi doğrula</a></p><p>Bağlantı 30 dakika geçerlidir; bu adım ödeme almaz veya şirket oluşturmaz. İsteği siz yapmadıysanız yok sayın.</p>"
                };
                using var smtp = new SmtpClient(configuration["Email:Smtp:Host"], int.Parse(configuration["Email:Smtp:Port"]!))
                { EnableSsl = configuration.GetValue("Email:Smtp:EnableSsl", true) };
                if (!string.IsNullOrWhiteSpace(configuration["Email:Smtp:Username"]))
                    smtp.Credentials = new NetworkCredential(configuration["Email:Smtp:Username"], configuration["Email:Smtp:Password"]);
                await smtp.SendMailAsync(message, ct);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                logs.CreateLogger("CheckoutEmail").LogError("Checkout verification delivery failed. Error type: {ErrorType}", error.GetType().Name);
                return Results.Problem("Doğrulama e-postası gönderilemedi. Daha sonra tekrar deneyin.", statusCode: 503);
            }
            return Results.Accepted(value: new { message = "Doğrulama bağlantısı e-posta adresinize gönderildi. Bağlantı 30 dakika geçerlidir." });
        }).AllowAnonymous().RequireRateLimiting("checkout-email").WithMetadata(new RequestSizeLimitAttribute(8192));

        endpoints.MapPost("/api/checkout/email/confirm", async (ConfirmCheckoutEmail request, HttpContext context,
            IAntiforgery antiforgery, IDataProtectionProvider protection) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            try { await antiforgery.ValidateRequestAsync(context); }
            catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
            var proof = ReadCheckoutEmailProof(request.Token, protection);
            return proof is null ? Results.BadRequest(new { message = "Doğrulama bağlantısı geçersiz veya süresi dolmuş. Yeni bağlantı isteyin." }) :
                Results.Ok(new { email = proof.Email, planId = proof.PlanId });
        }).AllowAnonymous().RequireRateLimiting("checkout-email").WithMetadata(new RequestSizeLimitAttribute(8192));
    }

    private static CheckoutEmailProof? ReadCheckoutEmailProof(string? token, IDataProtectionProvider protection)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 7000) return null;
        try
        {
            var proof = JsonSerializer.Deserialize<CheckoutEmailProof>(protection.CreateProtector(CheckoutEmailPurpose)
                .ToTimeLimitedDataProtector().Unprotect(token));
            return proof is not null && proof.PlanId != Guid.Empty && proof.Email is { Length: > 0 and <= 256 } &&
                MailAddress.TryCreate(proof.Email, out _) ? proof : null;
        }
        catch (Exception error) when (error is CryptographicException or JsonException or FormatException) { return null; }
    }
}
