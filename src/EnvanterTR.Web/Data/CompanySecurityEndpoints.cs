using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class CompanySecurityEndpoints
{
    private static readonly TimeSpan EnrollmentLifetime = TimeSpan.FromMinutes(15);

    public static IEndpointRouteBuilder MapCompanySecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/company-security").RequireAuthorization("settings.manage");
        api.MapGet("", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var company = await db.Companies.AsNoTracking().Where(item => item.Id == companyId && item.IsActive)
                .Select(item => new CompanySecurityPolicyRequest(item.SecurityPasswordMinimumLength,
                    item.SecuritySessionDurationHours, item.SecurityRequireMfa)).SingleOrDefaultAsync(ct);
            return company is null ? Results.NotFound() : Results.Ok(company);
        });

        api.MapPut("", async ([FromBody] CompanySecurityPolicyRequest request, ClaimsPrincipal user,
            HttpContext context, IAntiforgery antiforgery, AppDbContext db, CancellationToken ct) =>
        {
            if (!await ValidateCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
                return Results.Forbid();
            if (request.PasswordMinimumLength is < 12 or > 64 || request.SessionDurationHours is < 1 or > 720)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["passwordMinimumLength"] = ["Parola uzunluğu 12–64 karakter arasında olmalıdır."],
                    ["sessionDurationHours"] = ["Oturum süresi 1–720 saat arasında olmalıdır."]
                });

            var company = await db.Companies.SingleOrDefaultAsync(item => item.Id == companyId && item.IsActive, ct);
            if (company is null) return Results.NotFound();
            var before = new CompanySecurityPolicyRequest(company.SecurityPasswordMinimumLength,
                company.SecuritySessionDurationHours, company.SecurityRequireMfa);
            if (before == request) return Results.Ok(before);
            company.SecurityPasswordMinimumLength = request.PasswordMinimumLength;
            company.SecuritySessionDurationHours = request.SessionDurationHours;
            company.SecurityRequireMfa = request.RequireMfa;
            var activeSessions = await db.UserSessions.Where(session => session.CompanyId == companyId && session.RevokedAt == null).ToListAsync(ct);
            foreach (var session in activeSessions)
            {
                var policyExpiry = session.CreatedAt.AddHours(request.SessionDurationHours);
                if (session.ExpiresAt > policyExpiry) session.ExpiresAt = policyExpiry;
            }
            if (request.RequireMfa)
            {
                var usersWithoutMfa = await db.CompanyMemberships.AsNoTracking()
                    .Join(db.Users.AsNoTracking(), member => member.UserId, account => account.Id,
                        (member, account) => new { member.CompanyId, account.Id, account.TwoFactorEnabled })
                    .Where(item => item.CompanyId == companyId && !item.TwoFactorEnabled)
                    .Select(item => item.Id).ToListAsync(ct);
                if (usersWithoutMfa.Count > 0)
                    await db.UserSessions.Where(session => session.CompanyId == companyId && usersWithoutMfa.Contains(session.UserId) && session.RevokedAt == null)
                        .ExecuteUpdateAsync(update => update.SetProperty(session => session.RevokedAt, DateTimeOffset.UtcNow), ct);
            }
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId, ActorUserId = actorId, TargetType = "CompanySecurityPolicy", TargetId = companyId,
                Action = "CompanySecurityPolicyChanged",
                Details = $"Parola en az {request.PasswordMinimumLength} karakter; oturum en fazla {request.SessionDurationHours} saat; zorunlu MFA: {(request.RequireMfa ? "açık" : "kapalı")}."
            });
            await db.SaveChangesAsync(ct);
            return Results.Ok(request);
        });

        endpoints.MapPost("/api/session/company-mfa/setup", async ([FromBody] CompanyMfaEnrollmentRequest request,
            HttpContext context, IAntiforgery antiforgery, UserManager<EnvanterUser> userManager,
            AppDbContext db, CancellationToken ct) =>
        {
            if (!await ValidateCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            var token = request.Ticket?.Trim();
            if (string.IsNullOrEmpty(token) || token.Length > 128) return Results.Unauthorized();
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var challenge = await LoadForUpdateAsync(db, userManager, token, ct);
            if (!IsChallengeUsable(challenge, DateTimeOffset.UtcNow)) return Results.Unauthorized();
            var company = await db.Companies.SingleOrDefaultAsync(item => item.Id == challenge!.CompanyId && item.IsActive && item.SecurityRequireMfa, ct);
            var member = await db.CompanyMemberships.AsNoTracking().AnyAsync(item => item.CompanyId == challenge!.CompanyId && item.UserId == challenge.UserId, ct);
            var user = await userManager.FindByIdAsync(challenge!.UserId.ToString());
            if (company is null || !member || user is null || user.TwoFactorEnabled) return Results.Unauthorized();

            if (challenge.SetupStartedAt is null)
            {
                var reset = await userManager.ResetAuthenticatorKeyAsync(user);
                if (!reset.Succeeded) return Results.Problem("Authenticator anahtarı oluşturulamadı.", statusCode: StatusCodes.Status500InternalServerError);
                challenge.TicketHash = HashEnrollmentTicket(token, user.SecurityStamp);
                challenge.SetupStartedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            var secret = await userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrWhiteSpace(secret)) return Results.Problem("Authenticator anahtarı oluşturulamadı.", statusCode: StatusCodes.Status500InternalServerError);
            await transaction.CommitAsync(ct);
            const string issuer = "EnvanterTR";
            var account = user.Email ?? user.UserName ?? user.Id.ToString("N");
            var label = Uri.EscapeDataString($"{issuer}:{account}");
            var uri = $"otpauth://totp/{label}?secret={Uri.EscapeDataString(secret)}&issuer={Uri.EscapeDataString(issuer)}&digits=6&period=30";
            return Results.Ok(new { secret, authenticatorUri = uri, companyName = company.Name, expiresAt = challenge.ExpiresAt });
        }).AllowAnonymous().RequireRateLimiting("login");

        endpoints.MapPost("/api/session/company-mfa/verify", async ([FromBody] CompanyMfaVerificationRequest request,
            HttpContext context, IAntiforgery antiforgery, UserManager<EnvanterUser> userManager,
            SignInManager<EnvanterUser> signInManager, AppDbContext db, CancellationToken ct) =>
        {
            if (!await ValidateCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            var token = request.Ticket?.Trim();
            var code = request.Code?.Trim().Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
            if (string.IsNullOrEmpty(token) || token.Length > 128 || string.IsNullOrEmpty(code) || code.Length != 6 || code.Any(c => !char.IsAsciiDigit(c)))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["code"] = ["Authenticator uygulamasındaki güncel 6 haneli kodu girin."] });

            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var challenge = await LoadForUpdateAsync(db, userManager, token, ct);
            if (!IsChallengeUsable(challenge, DateTimeOffset.UtcNow) || challenge!.SetupStartedAt is null)
                return Results.Unauthorized();
            var company = await db.Companies.SingleOrDefaultAsync(item => item.Id == challenge.CompanyId && item.IsActive && item.SecurityRequireMfa, ct);
            var membership = await db.CompanyMemberships.AsNoTracking().SingleOrDefaultAsync(item => item.CompanyId == challenge.CompanyId && item.UserId == challenge.UserId, ct);
            var user = await userManager.FindByIdAsync(challenge.UserId.ToString());
            if (company is null || membership is null || user is null || user.TwoFactorEnabled) return Results.Unauthorized();
            if (!await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code))
                return Results.Json(new { message = "Doğrulama kodu geçersiz veya süresi doldu. Uygulamadaki güncel kodu deneyin." }, statusCode: StatusCodes.Status400BadRequest);

            var enabled = await userManager.SetTwoFactorEnabledAsync(user, true);
            if (!enabled.Succeeded) return Results.Problem("İki aşamalı doğrulama açılamadı.", statusCode: StatusCodes.Status500InternalServerError);
            var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            if (recoveryCodes is null) return Results.Problem("Kurtarma kodları oluşturulamadı.", statusCode: StatusCodes.Status500InternalServerError);

            var now = DateTimeOffset.UtcNow;
            var rememberHours = challenge.RememberMe ? 720 : 24;
            var session = new UserSession
            {
                UserId = user.Id, CompanyId = company.Id, PlatformAdmin = false,
                IpAddress = context.Connection.RemoteIpAddress?.ToString() is { Length: > 64 } ip ? ip[^64..] : context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString() is { Length: > 512 } agent ? agent[..512] : context.Request.Headers.UserAgent.ToString(),
                CreatedAt = now, LastSeenAt = now,
                ExpiresAt = now.AddHours(Math.Min(company.SecuritySessionDurationHours, rememberHours))
            };
            db.UserSessions.Add(session);
            db.AuditEntries.Add(new AuditEntry { CompanyId = company.Id, ActorUserId = user.Id,
                TargetType = "Account", TargetId = user.Id, Action = "MfaEnabledByCompanyPolicy",
                Details = "Şirketin zorunlu MFA ilk kurulumunu tamamladı." });
            challenge.CompletedAt = now;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            var claims = new List<Claim> { new(ClaimTypes.Name, user.DisplayName), new("session_id", session.Id.ToString("N")),
                new("company_id", company.Id.ToString()), new("company_role", membership.RoleKey) };
            await signInManager.SignInWithClaimsAsync(user, new AuthenticationProperties
            {
                IsPersistent = challenge.RememberMe,
                ExpiresUtc = session.ExpiresAt,
                AllowRefresh = false
            }, claims);
            return Results.Ok(new { authenticated = true, companyId = company.Id, role = membership.RoleKey,
                platformAdmin = false, name = user.DisplayName, recoveryCodes });
        }).AllowAnonymous().RequireRateLimiting("login");

        return endpoints;
    }

    public static async Task<int> GetMinimumPasswordLengthAsync(AppDbContext db, Guid userId, CancellationToken ct)
    {
        var minimumLength = await db.CompanyMemberships.AsNoTracking().Where(member => member.UserId == userId)
            .Join(db.Companies.AsNoTracking().Where(company => company.IsActive), member => member.CompanyId,
                company => company.Id, (_, company) => (int?)company.SecurityPasswordMinimumLength)
            .MaxAsync(ct);
        return minimumLength ?? 12;
    }

    private static async Task<CompanyMfaEnrollmentChallenge?> LoadForUpdateAsync(AppDbContext db,
        UserManager<EnvanterUser> userManager, string ticket, CancellationToken ct)
    {
        var separator = ticket.IndexOf('.');
        if (separator != 32 || !Guid.TryParseExact(ticket[..separator], "N", out var userId)) return null;
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.EmailConfirmed || await userManager.IsLockedOutAsync(user)) return null;
        // A password reset or other security-stamp change must also invalidate a pending first-login ticket.
        var hash = HashEnrollmentTicket(ticket, user.SecurityStamp);
        return await db.CompanyMfaEnrollmentChallenges
            .FromSqlInterpolated($"SELECT * FROM \"CompanyMfaEnrollmentChallenges\" WHERE \"TicketHash\" = {hash} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
    }

    private static bool IsChallengeUsable(CompanyMfaEnrollmentChallenge? challenge, DateTimeOffset now) =>
        challenge is not null && challenge.CompletedAt is null && challenge.ExpiresAt > now;

    internal static string HashEnrollmentTicket(string ticket, string? securityStamp) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(ticket + "\0" + securityStamp)));
    private static bool TryCompanyId(ClaimsPrincipal user, out Guid companyId) => Guid.TryParse(user.FindFirstValue("company_id"), out companyId);

    private static async Task<bool> ValidateCsrfAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try { await antiforgery.ValidateRequestAsync(context); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }
}
