using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
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

// The Linux production profile exposes the app only to Caddy on its private
// Compose network. Caddy's rightmost X-Forwarded-For entry is the client IP.
// Keep this opt-in: never trust caller-supplied forwarding headers on a public app port.
var trustProxyForwardedFor = builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled");

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    Directory.CreateDirectory(dataProtectionKeysPath);
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

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
        options.ValidationInterval = TimeSpan.FromMinutes(5));
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.Name = "EnvanterTR.Session";
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
});
app.UseAuthentication();
app.Use(async (context, next) =>
{
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
            where membership.CompanyId == companyId && membership.UserId == userId && company.IsActive
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
    if (companyClaim is null && !isPlatformAdmin)
    {
        await context.SignOutAsync(IdentityConstants.ApplicationScheme);
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
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
    app.MapPlatformEndpoints();
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
