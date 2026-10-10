using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class CustomerApiEndpoints
{
    private static readonly string[] KnownScopes = ["assets:read", "accessories:read", "people:read"];
    private static readonly HashSet<string> AssetReaders = new(StringComparer.Ordinal)
        { "owner", "admin", "asset_manager", "asset_operator", "auditor", "viewer" };
    private static readonly HashSet<string> PeopleReaders = new(StringComparer.Ordinal)
        { "owner", "admin", "asset_manager", "asset_operator", "auditor", "hr_operator" };

    public static void MapCustomerApiEndpoints(this WebApplication app)
    {
        app.MapGet("/api/integrations/tokens", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentityScope(user, out var companyId, out var userId)) return Results.Forbid();
            var tokens = await db.CompanyApiTokens.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.Name, x.TokenPrefix, x.ScopesJson, x.CreatedAt, x.ExpiresAt, x.LastUsedAt, x.RevokedAt })
                .Take(50).ToListAsync(ct);
            return Results.Ok(tokens.Select(x => new
            {
                x.Id, x.Name, x.TokenPrefix, Scopes = JsonSerializer.Deserialize<string[]>(x.ScopesJson) ?? [],
                x.CreatedAt, x.ExpiresAt, x.LastUsedAt, x.RevokedAt,
                IsExpired = x.ExpiresAt is DateTimeOffset expiry && expiry <= DateTimeOffset.UtcNow
            }));
        }).RequireAuthorization("company.member");

        app.MapPost("/api/integrations/tokens", async (CreateTokenRequest request, ClaimsPrincipal user, HttpContext context,
            AppDbContext db, IAntiforgery antiforgery, CancellationToken ct) =>
        {
            if (!TryIdentityScope(user, out var companyId, out var userId)) return Results.Forbid();
            if (!await ValidateCsrfAsync(context, antiforgery)) return Results.StatusCode(StatusCodes.Status400BadRequest);
            var name = (request.Name ?? string.Empty).Trim();
            if (name.Length is < 2 or > 100) return Validation("name", "Ad 2 ile 100 karakter arasında olmalıdır.");
            var scopes = (request.Scopes ?? []).Select(x => x?.Trim() ?? string.Empty).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            if (scopes.Length == 0 || scopes.Any(x => !KnownScopes.Contains(x, StringComparer.Ordinal)))
                return Validation("scopes", "En az bir geçerli salt okunur API yetkisi seçin.");
            var role = user.FindFirstValue("company_role") ?? string.Empty;
            if (scopes.Contains("people:read", StringComparer.Ordinal) && !PeopleReaders.Contains(role))
                return Results.Forbid();
            if (scopes.Any(x => x != "people:read") && !AssetReaders.Contains(role))
                return Results.Forbid();
            if (request.ExpiresAt is DateTimeOffset expiry &&
                (expiry <= DateTimeOffset.UtcNow.AddHours(1) || expiry > DateTimeOffset.UtcNow.AddDays(365)))
                return Validation("expiresAt", "Son kullanma tarihi en az 1 saat sonra ve en fazla 365 gün içinde olmalıdır.");
            if (await db.CompanyApiTokens.CountAsync(x => x.CompanyId == companyId && x.UserId == userId && x.RevokedAt == null &&
                    (x.ExpiresAt == null || x.ExpiresAt > DateTimeOffset.UtcNow), ct) >= 10)
                return Results.Problem("Kullanıcı başına en fazla 10 etkin API anahtarı oluşturulabilir.", statusCode: StatusCodes.Status409Conflict);

            var secret = "etr_live_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var token = new CompanyApiToken
            {
                CompanyId = companyId, UserId = userId, Name = name, TokenPrefix = secret[..16],
                TokenHash = Hash(secret), ScopesJson = JsonSerializer.Serialize(scopes), ExpiresAt = request.ExpiresAt
            };
            db.CompanyApiTokens.Add(token);
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId, ActorUserId = userId, TargetType = "CompanyApiToken", TargetId = token.Id,
                Action = "api-token.created", Details = $"{name}; yetkiler: {string.Join(", ", scopes)}"
            });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/integrations/tokens/{token.Id}", new
            {
                token.Id, token.Name, token.TokenPrefix, Scopes = scopes, token.CreatedAt, token.ExpiresAt,
                Secret = secret, SecretNotice = "Bu gizli anahtar yalnızca bu yanıt içinde gösterilir. Güvenli bir yerde saklayın."
            });
        }).RequireAuthorization("company.member");

        app.MapGet("/api/integrations/tokens/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentityScope(user, out var companyId, out var userId)) return Results.Forbid();
            var token = await db.CompanyApiTokens.AsNoTracking().SingleOrDefaultAsync(
                x => x.Id == id && x.CompanyId == companyId && x.UserId == userId, ct);
            if (token is null) return Results.NotFound();
            return Results.Ok(new
            {
                token.Id, token.Name, token.TokenPrefix, Scopes = JsonSerializer.Deserialize<string[]>(token.ScopesJson) ?? [],
                token.CreatedAt, token.ExpiresAt, token.LastUsedAt, token.RevokedAt,
                IsExpired = token.ExpiresAt is DateTimeOffset expiry && expiry <= DateTimeOffset.UtcNow
            });
        }).RequireAuthorization("company.member");

        app.MapPost("/api/integrations/tokens/{id:guid}/revoke", async (Guid id, ClaimsPrincipal user, HttpContext context,
            AppDbContext db, IAntiforgery antiforgery, CancellationToken ct) =>
        {
            if (!TryIdentityScope(user, out var companyId, out var userId)) return Results.Forbid();
            if (!await ValidateCsrfAsync(context, antiforgery)) return Results.StatusCode(StatusCodes.Status400BadRequest);
            var token = await db.CompanyApiTokens.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId && x.UserId == userId, ct);
            if (token is null) return Results.NotFound();
            if (token.RevokedAt is null)
            {
                token.RevokedAt = DateTimeOffset.UtcNow;
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId, TargetType = "CompanyApiToken",
                    TargetId = token.Id, Action = "api-token.revoked", Details = token.Name });
                await db.SaveChangesAsync(ct);
            }
            return Results.NoContent();
        }).RequireAuthorization("company.member");

        app.MapGet("/api/v1", () => Results.Ok(new
        {
            name = "EnvanterTR Customer API", version = "1", authentication = "Authorization: Bearer <token>",
            endpoints = new[] { "GET /api/v1/assets", "GET /api/v1/accessories", "GET /api/v1/employees" },
            pagination = new { page = 1, pageSize = 50, maximumPageSize = 100 }
        })).AllowAnonymous().RequireRateLimiting("customer-api");

        app.MapGet("/api/v1/assets", async (HttpContext context, AppDbContext db, [FromQuery] string? q,
            [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct) =>
        {
            var access = await AuthenticateAsync(context, db, "assets:read", ct);
            if (access.Error is not null) return access.Error;
            var companyId = access.Token!.CompanyId;
            var query = db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId && x.DeletedAt == null);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Validation("q", "Arama en fazla 120 karakter olabilir.");
                query = query.Where(x => x.AssetTag.Contains(term) || x.Name.Contains(term) ||
                    (x.SerialNumber != null && x.SerialNumber.Contains(term)));
            }
            var total = await query.CountAsync(ct);
            var (safePage, safeSize) = NormalizePage(page ?? 1, pageSize ?? 50);
            var items = await query.OrderBy(x => x.AssetTag).Skip((safePage - 1) * safeSize).Take(safeSize)
                .Select(x => new
                {
                    x.Id, x.AssetTag, x.Name, x.SerialNumber, x.Status, x.Location, x.PurchaseDate, x.WarrantyExpiresAt,
                    Category = db.AssetCategories.Where(c => c.CompanyId == companyId && c.Id == x.CategoryId).Select(c => c.Name).FirstOrDefault(),
                    Model = db.AssetModels.Where(m => m.CompanyId == companyId && m.Id == x.ModelId).Select(m => m.Name).FirstOrDefault(),
                    Brand = db.AssetBrands.Where(b => b.CompanyId == companyId && b.Id == x.BrandId).Select(b => b.Name).FirstOrDefault(),
                    AssignedEmployee = db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.AssignedEmployeeId).Select(e => e.DisplayName).FirstOrDefault()
                }).ToListAsync(ct);
            return Results.Ok(new { items, total, page = safePage, pageSize = safeSize });
        }).AllowAnonymous().RequireRateLimiting("customer-api");

        app.MapGet("/api/v1/accessories", async (HttpContext context, AppDbContext db, [FromQuery] string? q,
            [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct) =>
        {
            var access = await AuthenticateAsync(context, db, "accessories:read", ct);
            if (access.Error is not null) return access.Error;
            var companyId = access.Token!.CompanyId;
            var query = db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Validation("q", "Arama en fazla 120 karakter olabilir.");
                query = query.Where(x => x.Name.Contains(term) || (x.ModelNumber != null && x.ModelNumber.Contains(term)) ||
                    (x.ItemNumber != null && x.ItemNumber.Contains(term)));
            }
            var total = await query.CountAsync(ct);
            var (safePage, safeSize) = NormalizePage(page ?? 1, pageSize ?? 50);
            var items = await query.OrderBy(x => x.Name).Skip((safePage - 1) * safeSize).Take(safeSize)
                .Select(x => new
                {
                    x.Id, x.Name, Category = db.AccessoryCategories.Where(c => c.CompanyId == companyId && c.Id == x.CategoryId).Select(c => c.Name).FirstOrDefault() ?? x.Category,
                    x.ModelNumber, x.ItemNumber, x.Location, x.MinimumQuantity, x.TotalQuantity,
                    CheckedOutQuantity = db.AccessoryAssignments.Where(a => a.CompanyId == companyId && a.AccessoryId == x.Id)
                        .Sum(a => (int?)(a.Quantity - a.ReturnedQuantity)) ?? 0,
                    x.PurchaseDate, x.WarrantyExpiresAt
                }).ToListAsync(ct);
            return Results.Ok(new { items, total, page = safePage, pageSize = safeSize });
        }).AllowAnonymous().RequireRateLimiting("customer-api");

        app.MapGet("/api/v1/employees", async (HttpContext context, AppDbContext db, [FromQuery] string? q,
            [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct) =>
        {
            var access = await AuthenticateAsync(context, db, "people:read", ct);
            if (access.Error is not null) return access.Error;
            var companyId = access.Token!.CompanyId;
            var query = db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Validation("q", "Arama en fazla 120 karakter olabilir.");
                query = query.Where(x => x.EmployeeNumber.Contains(term) || x.DisplayName.Contains(term) ||
                    (x.Email != null && x.Email.Contains(term)) || (x.Department != null && x.Department.Contains(term)));
            }
            var total = await query.CountAsync(ct);
            var (safePage, safeSize) = NormalizePage(page ?? 1, pageSize ?? 50);
            var items = await query.OrderBy(x => x.DisplayName).Skip((safePage - 1) * safeSize).Take(safeSize)
                .Select(x => new { x.Id, x.EmployeeNumber, x.DisplayName, x.Email, x.Department, x.JobTitle, x.Location, x.StartedOn, x.IsActive })
                .ToListAsync(ct);
            return Results.Ok(new { items, total, page = safePage, pageSize = safeSize });
        }).AllowAnonymous().RequireRateLimiting("customer-api");
    }

    private static async Task<(CompanyApiToken? Token, IResult? Error)> AuthenticateAsync(HttpContext context, AppDbContext db,
        string scope, CancellationToken ct)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer etr_live_", StringComparison.Ordinal))
            return (null, Results.Unauthorized());
        var raw = header["Bearer ".Length..].Trim();
        if (raw.Length is < 40 or > 80) return (null, Results.Unauthorized());
        var hash = Hash(raw);
        var token = await db.CompanyApiTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        var now = DateTimeOffset.UtcNow;
        if (token is null || token.RevokedAt is not null || (token.ExpiresAt is DateTimeOffset expiry && expiry <= now))
            return (null, Results.Unauthorized());
        string[] scopes;
        try { scopes = JsonSerializer.Deserialize<string[]>(token.ScopesJson) ?? []; }
        catch (JsonException) { return (null, Results.Unauthorized()); }
        if (!scopes.Contains(scope, StringComparer.Ordinal)) return (null, Results.Forbid());

        var membership = await (from member in db.CompanyMemberships.AsNoTracking()
            join company in db.Companies.AsNoTracking() on member.CompanyId equals company.Id
            join user in db.Users.AsNoTracking() on member.UserId equals user.Id
            where member.CompanyId == token.CompanyId && member.UserId == token.UserId && company.IsActive &&
                (!user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd <= now)
            select member.RoleKey).SingleOrDefaultAsync(ct);
        var roleAllowed = scope == "people:read" ? membership is not null && PeopleReaders.Contains(membership) :
            membership is not null && AssetReaders.Contains(membership);
        if (!roleAllowed) return (null, Results.Unauthorized());
        token.LastUsedAt = now;
        await db.SaveChangesAsync(ct);
        return (token, null);
    }

    private static (int Page, int PageSize) NormalizePage(int page, int pageSize) =>
        (Math.Max(page, 1), Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 100));

    private static string Hash(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret))).ToLowerInvariant();
    private static bool TryIdentityScope(ClaimsPrincipal user, out Guid companyId, out Guid userId)
    {
        companyId = Guid.Empty;
        userId = Guid.Empty;
        return Guid.TryParse(user.FindFirstValue("company_id"), out companyId) &&
            Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
    private static async Task<bool> ValidateCsrfAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try { await antiforgery.ValidateRequestAsync(context); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }
    private static IResult Validation(string key, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [key] = [message] });

    public sealed record CreateTokenRequest(string? Name, string[]? Scopes, DateTimeOffset? ExpiresAt);
}
