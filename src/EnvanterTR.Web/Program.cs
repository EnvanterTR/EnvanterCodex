using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService();

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
builder.Services.AddRateLimiter(options => options.AddPolicy("login", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            AutoReplenishment = true
        })));

var connectionString = builder.Configuration.GetConnectionString("EnvanterTR");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
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
app.UseStaticFiles();
app.UseAuthentication();
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
