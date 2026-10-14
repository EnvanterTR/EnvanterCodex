using EnvanterTR.Web.Data;
using EnvanterTR.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService();

// Windows Server installs keep the PostgreSQL password in a LocalMachine DPAPI
// blob under ProgramData. Only the service identity and administrators can read it.
var protectedConnectionStringPath = builder.Configuration["WindowsSecrets:ConnectionStringPath"];
if (!string.IsNullOrWhiteSpace(protectedConnectionStringPath))
{
    if (!OperatingSystem.IsWindows())
        throw new InvalidOperationException("Windows DPAPI connection secrets can only be loaded on Windows.");
    if (!File.Exists(protectedConnectionStringPath))
        throw new InvalidOperationException("The protected PostgreSQL connection secret is missing.");

    var protectedConnectionString = File.ReadAllBytes(protectedConnectionStringPath);
    byte[]? clearConnectionString = null;
    try
    {
        clearConnectionString = ProtectedData.Unprotect(protectedConnectionString, null, DataProtectionScope.LocalMachine);
        var clearConnectionStringText = new UTF8Encoding(false, true).GetString(clearConnectionString);
        if (string.IsNullOrWhiteSpace(clearConnectionStringText))
            throw new InvalidOperationException("The protected PostgreSQL connection secret is empty.");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:EnvanterTR"] = clearConnectionStringText
        });
    }
    catch (Exception exception) when (exception is CryptographicException or DecoderFallbackException)
    {
        throw new InvalidOperationException("The protected PostgreSQL connection secret could not be opened on this machine.", exception);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(protectedConnectionString);
        if (clearConnectionString is not null)
            CryptographicOperations.ZeroMemory(clearConnectionString);
    }
}

// Windows Server SMTP credentials are stored separately from appsettings and
// protected with machine-scoped DPAPI by configure-email.ps1.
var protectedSmtpPasswordPath = builder.Configuration["WindowsSecrets:SmtpPasswordPath"];
if (!string.IsNullOrWhiteSpace(protectedSmtpPasswordPath))
{
    if (!OperatingSystem.IsWindows())
        throw new InvalidOperationException("Windows DPAPI SMTP secrets can only be loaded on Windows.");
    if (!File.Exists(protectedSmtpPasswordPath))
        throw new InvalidOperationException("The protected SMTP password secret is missing.");

    var protectedSmtpPassword = File.ReadAllBytes(protectedSmtpPasswordPath);
    byte[]? clearSmtpPassword = null;
    try
    {
        clearSmtpPassword = ProtectedData.Unprotect(protectedSmtpPassword, null, DataProtectionScope.LocalMachine);
        var clearSmtpPasswordText = new UTF8Encoding(false, true).GetString(clearSmtpPassword);
        if (string.IsNullOrWhiteSpace(clearSmtpPasswordText))
            throw new InvalidOperationException("The protected SMTP password secret is empty.");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:Smtp:Password"] = clearSmtpPasswordText
        });
    }
    catch (Exception exception) when (exception is CryptographicException or DecoderFallbackException)
    {
        throw new InvalidOperationException("The protected SMTP password secret could not be opened on this machine.", exception);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(protectedSmtpPassword);
        if (clearSmtpPassword is not null)
            CryptographicOperations.ZeroMemory(clearSmtpPassword);
    }
}

// The Linux deployment enables this only for the web container on its private
// Compose network; the web service has no published host port. Keep proxy trust
// opt-in so forwarded headers are never accepted on a directly public app port.
var trustProxyForwardedFor = builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled");
if (trustProxyForwardedFor)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.ForwardLimit = 1;
    });
}

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    Directory.CreateDirectory(dataProtectionKeysPath);
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}
builder.Services.AddSingleton<AssetCustomFieldProtection>();
builder.Services.Configure<IyzipayOptions>(builder.Configuration.GetSection("Iyzipay"));
builder.Services.AddHttpClient<IyzipayCheckoutClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<IyzipayOptions>>().Value;
    if (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var apiUri) &&
        apiUri.Scheme == Uri.UriSchemeHttps && apiUri.UserInfo.Length == 0 && apiUri.AbsolutePath == "/" &&
        apiUri.Host is "api.iyzipay.com" or "sandbox-api.iyzipay.com")
        client.BaseAddress = apiUri;
    client.Timeout = TimeSpan.FromSeconds(20);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

