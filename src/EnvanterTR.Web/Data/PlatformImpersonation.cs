using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnvanterTR.Web.Data;

public sealed record StartPlatformImpersonation(Guid UserId, string Reason);
public sealed record PlatformImpersonationInfo(string ActorName, string AsUserName, DateTimeOffset ExpiresAt);

public static class PlatformImpersonation
{
    public const string ReturnCookieName = "EnvanterTR.ImpersonationReturn";
    public const string StopPath = "/api/session/impersonation/stop";
    private const string SessionItem = "EnvanterTR.ImpersonationSession";
    private const string InfoItem = "EnvanterTR.ImpersonationInfo";

    public static PlatformImpersonationInfo? GetInfo(HttpContext context) => context.Items[InfoItem] as PlatformImpersonationInfo;
    public static bool IsImpersonating(HttpContext context) => context.Items[SessionItem] is UserSession;
    public static bool IsExitRequest(HttpContext context) => context.Request.Path == StopPath ||
        context.Request.Path == "/api/session/logout" && IsImpersonating(context);

    public static void MapEndpoints(RouteGroupBuilder admin, IEndpointRouteBuilder endpoints)
    {
        admin.MapPost("/companies/{companyId:guid}/impersonations", StartAsync)
            .WithMetadata(new RequestSizeLimitAttribute(8192));
        endpoints.MapPost(StopPath, StopAsync).RequireAuthorization().RequireRateLimiting("customer-api")
            .WithMetadata(new RequestSizeLimitAttribute(1024));
    }

