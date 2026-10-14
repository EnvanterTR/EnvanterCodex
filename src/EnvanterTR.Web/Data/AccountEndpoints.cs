using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AccountEndpoints
{
    private const long AccountPhotoMaxSizeBytes = 2_097_152;
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
            if (user is null) return LoginCredentialsRejected();
            var accountWasLocked = await userManager.IsLockedOutAsync(user);
            var passwordCheck = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!passwordCheck.Succeeded)
            {
                if (!accountWasLocked) await RecordFailedLoginAsync(db, user, cancellationToken);
                return LoginCredentialsRejected();
            }
            if (!user.EmailConfirmed)
                return Results.Json(new { message = "Davet e-postasındaki bağlantıdan parolanızı belirleyerek hesabınızı etkinleştirin." }, statusCode: StatusCodes.Status403Forbidden);

            var tenantSlug = TenantHostResolver.ResolveSlug(context.Request, context.RequestServices
                .GetRequiredService<IConfiguration>()["TenantRouting:RootDomain"]);
            if (tenantSlug is { Length: 0 }) return Results.NotFound();
            Guid? tenantCompanyId = null;
            if (tenantSlug is not null)
            {
                tenantCompanyId = await db.Companies.AsNoTracking()
                    .Where(company => company.IsActive && company.Slug == tenantSlug)
                    .Select(company => (Guid?)company.Id)
                    .SingleOrDefaultAsync(cancellationToken);
                if (tenantCompanyId is null) return Results.NotFound();
                if (request.CompanyId is Guid requestedCompanyId && requestedCompanyId != tenantCompanyId)
                    return Results.Forbid();
            }

            var isPlatformAdmin = await db.PlatformAdministrators.AsNoTracking().AnyAsync(x => x.UserId == user.Id, cancellationToken);
            var memberships = await db.CompanyMemberships.AsNoTracking().Where(x => x.UserId == user.Id &&
                (tenantCompanyId == null || x.CompanyId == tenantCompanyId) &&
                db.Companies.Any(company => company.Id == x.CompanyId && company.IsActive)).ToListAsync(cancellationToken);
            var membership = tenantCompanyId is not null
                ? memberships.SingleOrDefault(x => x.CompanyId == tenantCompanyId)
                : request.CompanyId is Guid selected
                ? memberships.SingleOrDefault(x => x.CompanyId == selected)
                : memberships.Count == 1 ? memberships[0] : null;
            if (request.CompanyId is not null && membership is null)
                return Results.Forbid();
            if (tenantCompanyId is not null && membership is null)
                return Results.Forbid();
            if (tenantCompanyId is null && memberships.Count > 1 && request.CompanyId is null)
            {
                var companyIds = memberships.Select(x => x.CompanyId).ToArray();
                var companies = await db.Companies.AsNoTracking().Where(x => companyIds.Contains(x.Id))
                    .OrderBy(x => x.Name).Select(x => new { id = x.Id, name = x.Name }).ToListAsync(cancellationToken);
                return Results.Conflict(new { message = "Devam etmek için çalışacağınız şirketi seçin.", companies });
            }
            if (membership is null && !isPlatformAdmin)
                return memberships.Count == 0
                    ? Results.Forbid()
                    : Results.ValidationProblem(new Dictionary<string, string[]> { ["companyId"] = ["Birden fazla şirket erişiminiz var; şirket seçin."] });
            if (membership is not null && !await db.Companies.AsNoTracking()
                    .AnyAsync(x => x.Id == membership.CompanyId && x.IsActive, cancellationToken))
                return Results.Forbid();

            if (user.TwoFactorEnabled)
            {
                var code = request.TwoFactorCode?.Trim();
                if (string.IsNullOrWhiteSpace(code))
                    return Results.Conflict(new { twoFactorRequired = true, message = "Authenticator uygulamanızdaki 6 haneli kodu veya tek kullanımlık kurtarma kodunu girin." });
                var verified = code.Length <= 32 && await userManager.VerifyTwoFactorTokenAsync(
                    user, TokenOptions.DefaultAuthenticatorProvider, code.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal));
                if (!verified)
                {
                    var accountWasLockedBeforeCodeCheck = await userManager.IsLockedOutAsync(user);
                    var recovery = await userManager.RedeemTwoFactorRecoveryCodeAsync(user, code);
                    if (!recovery.Succeeded)
                    {
                        await userManager.AccessFailedAsync(user);
                        if (!accountWasLockedBeforeCodeCheck) await RecordFailedLoginAsync(db, user, cancellationToken);
                        return await userManager.IsLockedOutAsync(user)
                            ? Results.Json(new { message = "Çok sayıda hatalı doğrulama kodu. Bir süre sonra tekrar deneyin." }, statusCode: StatusCodes.Status423Locked)
                            : Results.Json(new { twoFactorRequired = true, message = "Doğrulama kodu geçersiz veya kullanılmış. Yeniden deneyin." }, statusCode: StatusCodes.Status400BadRequest);
                    }
                }
                await userManager.ResetAccessFailedCountAsync(user);
            }

            if (membership is not null)
            {
                db.AuditEntries.Add(new AuditEntry { CompanyId = membership.CompanyId, ActorUserId = user.Id,
                    TargetType = "Account", TargetId = user.Id, Action = "LoginSucceeded", Details = "Şirket oturumu açıldı." });
            }
            else if (isPlatformAdmin)
            {
                db.PlatformAuditEntries.Add(new PlatformAuditEntry { ActorUserId = user.Id, Action = "LoginSucceeded",
                    TargetType = "PlatformAccount", TargetId = user.Id, Details = "Platform yönetici oturumu açıldı." });
            }
            var now = DateTimeOffset.UtcNow;
            var sessionRecord = new UserSession
            {
                UserId = user.Id,
                CompanyId = membership?.CompanyId,
                PlatformAdmin = isPlatformAdmin,
                IpAddress = context.Connection.RemoteIpAddress?.ToString() is { Length: > 64 } ip ? ip[^64..] : context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString() is { Length: > 512 } agent ? agent[..512] : context.Request.Headers.UserAgent.ToString(),
                CreatedAt = now,
                LastSeenAt = now,
                ExpiresAt = now.AddDays(request.RememberMe ? 30 : 1)
            };
            db.UserSessions.Add(sessionRecord);
            await db.SaveChangesAsync(cancellationToken);

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.DisplayName),
                new("session_id", sessionRecord.Id.ToString("N"))
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

        endpoints.MapGet("/api/session/2fa", async (ClaimsPrincipal principal, UserManager<EnvanterUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(principal);
            return user is null ? Results.Unauthorized() : Results.Ok(new { enabled = user.TwoFactorEnabled });
        }).RequireAuthorization();

        endpoints.MapPost("/api/session/platform-reauth", async ([FromBody] PlatformReauthenticationRequest request,
            ClaimsPrincipal principal, HttpContext context, IAntiforgery antiforgery,
            UserManager<EnvanterUser> userManager, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery))
                return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            if (!principal.HasClaim("platform_admin", "true") ||
                !Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
                !await db.PlatformAdministrators.AsNoTracking().AnyAsync(admin => admin.UserId == userId, cancellationToken))
                return Results.Forbid();

            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null || !user.TwoFactorEnabled)
                return Results.Json(new { message = "Parola veya yönetici doğrulaması geçersiz." }, statusCode: StatusCodes.Status401Unauthorized);
            if (string.IsNullOrWhiteSpace(request.Password) || !await userManager.CheckPasswordAsync(user, request.Password))
            {
                await userManager.AccessFailedAsync(user);
                return Results.Json(new { message = "Parola veya yönetici doğrulaması geçersiz." }, statusCode: StatusCodes.Status401Unauthorized);
            }

            var code = request.Code?.Trim();
            if (string.IsNullOrWhiteSpace(code) || code.Length > 32)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["code"] = ["Authenticator veya kullanılmamış kurtarma kodu gereklidir."] });
            var normalizedCode = code.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
            var verified = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, normalizedCode);
            if (!verified) verified = (await userManager.RedeemTwoFactorRecoveryCodeAsync(user, code)).Succeeded;
            if (!verified)
            {
                await userManager.AccessFailedAsync(user);
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["code"] = ["Doğrulama kodu geçersiz veya daha önce kullanılmış."] });
            }
            await userManager.ResetAccessFailedCountAsync(user);

            var identity = principal.Identity as ClaimsIdentity;
            if (identity is null) return Results.Unauthorized();
            foreach (var oldClaim in identity.FindAll("platform_reauth_at").ToArray()) identity.RemoveClaim(oldClaim);
            var now = DateTimeOffset.UtcNow;
            identity.AddClaim(new Claim("platform_reauth_at", now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture)));
            db.PlatformAuditEntries.Add(new PlatformAuditEntry
            {
                ActorUserId = userId, Action = "PlatformReauthenticated", TargetType = "PlatformAccount", TargetId = userId,
                Details = "Platform yönetici işlemleri için parola ve ikinci aşama doğrulandı."
            });
            await db.SaveChangesAsync(cancellationToken);
            var authResult = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            await context.SignInAsync(IdentityConstants.ApplicationScheme, principal, authResult.Properties ?? new AuthenticationProperties());
            return Results.Ok(new { reauthenticatedAt = now, validForSeconds = 600 });
        }).RequireAuthorization().RequireRateLimiting("step-up");

        endpoints.MapGet("/api/session/devices", async (ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Unauthorized();
            var now = DateTimeOffset.UtcNow;
            var currentSessionId = Guid.TryParse(principal.FindFirstValue("session_id"), out var parsedSessionId) ? parsedSessionId : (Guid?)null;
            var activeSessions = db.UserSessions.AsNoTracking()
                .Where(session => session.UserId == userId && session.RevokedAt == null && session.ExpiresAt > now);
            var total = await activeSessions.CountAsync(cancellationToken);
            var sessions = await activeSessions
                .OrderByDescending(session => session.LastSeenAt)
                .Take(100)
                .Select(session => new
                {
                    id = session.Id,
                    current = session.Id == currentSessionId,
                    session.CompanyId,
                    session.PlatformAdmin,
                    session.IpAddress,
                    session.UserAgent,
                    session.CreatedAt,
                    session.LastSeenAt,
                    session.ExpiresAt
                }).ToListAsync(cancellationToken);
            return Results.Ok(new { sessions, truncated = total > sessions.Count });
        }).RequireAuthorization();

        endpoints.MapPost("/api/session/devices/{id:guid}/revoke", async (Guid id, ClaimsPrincipal principal,
            HttpContext context, IAntiforgery antiforgery, AppDbContext db, SignInManager<EnvanterUser> signInManager,
            CancellationToken cancellationToken) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Unauthorized();
            var session = await db.UserSessions.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId && item.RevokedAt == null,
                cancellationToken);
            if (session is null) return Results.NotFound();
            var currentSessionId = Guid.TryParse(principal.FindFirstValue("session_id"), out var parsedCurrentId) ? parsedCurrentId : (Guid?)null;
            session.RevokedAt = DateTimeOffset.UtcNow;
            if (Guid.TryParse(principal.FindFirstValue("company_id"), out var companyId))
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId, TargetType = "AccountSession",
                    TargetId = id, Action = "SessionRevoked", Details = id == currentSessionId ? "Mevcut oturum kapatıldı." : "Hesabın oturumu uzaktan kapatıldı." });
            else
                db.PlatformAuditEntries.Add(new PlatformAuditEntry { ActorUserId = userId, TargetType = "AccountSession", TargetId = id,
                    Action = "SessionRevoked", Details = "Hesabın oturumu uzaktan kapatıldı." });
            await db.SaveChangesAsync(cancellationToken);
            if (currentSessionId == id) await signInManager.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        endpoints.MapPost("/api/session/password/change", async ([FromBody] ChangeOwnPasswordRequest request,
            ClaimsPrincipal principal, HttpContext context, IAntiforgery antiforgery,
            UserManager<EnvanterUser> userManager, SignInManager<EnvanterUser> signInManager,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            var user = await userManager.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();
            if (string.IsNullOrEmpty(request.CurrentPassword) || request.CurrentPassword.Length > 256 ||
                string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length > 256 ||
                !string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["password"] = ["Mevcut parola, yeni parola ve eşleşen parola tekrarı gereklidir."] });
            if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["currentPassword"] = ["Mevcut parola doğrulanamadı."] });

            var validationErrors = new List<string>();
            foreach (var validator in userManager.PasswordValidators)
            {
                var validation = await validator.ValidateAsync(userManager, user, request.NewPassword);
                if (!validation.Succeeded) validationErrors.AddRange(validation.Errors.Select(error => error.Description));
            }
            if (validationErrors.Count > 0)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["newPassword"] = validationErrors.Distinct().ToArray() });

            if (user.TwoFactorEnabled)
            {
                var code = request.TwoFactorCode?.Trim();
                if (string.IsNullOrWhiteSpace(code) || code.Length > 32)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["twoFactorCode"] = ["Authenticator veya kullanılmamış kurtarma kodu gereklidir."] });
                var verified = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider,
                    code.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal));
                if (!verified && !(await userManager.RedeemTwoFactorRecoveryCodeAsync(user, code)).Succeeded)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["twoFactorCode"] = ["Doğrulama kodu geçersiz veya daha önce kullanılmış."] });
            }

            var changed = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!changed.Succeeded)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["newPassword"] = changed.Errors.Select(error => error.Description).ToArray() });
            await userManager.UpdateSecurityStampAsync(user);
            if (Guid.TryParse(principal.FindFirstValue("company_id"), out var companyId))
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = user.Id,
                    TargetType = "Account", TargetId = user.Id, Action = "PasswordChanged", Details = "Hesap parolası değiştirildi; diğer oturumlar geçersiz kılındı." });
            else
                db.PlatformAuditEntries.Add(new PlatformAuditEntry { ActorUserId = user.Id,
                    TargetType = "PlatformAccount", TargetId = user.Id, Action = "PasswordChanged", Details = "Platform hesabı parolası değiştirildi." });
            if (Guid.TryParse(principal.FindFirstValue("session_id"), out var currentSessionId))
                await db.UserSessions.Where(session => session.UserId == user.Id && session.Id != currentSessionId && session.RevokedAt == null)
                    .ExecuteUpdateAsync(update => update.SetProperty(session => session.RevokedAt, DateTimeOffset.UtcNow), cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await signInManager.RefreshSignInAsync(user);
            return Results.Ok(new { message = "Parolanız değiştirildi. Diğer cihazlardaki oturumlar en geç 5 dakika içinde kapatılacak." });
        }).RequireAuthorization().RequireRateLimiting("login");

        endpoints.MapPost("/api/session/2fa/setup", async ([FromBody] TwoFactorSetupRequest request, ClaimsPrincipal principal,
            HttpContext context, IAntiforgery antiforgery, UserManager<EnvanterUser> userManager, CancellationToken cancellationToken) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            var user = await userManager.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();
            if (user.TwoFactorEnabled) return Results.Conflict(new { message = "İki aşamalı doğrulama zaten etkin." });
            if (string.IsNullOrEmpty(request.CurrentPassword) || !await userManager.CheckPasswordAsync(user, request.CurrentPassword))
                return Results.Unauthorized();
            var reset = await userManager.ResetAuthenticatorKeyAsync(user);
            if (!reset.Succeeded) return Results.Problem("Authenticator anahtarı oluşturulamadı.", statusCode: StatusCodes.Status500InternalServerError);
            var key = await userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrWhiteSpace(key)) return Results.Problem("Authenticator anahtarı oluşturulamadı.", statusCode: StatusCodes.Status500InternalServerError);
            var issuer = "EnvanterTR";
            var account = user.Email ?? user.UserName ?? user.Id.ToString("N");
            var label = Uri.EscapeDataString($"{issuer}:{account}");
            var uri = $"otpauth://totp/{label}?secret={Uri.EscapeDataString(key)}&issuer={Uri.EscapeDataString(issuer)}&digits=6&period=30";
            return Results.Ok(new { secret = key, authenticatorUri = uri });
        }).RequireAuthorization().RequireRateLimiting("login");

        endpoints.MapPost("/api/session/2fa/enable", async ([FromBody] TwoFactorCodeRequest request, ClaimsPrincipal principal,
            HttpContext context, IAntiforgery antiforgery, UserManager<EnvanterUser> userManager, AppDbContext db) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            var user = await userManager.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();
            if (user.TwoFactorEnabled) return Results.Conflict(new { message = "İki aşamalı doğrulama zaten etkin." });
            var code = request.Code?.Trim().Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
            if (string.IsNullOrWhiteSpace(await userManager.GetAuthenticatorKeyAsync(user)) || string.IsNullOrEmpty(code) ||
                !await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["code"] = ["Authenticator uygulamasındaki güncel 6 haneli kod doğrulanamadı."] });
            AddAccountSecurityAudit(db, principal, user.Id, "TwoFactorEnabled", "İki aşamalı doğrulama etkinleştirildi.");
            var enabled = await userManager.SetTwoFactorEnabledAsync(user, true);
            if (!enabled.Succeeded) return Results.ValidationProblem(new Dictionary<string, string[]> { ["twoFactor"] = enabled.Errors.Select(x => x.Description).ToArray() });
            var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            return recoveryCodes is null
                ? Results.Problem("Kurtarma kodları oluşturulamadı.", statusCode: StatusCodes.Status500InternalServerError)
                : Results.Ok(new { enabled = true, recoveryCodes });
        }).RequireAuthorization().RequireRateLimiting("login");

        endpoints.MapPost("/api/session/2fa/recovery-codes", async ([FromBody] TwoFactorRecoveryRequest request, ClaimsPrincipal principal,
            HttpContext context, IAntiforgery antiforgery, UserManager<EnvanterUser> userManager, AppDbContext db) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            var user = await userManager.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();
            if (!user.TwoFactorEnabled || string.IsNullOrEmpty(request.CurrentPassword) ||
                !await userManager.CheckPasswordAsync(user, request.CurrentPassword)) return Results.Unauthorized();
            var code = request.Code?.Trim().Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
            if (string.IsNullOrEmpty(code) || !await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["code"] = ["Yeni kurtarma kodları üretmek için authenticator kodunu doğrulayın."] });
            AddAccountSecurityAudit(db, principal, user.Id, "TwoFactorRecoveryCodesRotated", "İki aşamalı doğrulama kurtarma kodları yenilendi.");
            var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            return recoveryCodes is null
                ? Results.Problem("Kurtarma kodları oluşturulamadı.", statusCode: StatusCodes.Status500InternalServerError)
                : Results.Ok(new { recoveryCodes });
        }).RequireAuthorization().RequireRateLimiting("login");

        endpoints.MapPost("/api/session/2fa/disable", async ([FromBody] TwoFactorRecoveryRequest request, ClaimsPrincipal principal,
            HttpContext context, IAntiforgery antiforgery, UserManager<EnvanterUser> userManager, AppDbContext db) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            var user = await userManager.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();
            if (!user.TwoFactorEnabled || string.IsNullOrEmpty(request.CurrentPassword) ||
                !await userManager.CheckPasswordAsync(user, request.CurrentPassword)) return Results.Unauthorized();
            var code = request.Code?.Trim();
            var verified = !string.IsNullOrEmpty(code) && await userManager.VerifyTwoFactorTokenAsync(user,
                TokenOptions.DefaultAuthenticatorProvider, code.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal));
            if (!verified && !string.IsNullOrEmpty(code))
                verified = (await userManager.RedeemTwoFactorRecoveryCodeAsync(user, code)).Succeeded;
            if (!verified) return Results.ValidationProblem(new Dictionary<string, string[]> { ["code"] = ["Authenticator veya kullanılmamış kurtarma kodu doğrulanamadı."] });
            AddAccountSecurityAudit(db, principal, user.Id, "TwoFactorDisabled", "İki aşamalı doğrulama kapatıldı.");
            var disabled = await userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disabled.Succeeded) return Results.ValidationProblem(new Dictionary<string, string[]> { ["twoFactor"] = disabled.Errors.Select(x => x.Description).ToArray() });
            await userManager.ResetAuthenticatorKeyAsync(user);
            return Results.Ok(new { enabled = false });
        }).RequireAuthorization().RequireRateLimiting("login");

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
            AppDbContext db,
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
            var companyIds = await db.CompanyMemberships.AsNoTracking().Where(x => x.UserId == user.Id)
                .Select(x => x.CompanyId).Distinct().ToListAsync(cancellationToken);
            foreach (var companyId in companyIds)
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = user.Id,
                    TargetType = "Account", TargetId = user.Id, Action = "PasswordReset",
                    Details = "E-posta doğrulamalı parola yenileme tamamlandı; diğer oturumlar geçersiz kılındı." });
            db.PlatformAuditEntries.Add(new PlatformAuditEntry { ActorUserId = user.Id,
                CompanyId = companyIds.Count == 1 ? companyIds[0] : null, TargetType = "Account", TargetId = user.Id,
                Action = "PasswordReset", Details = "E-posta doğrulamalı parola yenileme tamamlandı." });
            await db.UserSessions.Where(session => session.UserId == user.Id && session.RevokedAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(session => session.RevokedAt, DateTimeOffset.UtcNow), cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
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
            SignInManager<EnvanterUser> signInManager, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            if (Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            {
                if (Guid.TryParse(context.User.FindFirstValue("session_id"), out var sessionId))
                    await db.UserSessions.Where(session => session.Id == sessionId && session.UserId == actorId && session.RevokedAt == null)
                        .ExecuteUpdateAsync(update => update.SetProperty(session => session.RevokedAt, DateTimeOffset.UtcNow), cancellationToken);
                if (Guid.TryParse(context.User.FindFirstValue("company_id"), out var companyId))
                    db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Account",
                        TargetId = actorId, Action = "Logout", Details = "Şirket oturumu kapatıldı." });
                else
                    db.PlatformAuditEntries.Add(new PlatformAuditEntry { ActorUserId = actorId, TargetType = "PlatformAccount",
                        TargetId = actorId, Action = "Logout", Details = "Platform yönetici oturumu kapatıldı." });
                await db.SaveChangesAsync();
            }
            await signInManager.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        endpoints.MapGet("/api/session", async (ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var companyId = Guid.TryParse(user.FindFirstValue("company_id"), out var parsedCompanyId)
                ? parsedCompanyId : (Guid?)null;
            var companyName = companyId is Guid id
                ? await db.Companies.AsNoTracking().Where(x => x.Id == id && x.IsActive)
                    .Select(x => x.Name).SingleOrDefaultAsync(cancellationToken)
                : null;
            var now = DateTimeOffset.UtcNow;
            var subscription = companyId is Guid subscriptionCompanyId
                ? await (from item in db.TenantSubscriptions.AsNoTracking()
                    join plan in db.SubscriptionPlans.AsNoTracking() on item.PlanId equals plan.Id
                    where item.CompanyId == subscriptionCompanyId
                    select new
                    {
                        plan.Name,
                        plan.DeploymentKind,
                        item.Status,
                        item.IsDemo,
                        item.StartsAt,
                        item.EndsAt
                    }).SingleOrDefaultAsync(cancellationToken)
                : null;
            return Results.Ok(new
            {
                userId,
                companyId = companyId?.ToString(),
                companyName,
                subscriptionAccess = subscription is null ? null : new
                {
                    planName = subscription.Name,
                    subscription.DeploymentKind,
                    subscription.Status,
                    subscription.IsDemo,
                    subscription.StartsAt,
                    subscription.EndsAt,
                    mode = TenantSubscriptionAccess.IsReadOnly(subscription.DeploymentKind, subscription.Status,
                        subscription.StartsAt, subscription.EndsAt, now) ? "readOnly" : "active"
                },
                role = user.FindFirstValue("company_role"),
                platformAdmin = user.HasClaim("platform_admin", "true"),
                name = user.Identity?.Name,
                hasPhoto = Guid.TryParse(userId, out var parsedUserId) && new[] { "png", "jpg", "webp" }
                    .Any(extension => System.IO.File.Exists(AccountPhotoPath(configuration, parsedUserId, extension)))
            });
        }).RequireAuthorization();

        endpoints.MapPost("/api/session/photo", async (HttpRequest request, HttpContext context, IAntiforgery antiforgery,
            ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
            if (!request.HasFormContentType) return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
            var form = await request.ReadFormAsync(cancellationToken);
            var upload = form.Files.GetFile("file");
            if (upload is null || upload.Length is < 1 or > AccountPhotoMaxSizeBytes)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Profil fotoğrafı 1 bayt ile 2 MB arasında olmalıdır."] });
            await using var buffer = new MemoryStream((int)upload.Length);
            await upload.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            var detected = DetectProfileImage(bytes);
            if (detected.ContentType is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Yalnızca doğrulanmış PNG, JPEG veya WebP fotoğraf yüklenebilir."] });

            var directory = AccountPhotoDirectory(configuration, userId);
            Directory.CreateDirectory(directory);
            var tempPath = Path.Combine(directory, $"upload-{Guid.NewGuid():N}.tmp");
            var destination = AccountPhotoPath(configuration, userId, detected.Extension);
            var previousPath = new[] { "png", "jpg", "webp" }.Select(extension => AccountPhotoPath(configuration, userId, extension))
                .FirstOrDefault(System.IO.File.Exists);
            var previousBackupPath = previousPath is null ? null : Path.Combine(directory, $"replace-{Guid.NewGuid():N}.tmp");
            try
            {
                await System.IO.File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);
                if (previousPath is not null) System.IO.File.Move(previousPath, previousBackupPath!);
                System.IO.File.Move(tempPath, destination);
                if (Guid.TryParse(user.FindFirstValue("company_id"), out var companyId))
                {
                    db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId,
                        TargetType = "Account", TargetId = userId, Action = "ProfilePhotoUpdated",
                        Details = $"Hesap profil fotoğrafı güncellendi ({detected.ContentType}, {bytes.LongLength} bayt)." });
                    await db.SaveChangesAsync(cancellationToken);
                }
            }
            catch
            {
                try { if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                try { if (System.IO.File.Exists(destination)) System.IO.File.Delete(destination); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                try { if (previousPath is not null && previousBackupPath is not null && System.IO.File.Exists(previousBackupPath)) System.IO.File.Move(previousBackupPath, previousPath, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                throw;
            }
            foreach (var extension in new[] { "png", "jpg", "webp" })
            {
                var oldPath = AccountPhotoPath(configuration, userId, extension);
                try { if (!string.Equals(oldPath, destination, StringComparison.Ordinal) && System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
            try { if (previousBackupPath is not null && System.IO.File.Exists(previousBackupPath)) System.IO.File.Delete(previousBackupPath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            return Results.Ok(new { photoUrl = $"/api/session/photo?v={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" });
        }).RequireAuthorization().WithMetadata(new RequestSizeLimitAttribute(AccountPhotoMaxSizeBytes + 65_536));

        endpoints.MapGet("/api/session/photo", (ClaimsPrincipal user, IConfiguration configuration) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
            foreach (var candidate in new[] { ("png", "image/png"), ("jpg", "image/jpeg"), ("webp", "image/webp") })
            {
                var path = AccountPhotoPath(configuration, userId, candidate.Item1);
                if (!System.IO.File.Exists(path)) continue;
                var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                return Results.File(stream, candidate.Item2, enableRangeProcessing: false);
            }
            return Results.NotFound();
        }).RequireAuthorization();

        endpoints.MapDelete("/api/session/photo", async (HttpContext context, IAntiforgery antiforgery,
            ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
            var directory = AccountPhotoDirectory(configuration, userId);
            var staged = new List<(string Original, string Temporary)>();
            try
            {
                foreach (var extension in new[] { "png", "jpg", "webp" })
                {
                    var path = AccountPhotoPath(configuration, userId, extension);
                    if (!System.IO.File.Exists(path)) continue;
                    var temporary = Path.Combine(directory, $"remove-{Guid.NewGuid():N}.tmp");
                    System.IO.File.Move(path, temporary); staged.Add((path, temporary));
                }
                if (Guid.TryParse(user.FindFirstValue("company_id"), out var companyId))
                {
                    db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId,
                        TargetType = "Account", TargetId = userId, Action = "ProfilePhotoRemoved", Details = "Hesap profil fotoğrafı kaldırıldı." });
                    await db.SaveChangesAsync(cancellationToken);
                }
            }
            catch
            {
                foreach (var item in staged) try { if (System.IO.File.Exists(item.Temporary)) System.IO.File.Move(item.Temporary, item.Original, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                throw;
            }
            foreach (var item in staged) try { if (System.IO.File.Exists(item.Temporary)) System.IO.File.Delete(item.Temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            return Results.NoContent();
        }).RequireAuthorization();

        return endpoints;
    }

    private static IResult LoginCredentialsRejected() =>
        Results.Json(new { message = "Giriş bilgileri geçersiz veya hesap geçici olarak kullanılamıyor." }, statusCode: StatusCodes.Status401Unauthorized);

    private static async Task RecordFailedLoginAsync(AppDbContext db, EnvanterUser user, CancellationToken cancellationToken)
    {
        var companyIds = await db.CompanyMemberships.AsNoTracking().Where(x => x.UserId == user.Id)
            .Select(x => x.CompanyId).Distinct().ToListAsync(cancellationToken);
        foreach (var companyId in companyIds)
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, TargetType = "Account", TargetId = user.Id,
                Action = "LoginFailed", Details = "Parola doğrulaması başarısız oldu." });
        db.PlatformAuditEntries.Add(new PlatformAuditEntry { ActorUserId = user.Id, CompanyId = companyIds.Count == 1 ? companyIds[0] : null,
            Action = "LoginFailed", TargetType = "Account", TargetId = user.Id, Details = "Parola doğrulaması başarısız oldu." });
        await db.SaveChangesAsync(cancellationToken);
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

    private static void AddAccountSecurityAudit(AppDbContext db, ClaimsPrincipal principal, Guid actorId,
        string action, string details)
    {
        if (Guid.TryParse(principal.FindFirstValue("company_id"), out var companyId))
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Account",
                TargetId = actorId, Action = action, Details = details });
        else
            db.PlatformAuditEntries.Add(new PlatformAuditEntry { ActorUserId = actorId, TargetType = "PlatformAccount",
                TargetId = actorId, Action = action, Details = details });
    }

    private static bool TokensMatch(string supplied, string? expected)
    {
        if (string.IsNullOrWhiteSpace(expected)) return false;
        var left = Encoding.UTF8.GetBytes(supplied);
        var right = Encoding.UTF8.GetBytes(expected);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }

    private static string AccountPhotoDirectory(IConfiguration configuration, Guid userId) =>
        Path.Combine(configuration["Storage:CompanyMediaPath"] ?? "/var/lib/envantertr/company-media", "accounts", userId.ToString("N"));

    private static string AccountPhotoPath(IConfiguration configuration, Guid userId, string extension) =>
        Path.Combine(AccountPhotoDirectory(configuration, userId), $"profile.{extension}");

    private static (string? ContentType, string Extension) DetectProfileImage(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ("image/png", "png");
        if (bytes.Length >= 3 && bytes[..3].SequenceEqual(new byte[] { 255, 216, 255 })) return ("image/jpeg", "jpg");
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8)) return ("image/webp", "webp");
        return (null, "");
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

    public static async Task SendCompanyAccessAddedEmailAsync(string recipient, string displayName, string companyName,
        string roleKey, IConfiguration configuration, CancellationToken cancellationToken)
    {
        var host = configuration["Email:Smtp:Host"];
        var publicBaseUrl = configuration["Email:PublicBaseUrl"]?.TrimEnd('/');
        var fromAddress = configuration["Email:FromAddress"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(publicBaseUrl) ||
            string.IsNullOrWhiteSpace(fromAddress))
            throw new InvalidOperationException("Company access email configuration is incomplete.");
        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("https" or "http"))
            throw new InvalidOperationException("Email:PublicBaseUrl must be an absolute HTTP(S) URL.");
        if (baseUri.Scheme != Uri.UriSchemeHttps && !string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Company access links must use HTTPS outside development.");
        if (!int.TryParse(configuration["Email:Smtp:Port"], out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException("Email:Smtp:Port must be a valid TCP port.");

        var roleName = roleKey switch
        {
            "admin" => "Yönetici",
            "asset_manager" => "Varlık yöneticisi",
            "asset_operator" => "Varlık sorumlusu",
            "auditor" => "Denetçi",
            "hr_operator" => "Ekip yöneticisi",
            _ => "Görüntüleyici"
        };
        var loginUrl = WebUtility.HtmlEncode(new Uri(baseUri, "/").AbsoluteUri);
        var from = new MailAddress(fromAddress, configuration["Email:FromName"] ?? "EnvanterTR");
        using var message = new MailMessage(from, new MailAddress(recipient))
        {
            Subject = "EnvanterTR şirket erişimi eklendi",
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true,
            Body = $"<p>Merhaba {WebUtility.HtmlEncode(displayName)},</p><p>{WebUtility.HtmlEncode(companyName)} şirket alanına {roleName} rolüyle erişiminiz eklendi. Mevcut EnvanterTR hesabınızla giriş yapıp bu şirketi seçebilirsiniz.</p><p><a href=\"{loginUrl}\">EnvanterTR girişini aç</a></p>"
        };
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = !string.Equals(configuration["Email:Smtp:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15000
        };
        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username)) client.Credentials = new NetworkCredential(username, password ?? string.Empty);
        await client.SendMailAsync(message, cancellationToken);
    }

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