builder.Services.AddHealthChecks();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (trustProxyForwardedFor)
        {
            var forwarded = context.Request.Headers["X-Forwarded-For"].ToString()
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault();
            if (System.Net.IPAddress.TryParse(forwarded, out var parsedForwardedIp))
                clientIp = parsedForwardedIp.ToString();
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            clientIp,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    options.AddPolicy("step-up", context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (trustProxyForwardedFor)
        {
            var forwarded = context.Request.Headers["X-Forwarded-For"].ToString()
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault();
            if (System.Net.IPAddress.TryParse(forwarded, out var parsedForwardedIp))
                clientIp = parsedForwardedIp.ToString();
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            clientIp,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    options.AddPolicy("customer-api", context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (trustProxyForwardedFor)
        {
            var forwarded = context.Request.Headers["X-Forwarded-For"].ToString()
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (System.Net.IPAddress.TryParse(forwarded, out var parsedForwardedIp)) clientIp = parsedForwardedIp.ToString();
        }
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
        });
    });
    options.AddPolicy("checkout", context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (trustProxyForwardedFor)
        {
            var forwarded = context.Request.Headers["X-Forwarded-For"].ToString()
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (System.Net.IPAddress.TryParse(forwarded, out var parsedForwardedIp)) clientIp = parsedForwardedIp.ToString();
        }
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0, AutoReplenishment = true
        });
    });
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        return ValueTask.CompletedTask;
    };
});

var connectionString = builder.Configuration.GetConnectionString("EnvanterTR");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
    builder.Services.AddScoped<OperationalNotificationEmailService>();
    builder.Services.AddHostedService<OperationalNotificationEmailWorker>();
    builder.Services.AddScoped<SupportTicketEmailService>();
    builder.Services.AddHostedService<SupportTicketEmailWorker>();
    builder.Services.AddIdentity<EnvanterUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    }).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
    builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    {
        options.ValidationInterval = TimeSpan.FromMinutes(5);
        options.OnRefreshingPrincipal = context =>
        {
            if (context.CurrentPrincipal?.Identity is ClaimsIdentity currentIdentity &&
                context.NewPrincipal?.Identity is ClaimsIdentity refreshedIdentity)
            {
                // Identity rebuilds the principal when it refreshes the security stamp.
                // Carry only the claims owned by this application; the request middleware
                // below revalidates the session, company membership, role, and admin status.
                foreach (var claimType in new[] { "session_id", "company_id", "company_role", "platform_admin", "platform_reauth_at" })
                    foreach (var claim in currentIdentity.FindAll(claimType))
                        refreshedIdentity.AddClaim(claim);
            }

            return Task.CompletedTask;
        };
    });
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.Name = "EnvanterTR.Session";
        options.Cookie.Domain = builder.Configuration["TenantRouting:CookieDomain"];
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("asset.read", policy => policy.RequireAuthenticatedUser().RequireClaim("company_role",
            "owner", "admin", "asset_manager", "asset_operator", "auditor", "viewer"));
        options.AddPolicy("asset.write", policy => policy.RequireAuthenticatedUser().RequireClaim("company_role",
            "owner", "admin", "asset_manager", "asset_operator"));
        options.AddPolicy("settings.manage", policy => policy.RequireAuthenticatedUser().RequireClaim("company_role",
            "owner", "admin"));
        options.AddPolicy("people.read", policy => policy.RequireAuthenticatedUser().RequireClaim("company_role",
            "owner", "admin", "asset_manager", "asset_operator", "auditor", "hr_operator"));
        options.AddPolicy("people.manage", policy => policy.RequireAuthenticatedUser().RequireClaim("company_role",
            "owner", "admin", "asset_manager", "hr_operator"));
        options.AddPolicy("people.files.read", policy => policy.RequireAuthenticatedUser().RequireClaim("company_role",
            "owner", "admin", "hr_operator"));
        options.AddPolicy("people.files.manage", policy => policy.RequireAuthenticatedUser().RequireClaim("company_role",
            "owner", "admin", "hr_operator"));
        options.AddPolicy("company.member", policy => policy.RequireAuthenticatedUser().RequireClaim("company_role",
            "owner", "admin", "asset_manager", "asset_operator", "auditor", "viewer", "hr_operator"));
        options.AddPolicy("platform.admin", policy => policy.RequireAuthenticatedUser().RequireClaim("platform_admin", "true"));
    });
}