    private static async Task<IResult> StartAsync(Guid companyId, StartPlatformImpersonation request,
        HttpContext context, AppDbContext db, UserManager<EnvanterUser> users, SignInManager<EnvanterUser> signIn,
        IAntiforgery antiforgery, IConfiguration configuration, IWebHostEnvironment environment, CancellationToken ct)
    {
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
        var auth = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!auth.Succeeded || !auth.Principal!.HasClaim("platform_admin", "true") ||
            !Guid.TryParse(auth.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ||
            !Guid.TryParse(auth.Principal.FindFirstValue("session_id"), out var actorSessionId)) return Results.Unauthorized();
        var now = DateTimeOffset.UtcNow;
        if (!long.TryParse(auth.Principal.FindFirstValue("platform_reauth_at"), out var verifiedAt) ||
            verifiedAt < now.ToUnixTimeSeconds() - 600 || verifiedAt > now.ToUnixTimeSeconds() + 60)
            return Results.Json(new { code = "platform_reauth_required", message = "Güncel parola ve MFA doğrulaması gerekli." }, statusCode: 428);
        var reason = request.Reason?.Trim();
        if (reason is null || reason.Length is < 8 or > 500 || request.UserId == Guid.Empty)
            return Results.BadRequest(new { message = "Hesap seçin ve 8–500 karakterlik destek gerekçesi girin." });
        var original = await db.UserSessions.AsNoTracking().SingleOrDefaultAsync(session => session.Id == actorSessionId &&
            session.UserId == actorId && session.PlatformAdmin && session.CompanyId == null && session.ImpersonatorUserId == null &&
            session.RevokedAt == null && session.ExpiresAt > now, ct);
        var actor = await users.FindByIdAsync(actorId.ToString());
        if (original is null || actor?.EmailConfirmed != true || !actor.TwoFactorEnabled || actor.LockoutEnabled && actor.LockoutEnd > now || string.IsNullOrEmpty(actor.SecurityStamp) ||
            !await db.PlatformAdministrators.AnyAsync(admin => admin.UserId == actorId, ct)) return Results.Forbid();
        var root = RootDomain(configuration);
        if (root is null) return Results.Problem("Platform alan adı yapılandırması geçersiz.", statusCode: 503);
        var cookieDomain = configuration["TenantRouting:CookieDomain"];
        if (!environment.IsDevelopment() && (string.IsNullOrWhiteSpace(cookieDomain) || cookieDomain != cookieDomain.Trim() ||
            !string.Equals(cookieDomain.TrimStart('.'), root, StringComparison.OrdinalIgnoreCase)))
            return Results.Problem("Şirket alanları arası oturum çerezi yapılandırılmamış.", statusCode: 503);
        var company = await db.Companies.AsNoTracking().SingleOrDefaultAsync(company => company.Id == companyId && company.IsActive, ct);
        var membership = await db.CompanyMemberships.AsNoTracking().SingleOrDefaultAsync(member => member.CompanyId == companyId && member.UserId == request.UserId, ct);
        var target = await users.FindByIdAsync(request.UserId.ToString());
        if (company is null || !TenantHostResolver.IsValidSlug(company.Slug) || membership is null ||
            target?.EmailConfirmed != true || await users.IsLockedOutAsync(target)) return Results.NotFound();
        var portalUrl = TenantHostResolver.BuildPortalUrl(company.Slug, root);
        if (!(await CompanyRoleAuthorization.ResolveAsync(db, companyId, membership.RoleKey, ct)).Valid) return Results.Forbid();
        await db.UserSessions.Where(existing => existing.ImpersonatorSessionId == original.Id &&
            existing.ImpersonationEndedAt == null && (existing.ExpiresAt <= now || existing.RevokedAt != null))
            .ExecuteUpdateAsync(update => update.SetProperty(existing => existing.ImpersonationEndedAt, now)
                .SetProperty(existing => existing.ImpersonationReturnTokenHash, (string?)null), ct);
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var expires = now.AddMinutes(15) < original.ExpiresAt ? now.AddMinutes(15) : original.ExpiresAt;
        var session = new UserSession
        {
            UserId = target.Id, CompanyId = companyId, PlatformAdmin = false,
            CreatedAt = now, LastSeenAt = now, ExpiresAt = expires,
            IpAddress = Limit(context.Connection.RemoteIpAddress?.ToString(), 64),
            UserAgent = Limit(context.Request.Headers.UserAgent.ToString(), 512),
            ImpersonatorUserId = actorId, ImpersonatorSessionId = original.Id,
            ImpersonatorSecurityStampHash = Hash(actor.SecurityStamp), ImpersonationReturnTokenHash = Hash(token),
            ImpersonatorRememberMe = auth.Properties?.IsPersistent == true, ImpersonationReason = reason
        };
        db.UserSessions.Add(session);
        db.PlatformAuditEntries.Add(new PlatformAuditEntry { ActorUserId = actorId, CompanyId = companyId,
            Action = "ImpersonationStarted", TargetType = "Impersonation", TargetId = session.Id,
            Details = JsonSerializer.Serialize(new { asUserId = target.Id, role = membership.RoleKey, reason, expiresAt = expires }) });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Results.Conflict(new { message = "Bu yönetici oturumunda başka bir destek oturumu başlatıldı. Önce mevcut oturumdan dönün." }); }
        await SignInAsync(context, signIn, target, session, membership.RoleKey, false);
        context.Response.Cookies.Append(ReturnCookieName, token, CookieOptions(context, configuration, expires));
        return Results.Ok(new { portalUrl, companyId,
            userId = target.Id, expiresAt = expires });
    }

    public static async Task<IResult> StopAsync(HttpContext context, AppDbContext db, SignInManager<EnvanterUser> signIn,
        IAntiforgery antiforgery, IConfiguration configuration, CancellationToken ct)
    {
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
        var auth = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!auth.Succeeded || !Guid.TryParse(auth.Principal!.FindFirstValue("session_id"), out var sessionId) ||
            !Guid.TryParse(auth.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Unauthorized();
        var root = RootDomain(configuration);
        if (root is null) return Results.Problem("Platform alan adı yapılandırması geçersiz.", statusCode: 503);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var session = await db.UserSessions.FromSqlInterpolated($"SELECT * FROM \"UserSessions\" WHERE \"Id\" = {sessionId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
        if (session is null || session.UserId != userId || session.ImpersonatorUserId is null ||
            session.ImpersonationEndedAt is not null || session.ExpiresAt <= DateTimeOffset.UtcNow || !MatchesReturnCookie(context, session))
            return Results.Forbid();
        var authority = await ResolveAuthorityAsync(db, session, ct);
        if (authority is null) return Results.Unauthorized();
        var now = DateTimeOffset.UtcNow;
        session.RevokedAt ??= now; session.ImpersonationEndedAt = now; session.ImpersonationReturnTokenHash = null;
        db.PlatformAuditEntries.Add(new PlatformAuditEntry { ActorUserId = authority.Value.Actor.Id, CompanyId = session.CompanyId,
            Action = "ImpersonationEnded", TargetType = "Impersonation", TargetId = session.Id,
            Details = JsonSerializer.Serialize(new { asUserId = session.UserId }) });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        await SignInAsync(context, signIn, authority.Value.Actor, authority.Value.Original, null, session.ImpersonatorRememberMe == true);
        context.Response.Cookies.Delete(ReturnCookieName, CookieOptions(context, configuration, null));
        return Results.Ok(new { platformUrl = $"https://{root}/platform-admin.html" });
    }

    private static async Task SignInAsync(HttpContext context, SignInManager<EnvanterUser> signIn,
        EnvanterUser user, UserSession session, string? role, bool persistent)
    {
        var principal = await signIn.CreateUserPrincipalAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;
        foreach (var claim in identity.Claims.Where(claim => claim.Type is "platform_admin" or "platform_reauth_at" or
            "company_id" or "company_role" or "company_member" or "company_permission" or "session_id" or ClaimTypes.Name).ToArray())
            identity.RemoveClaim(claim);
        identity.AddClaim(new(ClaimTypes.Name, user.DisplayName));
        identity.AddClaim(new("session_id", session.Id.ToString("N")));
        if (role is not null)
        {
            identity.AddClaim(new("company_id", session.CompanyId!.Value.ToString()));
            identity.AddClaim(new("company_role", role));
        }
        else identity.AddClaim(new("platform_admin", "true"));
        await context.SignInAsync(IdentityConstants.ApplicationScheme, principal,
            new AuthenticationProperties { IsPersistent = persistent, ExpiresUtc = session.ExpiresAt, AllowRefresh = false });
    }

    private static async Task<(EnvanterUser Actor, UserSession Original)?> ResolveAuthorityAsync(AppDbContext db, UserSession session, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var actor = await db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == session.ImpersonatorUserId, ct);
        var original = await db.UserSessions.AsNoTracking().SingleOrDefaultAsync(original => original.Id == session.ImpersonatorSessionId &&
            original.UserId == session.ImpersonatorUserId && original.PlatformAdmin && original.CompanyId == null &&
            original.ImpersonatorUserId == null && original.RevokedAt == null && original.ExpiresAt > now, ct);
        if (actor?.EmailConfirmed != true || !actor.TwoFactorEnabled || string.IsNullOrEmpty(actor.SecurityStamp) || original is null ||
            !string.Equals(Hash(actor.SecurityStamp), session.ImpersonatorSecurityStampHash, StringComparison.Ordinal) ||
            !await db.PlatformAdministrators.AsNoTracking().AnyAsync(admin => admin.UserId == actor.Id, ct)) return null;
        return (actor, original);
    }

    private static CookieOptions CookieOptions(HttpContext context, IConfiguration configuration, DateTimeOffset? expires) => new()
    {
        HttpOnly = true, Secure = !context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment() || context.Request.IsHttps,
        SameSite = SameSiteMode.Strict, Path = "/", Domain = configuration["TenantRouting:CookieDomain"], Expires = expires, IsEssential = true
    };
    private static string? RootDomain(IConfiguration configuration)
    {
        var root = (configuration["TenantRouting:RootDomain"] ?? "envantertr.com").Trim().Trim('.').ToLowerInvariant();
        return root.Length > 0 && !root.Contains('/') && !root.Contains(':') &&
            Uri.TryCreate($"https://{root}/", UriKind.Absolute, out var uri) && uri.Host == root && uri.UserInfo.Length == 0 ? root : null;
    }
    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static bool MatchesReturnCookie(HttpContext context, UserSession session) =>
        context.Request.Cookies.TryGetValue(ReturnCookieName, out var token) && token.Length == 43 &&
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Hash(token)), Encoding.UTF8.GetBytes(session.ImpersonationReturnTokenHash ?? ""));
    private static string? Limit(string? value, int max) => value is { Length: > 0 } ? value[..Math.Min(value.Length, max)] : null;

    public static IApplicationBuilder UsePlatformImpersonation(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        if (!context.Request.Path.StartsWithSegments("/api") ||
            context.User.Identity?.AuthenticationType != IdentityConstants.ApplicationScheme ||
            !Guid.TryParse(context.User.FindFirstValue("session_id"), out var sessionId) ||
            !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) { await next(); return; }
        var db = context.RequestServices.GetRequiredService<AppDbContext>();
        var session = await db.UserSessions.AsNoTracking().SingleOrDefaultAsync(session => session.Id == sessionId && session.UserId == userId, context.RequestAborted);
        if (session?.ImpersonatorUserId is null) { await next(); return; }
        context.Items[SessionItem] = session;
        if (IsExitRequest(context)) { await next(); return; }
        var authority = await ResolveAuthorityAsync(db, session, context.RequestAborted);
        if (session.RevokedAt is not null || session.ExpiresAt <= DateTimeOffset.UtcNow || session.ImpersonationEndedAt is not null ||
            authority is null || !MatchesReturnCookie(context, session) ||
            !Guid.TryParse(context.User.FindFirstValue("company_id"), out var companyId) || companyId != session.CompanyId ||
            context.User.HasClaim("platform_admin", "true"))
        {
            await context.SignOutAsync(IdentityConstants.ApplicationScheme);
            context.Response.StatusCode = 401;
            return;
        }
        var targetName = await db.Users.AsNoTracking().Where(user => user.Id == session.UserId).Select(user => user.DisplayName).SingleAsync(context.RequestAborted);
        context.Items[InfoItem] = new PlatformImpersonationInfo(authority.Value.Actor.DisplayName, targetName, session.ExpiresAt);
        var scopeFactory = context.RequestServices.GetRequiredService<IServiceScopeFactory>();
        await using var auditScope = scopeFactory.CreateAsyncScope();
        var auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
        string Details(string state, int? status) => JsonSerializer.Serialize(new { asUserId = session.UserId,
            method = Limit(context.Request.Method, 16), path = Limit(context.Request.Path.Value, 400), state, status });
        var audit = new PlatformAuditEntry { ActorUserId = session.ImpersonatorUserId.Value, CompanyId = session.CompanyId,
            Action = "ImpersonationRequest", TargetType = "Impersonation", TargetId = session.Id, Details = Details("started", null) };
        auditDb.PlatformAuditEntries.Add(audit);
        await auditDb.SaveChangesAsync(context.RequestAborted);
        var failed = false;
        try { await next(); }
        catch { failed = true; throw; }
        finally
        {
            try
            {
                var details = Details(failed ? "failed" : "completed", failed ? 500 : context.Response.StatusCode);
                await auditDb.PlatformAuditEntries.Where(entry => entry.Id == audit.Id)
                    .ExecuteUpdateAsync(update => update.SetProperty(entry => entry.Details, details), CancellationToken.None);
            }
            catch (Exception error) { context.RequestServices.GetRequiredService<ILogger<Program>>().LogError(error, "Impersonation audit outcome update failed. Audit {AuditId}", audit.Id); }
        }
    });
}
