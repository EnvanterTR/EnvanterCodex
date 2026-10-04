using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/setup/status", async (AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            var migrations = db.Database.GetMigrations().ToArray();
            if (migrations.Length == 0)
                return Results.Problem("Veritabanı şeması henüz hazırlanmadı; ilk yönetici kurulumu güvenlik için kapalı.", statusCode: StatusCodes.Status503ServiceUnavailable);
            if ((await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
                return Results.Problem("Bekleyen veritabanı güncellemesi var; önce yedek alıp kontrollü güncelleme uygulayın.", statusCode: StatusCodes.Status503ServiceUnavailable);
            var configured = await db.Companies.AnyAsync(cancellationToken);
            return Results.Ok(new
            {
                setupRequired = !configured,
                setupTokenRequired = !configured,
                setupTokenConfigured = !string.IsNullOrWhiteSpace(configuration["Setup:BootstrapToken"])
            });
        }).AllowAnonymous();

        endpoints.MapGet("/api/session/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new { requestToken = tokens.RequestToken });
        }).AllowAnonymous();

        endpoints.MapGet("/api/platform/bootstrap/status", async (AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!db.Database.GetMigrations().Any() || (await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
                return Results.Problem("Veritabanı şeması hazır değil.", statusCode: StatusCodes.Status503ServiceUnavailable);
            var configured = await db.PlatformAdministrators.AnyAsync(cancellationToken);
            return Results.Ok(new
            {
                setupRequired = !configured,
                setupTokenConfigured = !string.IsNullOrWhiteSpace(configuration["Setup:PlatformBootstrapToken"])
            });
        }).AllowAnonymous();

        endpoints.MapPost("/api/setup/initial-owner", async ([FromBody] InitialOwnerRequest request,
            HttpContext context, IAntiforgery antiforgery, IConfiguration configuration,
            AppDbContext db, UserManager<EnvanterUser> userManager, CancellationToken cancellationToken) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            var suppliedToken = context.Request.Headers["X-Setup-Token"].ToString();
            var expectedToken = configuration["Setup:BootstrapToken"];
            if (!TokensMatch(suppliedToken, expectedToken)) return Results.NotFound();

            if (string.IsNullOrWhiteSpace(request.CompanyName) || request.CompanyName.Trim().Length > 200 ||
                string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length > 160 ||
                string.IsNullOrWhiteSpace(request.Email) || request.Email.Trim().Length > 256 ||
                string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["setup"] = ["Firma adı, yönetici adı, e-posta ve en az 12 karakterli parola gereklidir."]
                });

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            if (await db.Companies.AnyAsync(cancellationToken)) return Results.Conflict(new { message = "İlk kurulum zaten tamamlanmış." });

            var company = new Company { Name = request.CompanyName.Trim() };
            var email = request.Email.Trim();
            var user = await userManager.FindByEmailAsync(email);
            if (user is not null)
            {
                if (!user.EmailConfirmed || !await userManager.CheckPasswordAsync(user, request.Password))
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["account"] = ["Bu e-posta mevcut bir hesaba ait; hesabı bağlamak için mevcut parolasını girin."]
                    });
            }
            else
            {
                user = new EnvanterUser
                {
                    UserName = email, Email = email, EmailConfirmed = true,
                    DisplayName = request.FullName.Trim()
                };
                var createResult = await userManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["account"] = createResult.Errors.Select(x => x.Description).ToArray()
                    });
                }
            }
            db.Companies.Add(company);

            db.CompanyMemberships.Add(new CompanyMembership
            {
                CompanyId = company.Id, UserId = user.Id, RoleKey = "owner"
            });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "İlk kurulum çakıştı; sistem durumunu yenileyip tekrar deneyin." });
            }

            return Results.Created("/api/session/login", new { companyId = company.Id, email = user.Email, message = "İlk yönetici oluşturuldu. Şimdi oturum açabilirsiniz." });
        }).AllowAnonymous().RequireRateLimiting("login");

        endpoints.MapPost("/api/platform/bootstrap", async ([FromBody] PlatformBootstrapRequest request,
            HttpContext context, IAntiforgery antiforgery, IConfiguration configuration,
            AppDbContext db, UserManager<EnvanterUser> userManager, CancellationToken cancellationToken) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            if (!TokensMatch(context.Request.Headers["X-Platform-Setup-Token"].ToString(), configuration["Setup:PlatformBootstrapToken"]))
                return Results.NotFound();
            if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length > 160 ||
                string.IsNullOrWhiteSpace(request.Email) || request.Email.Trim().Length > 256 ||
                string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["account"] = ["Ad, geçerli e-posta ve en az 12 karakterli parola gereklidir."] });

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            if (await db.PlatformAdministrators.AnyAsync(cancellationToken))
                return Results.Conflict(new { message = "Platform yönetici hesabı zaten oluşturulmuş." });
            var email = request.Email.Trim();
            var user = await userManager.FindByEmailAsync(email);
            if (user is not null)
            {
                if (!user.EmailConfirmed || !await userManager.CheckPasswordAsync(user, request.Password))
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["account"] = ["Bu e-posta mevcut bir hesaba ait; hesabı bağlamak için mevcut parolasını girin."]
                    });
            }
            else
            {
                user = new EnvanterUser
                {
                    UserName = email, Email = email, EmailConfirmed = true,
                    DisplayName = request.FullName.Trim()
                };
                var result = await userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["account"] = result.Errors.Select(x => x.Description).ToArray()
                    });
                }
            }
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = user.Id });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "Platform hesabı oluşturma çakıştı." });
            }
            return Results.Created("/api/session/login", new { email = user.Email, message = "Platform yöneticisi oluşturuldu; bootstrap anahtarını ortam değişkeninden kaldırın." });
        }).AllowAnonymous().RequireRateLimiting("login");

        endpoints.MapPost("/api/session/login", async ([FromBody] LoginRequest request,
            HttpContext context, IAntiforgery antiforgery, UserManager<EnvanterUser> userManager,
            SignInManager<EnvanterUser> signInManager, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["credentials"] = ["E-posta ve parola zorunludur."] });

            var user = await userManager.FindByEmailAsync(request.Email.Trim());
            if (user is null) return Results.Unauthorized();
            var passwordCheck = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!passwordCheck.Succeeded)
                return passwordCheck.IsLockedOut
                    ? Results.Json(new { message = "Çok sayıda hatalı deneme. Bir süre sonra tekrar deneyin." }, statusCode: StatusCodes.Status423Locked)
                    : Results.Unauthorized();
            if (!user.EmailConfirmed)
                return Results.Json(new { message = "Davet e-postasındaki bağlantıdan parolanızı belirleyerek hesabınızı etkinleştirin." }, statusCode: StatusCodes.Status403Forbidden);

            var isPlatformAdmin = await db.PlatformAdministrators.AsNoTracking().AnyAsync(x => x.UserId == user.Id, cancellationToken);
            var memberships = await db.CompanyMemberships.AsNoTracking().Where(x => x.UserId == user.Id).ToListAsync(cancellationToken);
            var membership = request.CompanyId is Guid selected
                ? memberships.SingleOrDefault(x => x.CompanyId == selected)
                : memberships.Count == 1 ? memberships[0] : null;
            if (request.CompanyId is not null && membership is null)
                return Results.Forbid();
            if (membership is null && !isPlatformAdmin)
                return memberships.Count == 0
                    ? Results.Forbid()
                    : Results.ValidationProblem(new Dictionary<string, string[]> { ["companyId"] = ["Birden fazla şirket erişiminiz var; şirket seçin."] });
            if (membership is not null && !await db.Companies.AsNoTracking()
                    .AnyAsync(x => x.Id == membership.CompanyId && x.IsActive, cancellationToken))
                return Results.Forbid();

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.DisplayName)
            };
            if (membership is not null)
            {
                claims.Add(new Claim("company_id", membership.CompanyId.ToString()));
                claims.Add(new Claim("company_role", membership.RoleKey));
            }
            if (isPlatformAdmin) claims.Add(new Claim("platform_admin", "true"));
            await signInManager.SignInWithClaimsAsync(user, request.RememberMe, claims);
            return Results.Ok(new { authenticated = true, companyId = membership?.CompanyId, role = membership?.RoleKey,
                platformAdmin = isPlatformAdmin, name = user.DisplayName });
        }).AllowAnonymous().RequireRateLimiting("login");

        endpoints.MapPost("/api/session/password/forgot", async ([FromBody] ForgotPasswordRequest request,
            HttpContext context, IAntiforgery antiforgery, UserManager<EnvanterUser> userManager,
            IConfiguration configuration, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery))
                return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            const string safeResponse = "Bu e-posta bir hesaba bağlıysa parola yenileme bağlantısı gönderilecektir.";
            var email = request.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email) || email.Length > 256 || !MailAddress.TryCreate(email, out _))
                return Results.Accepted(value: new { message = safeResponse });

            var user = await userManager.FindByEmailAsync(email);
            if (user is not null && user.EmailConfirmed)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                try
                {
                    await SendPasswordResetEmailAsync(email, token, configuration, cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // Never log the address, token, or reset URL. Configuration faults need an operator-visible event.
                    loggerFactory.CreateLogger("PasswordResetEmail").LogError(
                        "Password reset email delivery failed. Check the SMTP and public URL configuration. Error type: {ErrorType}",
                        exception.GetType().Name);
                }
            }
            else if (string.IsNullOrWhiteSpace(configuration["Email:Smtp:Host"]) ||
                     string.IsNullOrWhiteSpace(configuration["Email:PublicBaseUrl"]))
            {
                loggerFactory.CreateLogger("PasswordResetEmail").LogWarning(
                    "Password reset email was not sent because Email:Smtp:Host or Email:PublicBaseUrl is missing.");
            }
            return Results.Accepted(value: new { message = safeResponse });
        }).AllowAnonymous().RequireRateLimiting("login");

        endpoints.MapPost("/api/session/password/reset", async ([FromBody] ResetPasswordRequest request,
            HttpContext context, IAntiforgery antiforgery, UserManager<EnvanterUser> userManager,
            CancellationToken cancellationToken) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery))
                return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 256 ||
                string.IsNullOrWhiteSpace(request.Token) || request.Token.Length > 4096 ||
                string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length > 256)
                return Results.ValidationProblem(new Dictionary<string, string[]> {
                    ["reset"] = ["E-posta, geçerli yenileme bağlantısı ve yeni parola gereklidir."]
                });

            var user = await userManager.FindByEmailAsync(request.Email.Trim());
            if (user is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> {
                    ["reset"] = ["Bağlantı geçersiz veya süresi dolmuş. Yeni bir parola yenileme bağlantısı isteyin."]
                });
            var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
                return Results.ValidationProblem(new Dictionary<string, string[]> {
                    ["reset"] = result.Errors.Select(error => error.Description).ToArray()
                });
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                var confirmResult = await userManager.UpdateAsync(user);
                if (!confirmResult.Succeeded)
                    return Results.Problem("Parolanız kaydedildi ancak davet hesabı etkinleştirilemedi. Aynı bağlantıyla yeniden deneyin.", statusCode: StatusCodes.Status500InternalServerError);
            }
            return Results.Ok(new { message = "Parolanız yenilendi. Yeni parolanızla giriş yapabilirsiniz." });
        }).AllowAnonymous().RequireRateLimiting("login");

        endpoints.MapPost("/api/session/logout", async (HttpContext context, IAntiforgery antiforgery,
            SignInManager<EnvanterUser> signInManager) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            await signInManager.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        endpoints.MapGet("/api/session", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            var companyId = Guid.TryParse(user.FindFirstValue("company_id"), out var parsedCompanyId)
                ? parsedCompanyId : (Guid?)null;
            var companyName = companyId is Guid id
                ? await db.Companies.AsNoTracking().Where(x => x.Id == id && x.IsActive)
                    .Select(x => x.Name).SingleOrDefaultAsync(cancellationToken)
                : null;
            return Results.Ok(new
            {
                userId = user.FindFirstValue(ClaimTypes.NameIdentifier),
                companyId = companyId?.ToString(),
                companyName,
                role = user.FindFirstValue("company_role"),
                platformAdmin = user.HasClaim("platform_admin", "true"),
                name = user.Identity?.Name
            });
        }).RequireAuthorization();

        return endpoints;
    }

    private static async Task<bool> HasValidCsrfAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }

    private static bool TokensMatch(string supplied, string? expected)
    {
        if (string.IsNullOrWhiteSpace(expected)) return false;
        var left = Encoding.UTF8.GetBytes(supplied);
        var right = Encoding.UTF8.GetBytes(expected);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }

    private static Task SendPasswordResetEmailAsync(string recipient, string token,
        IConfiguration configuration, CancellationToken cancellationToken) =>
        SendAccountLinkEmailAsync(recipient, token, "EnvanterTR hesap parolası yenileme",
            "EnvanterTR hesabınız için parola yenileme isteği alındı.", null, configuration, cancellationToken);

    public static Task SendAccountInvitationEmailAsync(string recipient, string displayName, string token,
        IConfiguration configuration, CancellationToken cancellationToken) =>
        SendAccountLinkEmailAsync(recipient, token, "EnvanterTR şirket alanı daveti",
            $"Merhaba {WebUtility.HtmlEncode(displayName)}, EnvanterTR şirket alanına davet edildiniz. Giriş için parolanızı belirleyin.",
            "Parola belirle ve hesabı etkinleştir", configuration, cancellationToken);

    private static async Task SendAccountLinkEmailAsync(string recipient, string token, string subject,
        string intro, string? linkText, IConfiguration configuration, CancellationToken cancellationToken)
    {
        var host = configuration["Email:Smtp:Host"];
        var publicBaseUrl = configuration["Email:PublicBaseUrl"]?.TrimEnd('/');
        var fromAddress = configuration["Email:FromAddress"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(publicBaseUrl) ||
            string.IsNullOrWhiteSpace(fromAddress))
            throw new InvalidOperationException("Password reset email configuration is incomplete.");
        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme is not ("https" or "http"))
            throw new InvalidOperationException("Email:PublicBaseUrl must be an absolute HTTP(S) URL.");
        if (baseUri.Scheme != Uri.UriSchemeHttps && !string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Password reset links must use HTTPS outside development.");
        if (!int.TryParse(configuration["Email:Smtp:Port"], out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException("Email:Smtp:Port must be a valid TCP port.");

        var from = new MailAddress(fromAddress, configuration["Email:FromName"] ?? "EnvanterTR");
        var resetUri = new Uri(baseUri, "/reset-password.html");
        var builder = new UriBuilder(resetUri)
        {
            // Keep the credential-bearing token in the URL fragment. Browsers do not send fragments
            // in HTTP requests, referrers, or normal reverse-proxy access logs.
            Fragment = $"email={Uri.EscapeDataString(recipient)}&token={Uri.EscapeDataString(token)}"
        };
        var safeUrl = WebUtility.HtmlEncode(builder.Uri.AbsoluteUri);
        using var message = new MailMessage(from, new MailAddress(recipient))
        {
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true,
            Body = $"<p>{intro}</p><p><a href=\"{safeUrl}\">{WebUtility.HtmlEncode(linkText ?? "Yeni parola belirle")}</a></p><p>Bu isteği siz yapmadıysanız e-postayı yok sayabilirsiniz.</p>"
        };
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = !string.Equals(configuration["Email:Smtp:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15000
        };
        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password ?? string.Empty);
        await client.SendMailAsync(message, cancellationToken);
    }
}