var app = builder.Build();
if (args.Contains("--recover-local-platform-admin", StringComparer.OrdinalIgnoreCase))
{
    Environment.ExitCode = await LocalPlatformAdminRecovery.RunAsync(app.Services, app.Environment.IsDevelopment(), connectionString);
    return;
}

if (trustProxyForwardedFor)
{
    app.UseForwardedHeaders();
}

// Apply browser protections to both static pages and API responses. API responses
// can contain private records or set session cookies, so the browser must not cache them.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(self), microphone=(), geolocation=()";

    if (context.Request.Path.StartsWithSegments("/api"))
    {
        headers.CacheControl = "no-store, private";
    }

    await next();
});

// Older/manual login links may contain credentials in the query string. Strip them
// before serving any page so the browser replaces the address and does not keep
// the secret in its current history entry or expose it through a later referrer.
var sensitiveQueryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "email", "password", "pass", "passwd", "twoFactorCode", "companyId", "token", "access_token", "refresh_token", "code"
};
app.Use(async (context, next) =>
{
    if (HttpMethods.IsGet(context.Request.Method) && context.Request.Query.Keys.Any(sensitiveQueryKeys.Contains))
    {
        var safeQuery = context.Request.Query.Where(parameter => !sensitiveQueryKeys.Contains(parameter.Key)).ToArray();
        context.Response.StatusCode = StatusCodes.Status303SeeOther;
        context.Response.Headers.Location = context.Request.PathBase + context.Request.Path + QueryString.Create(safeQuery);
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        return;
    }

    await next();
});

// Schema changes are deliberately opt-in so a normal app start never mutates customer data.
if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.Error.WriteLine("Migration durduruldu: ConnectionStrings:EnvanterTR ayarlanmamış.");
        Environment.ExitCode = 2;
        return;
    }

    await using var migrationScope = app.Services.CreateAsyncScope();
    var migrationDb = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
    var pendingMigrations = (await migrationDb.Database.GetPendingMigrationsAsync()).ToArray();
    if (pendingMigrations.Length == 0)
    {
        Console.WriteLine("Veritabanı şeması güncel; uygulanacak migration yok.");
        return;
    }

    Console.WriteLine($"{pendingMigrations.Length} veritabanı migration'ı uygulanıyor.");
    await migrationDb.Database.MigrateAsync();
    Console.WriteLine("Veritabanı migration işlemi tamamlandı.");
    return;
}

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (context.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            context.Context.Response.Headers.Pragma = "no-cache";
            context.Context.Response.Headers.Expires = "0";
        }
    }
});
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception exception) when (HasSerializationFailure(exception) && !context.Response.HasStarted)
    {
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(new { message = "Kayıt eşzamanlı olarak değiştirildi; güncel durumu yenileyip yeniden deneyin." });
    }
    catch (Exception exception) when (AssetPlanQuota.IsExceeded(exception) && !context.Response.HasStarted)
    {
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
        await context.Response.WriteAsJsonAsync(new
        {
            code = AssetPlanQuota.ErrorCode,
            message = "Bu işlem planınızdaki varlık sınırını aşar. Planınızı yükseltin veya mevcut varlık kayıtlarını arşivleyin."
        });
    }
});
app.UseAuthentication();
app.Use(async (context, next) =>
{
    var tenantSlug = TenantHostResolver.ResolveSlug(context.Request,
        context.RequestServices.GetRequiredService<IConfiguration>()["TenantRouting:RootDomain"]);
    if (tenantSlug is { Length: 0 })
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    if (tenantSlug is not null && context.Request.Path.StartsWithSegments("/api/platform"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var principal = context.User;
    if (principal.Identity?.IsAuthenticated != true)
    {
        await next();
        return;
    }

    var db = context.RequestServices.GetRequiredService<AppDbContext>();
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
    {
        await context.SignOutAsync(IdentityConstants.ApplicationScheme);
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    if (string.Equals(principal.Identity?.AuthenticationType, IdentityConstants.ApplicationScheme, StringComparison.Ordinal))
    {
        var currentTime = DateTimeOffset.UtcNow;
        if (!Guid.TryParse(principal.FindFirstValue("session_id"), out var sessionId) ||
            !await db.UserSessions.AsNoTracking().AnyAsync(session => session.Id == sessionId && session.UserId == userId &&
                session.RevokedAt == null && session.ExpiresAt > currentTime, context.RequestAborted))
        {
            await context.SignOutAsync(IdentityConstants.ApplicationScheme);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        await db.UserSessions.Where(session => session.Id == sessionId && session.UserId == userId && session.RevokedAt == null &&
                session.LastSeenAt < currentTime.AddMinutes(-5))
            .ExecuteUpdateAsync(update => update.SetProperty(session => session.LastSeenAt, currentTime), context.RequestAborted);
    }

    var companyClaim = principal.FindFirst("company_id")?.Value;
    var roleClaim = principal.FindFirst("company_role")?.Value;
    string? currentRole = null;
    if (companyClaim is not null)
    {
        if (!Guid.TryParse(companyClaim, out var companyId) || string.IsNullOrWhiteSpace(roleClaim))
        {
            await context.SignOutAsync(IdentityConstants.ApplicationScheme);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        currentRole = await (from membership in db.CompanyMemberships.AsNoTracking()
            join company in db.Companies.AsNoTracking() on membership.CompanyId equals company.Id
            where membership.CompanyId == companyId && membership.UserId == userId && company.IsActive &&
                (tenantSlug == null || company.Slug == tenantSlug)
            select membership.RoleKey).SingleOrDefaultAsync(context.RequestAborted);
        if (currentRole is null)
        {
            await context.SignOutAsync(IdentityConstants.ApplicationScheme);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
    }
    else if (roleClaim is not null)
    {
        await context.SignOutAsync(IdentityConstants.ApplicationScheme);
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    var platformAdminClaim = principal.HasClaim("platform_admin", "true");
    var isPlatformAdmin = platformAdminClaim && await db.PlatformAdministrators.AsNoTracking()
        .AnyAsync(x => x.UserId == userId, context.RequestAborted);
    if (isPlatformAdmin && context.Request.Path.StartsWithSegments("/api/platform"))
    {
        var platformMfaEnabled = await db.Users.AsNoTracking().Where(x => x.Id == userId)
            .Select(x => (bool?)x.TwoFactorEnabled).SingleOrDefaultAsync(context.RequestAborted) == true;
        if (!platformMfaEnabled)
        {
            context.Response.StatusCode = StatusCodes.Status428PreconditionRequired;
            await context.Response.WriteAsJsonAsync(new
            {
                code = "platform_mfa_required",
                message = "Platform yönetimine erişmek için Authenticator tabanlı iki aşamalı doğrulamayı kurun."
            }, context.RequestAborted);
            return;
        }
    }
    if (tenantSlug is not null && companyClaim is null)
    {
        await context.SignOutAsync(IdentityConstants.ApplicationScheme);
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }
    if (companyClaim is null && !isPlatformAdmin)
    {
        await context.SignOutAsync(IdentityConstants.ApplicationScheme);
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    var requestPath = context.Request.Path;
    var isWriteRequest = HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) ||
        HttpMethods.IsPatch(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method);
    var isAccountEndpoint = requestPath.StartsWithSegments("/api/session");
    var isPlatformEndpoint = requestPath.StartsWithSegments("/api/platform");
    if (isPlatformAdmin && isPlatformEndpoint && isWriteRequest)
    {
        var reauthenticatedAtClaim = principal.FindFirstValue("platform_reauth_at");
        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var recentlyReauthenticated = long.TryParse(reauthenticatedAtClaim, out var reauthenticatedAtUnix) &&
            reauthenticatedAtUnix <= nowUnix + 60 && reauthenticatedAtUnix >= nowUnix - 600;
        if (!recentlyReauthenticated)
        {
            context.Response.StatusCode = StatusCodes.Status428PreconditionRequired;
            await context.Response.WriteAsJsonAsync(new
            {
                code = "platform_reauth_required",
                message = "Platform yönetimindeki değişiklikler için son 10 dakika içinde parolanızı ve Authenticator kodunuzu doğrulayın."
            }, context.RequestAborted);
            return;
        }
    }
    if (companyClaim is not null && isWriteRequest && requestPath.StartsWithSegments("/api") &&
        !isAccountEndpoint && !isPlatformEndpoint && Guid.TryParse(companyClaim, out var subscriptionCompanyId))
    {
        var subscription = await (from item in db.TenantSubscriptions.AsNoTracking()
            join plan in db.SubscriptionPlans.AsNoTracking() on item.PlanId equals plan.Id
            where item.CompanyId == subscriptionCompanyId
            orderby item.Status == "Active" || item.Status == "Trial" ? 0 : 1, item.StartsAt descending
            select new { plan.DeploymentKind, item.Status, item.StartsAt, item.EndsAt })
            .FirstOrDefaultAsync(context.RequestAborted);
        if (subscription is not null && TenantSubscriptionAccess.IsReadOnly(subscription.DeploymentKind,
                subscription.Status, subscription.StartsAt, subscription.EndsAt, DateTimeOffset.UtcNow))
        {
            context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            await context.Response.WriteAsJsonAsync(new
            {
                code = "subscription_read_only",
                message = "Şirket aboneliğinin süresi dolmuş veya duraklatılmış. Kayıtlarınız korunur ve görüntülenebilir; yazma işlemleri için platform yöneticinizden abonelik uzatması isteyin."
            }, context.RequestAborted);
            return;
        }
    }

    if (principal.Identity is ClaimsIdentity identity)
    {
        var changed = false;
        if (currentRole is not null && !string.Equals(currentRole, roleClaim, StringComparison.Ordinal))
        {
            foreach (var staleRole in identity.FindAll("company_role").ToArray())
                identity.RemoveClaim(staleRole);
            identity.AddClaim(new Claim("company_role", currentRole));
            changed = true;
        }
        if (platformAdminClaim && !isPlatformAdmin)
        {
            foreach (var staleAdminClaim in identity.FindAll("platform_admin").ToArray())
                identity.RemoveClaim(staleAdminClaim);
            changed = true;
        }
        if (changed)
            await context.SignInAsync(IdentityConstants.ApplicationScheme, principal);
    }

    await next();
});
app.UseAuthorization();
app.UseRateLimiter();
app.MapHealthChecks("/health");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    app.MapGet("/api/status", async (AppDbContext db, ILogger<Program> logger, CancellationToken cancellationToken) =>
    {
        try
        {
            if (!await db.Database.CanConnectAsync(cancellationToken))
                return Results.Ok(new { product = "EnvanterTR", status = "database-unavailable", message = "Veritabanı sunucusuna bağlanılamadı. Servis ve erişim ayarlarını kontrol edin." });

            var allMigrations = db.Database.GetMigrations().ToArray();
            var pendingMigrations = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
            var status = allMigrations.Length == 0 ? "migrations-not-created" :
                pendingMigrations.Length > 0 ? "migration-required" : "ready";
            var message = status switch
            {
                "migrations-not-created" => "Veritabanı erişilebilir ancak uygulama şeması için migration henüz oluşturulmamış.",
                "migration-required" => "Uygulama şeması güncel değil. Önce yedek alıp kontrollü güncelleme uygulayın.",
                _ => "Veritabanı erişilebilir ve uygulama şeması güncel görünüyor."
            };
            return Results.Ok(new { product = "EnvanterTR", status, message, migrationCount = allMigrations.Length, pendingMigrationCount = pendingMigrations.Length });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError("Database readiness check failed. Error type: {ErrorType}", exception.GetType().Name);
            return Results.Ok(new { product = "EnvanterTR", status = "database-unavailable", message = "Veritabanı kontrolü tamamlanamadı. Sunucu günlüklerini inceleyin." });
        }
    }).AllowAnonymous();

    app.MapAccountEndpoints();
    app.MapAssetEndpoints();
    app.MapDataExportEndpoints();
    app.MapOperationalEndpoints();
    app.MapCompanyTaskEndpoints();
    app.MapSupportEndpoints();
    app.MapPlatformEndpoints();
    app.MapCheckoutEndpoints();
    app.MapCustomerApiEndpoints();
}
else
{
    app.MapGet("/api/status", () => Results.Ok(new
    {
        product = "EnvanterTR", status = "database-not-configured",
        message = "Veritabanı bağlantısı kurulmamış. Hassas bağlantı bilgilerini ortam değişkeninden yapılandırın."
    })).AllowAnonymous();
    app.MapGet("/api/setup/status", () => Results.Problem(
        "PostgreSQL bağlantısı yapılandırılmamış.", statusCode: StatusCodes.Status503ServiceUnavailable)).AllowAnonymous();
}

app.Run();

static bool HasSerializationFailure(Exception exception)
{
    for (Exception? current = exception; current is not null; current = current.InnerException)
        if (current is Npgsql.PostgresException { SqlState: "40001" }) return true;
    return false;
}

public partial class Program { }
