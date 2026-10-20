using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnvanterTR.Web.Data;

public static class CustomerApiEndpoints
{
    private static readonly string[] KnownScopes = ["assets:read", "assets:write", "accessories:read", "people:read"];

    public static void MapCustomerApiEndpoints(this WebApplication app)
    {
        var tokenManagement = app.MapGroup("/api/integrations/tokens").RequireAuthorization("company.member");
        tokenManagement.AddEndpointFilter(async (invocation, next) =>
        {
            if (!TryIdentityScope(invocation.HttpContext.User, out var companyId)) return Results.Forbid();
            var db = invocation.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            if (!await TenantPlanFeatures.IsEnabledAsync(companyId, TenantPlanFeatures.CustomerApi, db,
                    invocation.HttpContext.RequestAborted))
                return TenantPlanFeatures.FeatureUnavailable(TenantPlanFeatures.CustomerApi);
            return await next(invocation);
        });

        tokenManagement.MapGet("", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
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
        });

        tokenManagement.MapPost("", async (CreateTokenRequest request, ClaimsPrincipal user, HttpContext context,
            AppDbContext db, IAntiforgery antiforgery, CancellationToken ct) =>
        {
            if (!TryIdentityScope(user, out var companyId, out var userId)) return Results.Forbid();
            if (!await ValidateCsrfAsync(context, antiforgery)) return Results.StatusCode(StatusCodes.Status400BadRequest);
            var name = (request.Name ?? string.Empty).Trim();
            if (name.Length is < 2 or > 100) return Validation("name", "Ad 2 ile 100 karakter arasında olmalıdır.");
            var scopes = (request.Scopes ?? []).Select(x => x?.Trim() ?? string.Empty).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            if (scopes.Length == 0 || scopes.Any(x => !KnownScopes.Contains(x, StringComparer.Ordinal)))
                return Validation("scopes", "En az bir geçerli API yetkisi seçin.");
            var role = await db.CompanyMemberships.AsNoTracking()
                .Where(member => member.CompanyId == companyId && member.UserId == userId)
                .Select(member => member.RoleKey).SingleOrDefaultAsync(ct);
            if (role is null) return Results.Forbid();
            var permissions = await CompanyRoleAuthorization.ResolveAsync(db, companyId, role, ct);
            if (!permissions.Valid || scopes.Any(scope => !IsScopeAllowed(scope, permissions.PermissionKeys)))
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
        });

        tokenManagement.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
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
        });

        tokenManagement.MapPost("/{id:guid}/revoke", async (Guid id, ClaimsPrincipal user, HttpContext context,
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
        });

        app.MapGet("/api/v1", () => Results.Ok(new
        {
            name = "EnvanterTR Customer API", version = "1", authentication = "Authorization: Bearer <token>",
            endpoints = new[] { "GET /api/v1/assets", "POST /api/v1/assets", "PUT /api/v1/assets/{id}", "PUT /api/v1/assets/{id}/installed-software", "GET /api/v1/asset-custom-fields", "GET /api/v1/accessories", "GET /api/v1/employees" },
            openApi = "/openapi/customer-api-v1.json",
            pagination = new { page = 1, pageSize = 50, maximumPageSize = 100 }
        })).AllowAnonymous().RequireRateLimiting("customer-api");

        app.MapPost("/api/v1/assets", async (HttpContext context, AppDbContext db, AssetCustomFieldProtection customFieldProtection,
            [FromBody] CreateAssetRequest request, CancellationToken ct) =>
        {
            var access = await AuthenticateAsync(context, db, "assets:write", ct);
            if (access.Error is not null) return access.Error;
            var token = access.Token!;
            var companyId = token.CompanyId;
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, token.UserId, ct);
            if (string.IsNullOrWhiteSpace(request.AssetTag) || string.IsNullOrWhiteSpace(request.Name) || request.CategoryId == Guid.Empty)
                return Validation("asset", "Etiket, ad ve şirket içindeki kategori zorunludur.");
            var tag = request.AssetTag.Trim().ToUpperInvariant();
            var name = request.Name.Trim();
            var serial = string.IsNullOrWhiteSpace(request.SerialNumber) ? null : request.SerialNumber.Trim();
            var location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();
            var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            var currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant();
            var insuranceProvider = string.IsNullOrWhiteSpace(request.InsuranceProvider) ? null : request.InsuranceProvider.Trim();
            var insurancePolicyNumber = string.IsNullOrWhiteSpace(request.InsurancePolicyNumber) ? null : request.InsurancePolicyNumber.Trim();
            if (tag.Length > 80 || name.Length > 240 || serial?.Length > 160 || location?.Length > 160 || notes?.Length > 4000 ||
                insuranceProvider?.Length > 160 || insurancePolicyNumber?.Length > 120)
                return Validation("asset", "Etiket/ad/seri numarası/konum/not alanlarından biri izin verilen uzunluğu aşıyor.");
            if (request.PurchaseCost is < 0 or > 9999999999999999.99m || currency.Length != 3 || currency.Any(c => !char.IsAsciiLetter(c)))
                return Validation("purchaseCost", "Alış bedeli negatif olamaz ve para birimi üç harfli kod olmalıdır.");
            var customFieldDefinitions = await db.AssetCustomFieldDefinitions.AsNoTracking()
                .Where(field => field.CompanyId == companyId && field.IsActive).ToListAsync(ct);
            if (AssetCustomFieldEndpoints.HasSensitiveInput(customFieldDefinitions, request.CustomFields?.Keys) &&
                !CanReadSensitive(access.Role)) return Results.Forbid();
            var normalizedCustomFields = AssetCustomFieldEndpoints.NormalizeValues(customFieldDefinitions, request.CustomFields,
                categoryId: request.CategoryId, modelId: request.ModelId);
            if (normalizedCustomFields.Error is not null)
                return Validation("customFields", normalizedCustomFields.Error);
            var protectedCustomFields = customFieldProtection.Protect(normalizedCustomFields.Json, null, companyId, customFieldDefinitions);
            if (protectedCustomFields.Error is not null)
                return Validation("customFields", protectedCustomFields.Error);
            if (await db.Assets.AnyAsync(x => x.CompanyId == companyId && x.AssetTag == tag, ct))
                return Results.Conflict(new { message = "Bu şirkette bu varlık etiketi zaten kullanılıyor." });
            if (!await db.AssetCategories.AnyAsync(x => x.CompanyId == companyId && x.Id == request.CategoryId, ct))
                return Validation("categoryId", "Kategori bu şirkette bulunamadı.");
            if (request.BrandId is Guid brandId && !await db.AssetBrands.AnyAsync(x => x.CompanyId == companyId && x.Id == brandId, ct))
                return Validation("brandId", "Marka bu şirkette bulunamadı.");
            if (request.ModelId is Guid modelId && !await db.AssetModels.AnyAsync(x => x.CompanyId == companyId && x.Id == modelId && x.BrandId == request.BrandId && (x.CategoryId == null || x.CategoryId == request.CategoryId), ct))
                return Validation("modelId", "Model seçilen marka, kategori ve şirketle eşleşmiyor.");
            if (request.DefaultLocationCatalogId is Guid locationId && !await db.CompanyLocations.AnyAsync(x => x.CompanyId == companyId && x.Id == locationId && x.IsActive, ct))
                return Validation("defaultLocationCatalogId", "Varsayılan konum bu şirkette bulunamadı.");

            var resolvedLocationId = await AssetEndpoints.ResolveLocationCatalogIdAsync(db, companyId, location, ct);
            if (allowedLocations is not null && (resolvedLocationId is not Guid allowedLocationId ||
                !allowedLocations.Contains(allowedLocationId) ||
                request.DefaultLocationCatalogId is Guid requestedDefault && !allowedLocations.Contains(requestedDefault)))
                return Results.Forbid();
            if (resolvedLocationId is Guid resolvedId && !await db.CompanyLocations.AnyAsync(x =>
                    x.CompanyId == companyId && x.Id == resolvedId && x.IsActive, ct))
                return Validation("location", "Konum şirket içinde etkin olmalıdır.");

            var actorId = token.UserId;
            var now = DatabaseTimestampNow();
            var asset = new Asset
            {
                CompanyId = companyId, AssetTag = tag, Name = name, CategoryId = request.CategoryId,
                BrandId = request.BrandId, ModelId = request.ModelId, SerialNumber = serial,
                Location = location, LocationCatalogId = resolvedLocationId,
                DefaultLocationCatalogId = request.DefaultLocationCatalogId, PurchaseDate = request.PurchaseDate,
                WarrantyExpiresAt = request.WarrantyExpiresAt, InsuranceProvider = insuranceProvider,
                InsurancePolicyNumber = insurancePolicyNumber, InsuranceExpiresAt = request.InsuranceExpiresAt,
                PurchaseCost = request.PurchaseCost, Currency = currency,
                IsRequestable = request.IsRequestable == true, IsByod = request.IsByod == true, Notes = notes,
                CustomFieldsJson = protectedCustomFields.Json,
                CreatedAt = now, UpdatedAt = now
            };
            db.Assets.Add(asset);
            db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = asset.Id, ActorUserId = actorId,
                EventType = "Created", Details = $"Varlık müşteri API anahtarıyla oluşturuldu ({token.TokenPrefix})." });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Asset", TargetId = asset.Id, Action = "asset.api-created", Details = $"API anahtarı: {token.Name}" });
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException exception) when (AssetPlanQuota.IsExceeded(exception)) { return AssetPlanQuota.ExceededResult(); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Varlık kaydı çakıştı; etiket ve ilişkili katalog kayıtlarını kontrol edin." }); }
            return Results.Created($"/api/v1/assets/{asset.Id}", new { asset.Id, asset.AssetTag, asset.Name,
                asset.Status, asset.SerialNumber, asset.Location, asset.CategoryId, asset.BrandId, asset.ModelId,
                asset.PurchaseDate, asset.WarrantyExpiresAt, asset.InsuranceProvider, asset.InsurancePolicyNumber,
                asset.InsuranceExpiresAt, asset.UpdatedAt,
                CustomFields = customFieldProtection.Read(asset.CustomFieldsJson, customFieldDefinitions, companyId,
                    revealSensitive: CanReadSensitive(access.Role)) });
        }).AllowAnonymous().RequireRateLimiting("customer-api").WithMetadata(new RequestSizeLimitAttribute(65_536));

        app.MapPut("/api/v1/assets/{id:guid}", async (Guid id, HttpContext context, AppDbContext db,
            AssetCustomFieldProtection customFieldProtection,
            [FromBody] UpdateAssetRequest request, CancellationToken ct) =>
        {
            var access = await AuthenticateAsync(context, db, "assets:write", ct);
            if (access.Error is not null) return access.Error;
            var token = access.Token!;
            var companyId = token.CompanyId;
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, token.UserId, ct);
            var tag = request.AssetTag?.Trim().ToUpperInvariant();
            var name = request.Name?.Trim();
            var serial = string.IsNullOrWhiteSpace(request.SerialNumber) ? null : request.SerialNumber.Trim();
            var location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();
            var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            var currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(tag) || tag.Length > 80 || string.IsNullOrWhiteSpace(name) || name.Length > 240 || request.CategoryId == Guid.Empty)
                return Validation("asset", "Etiket, ad ve şirket içindeki kategori zorunludur; etiket/ad sınırlarını kontrol edin.");
            if (serial?.Length > 160 || location?.Length > 160 || notes?.Length > 4000)
                return Validation("asset", "Seri numarası, konum veya not alanı izin verilen uzunluğu aşıyor.");
            var insuranceProvider = string.IsNullOrWhiteSpace(request.InsuranceProvider) ? null : request.InsuranceProvider.Trim();
            var insurancePolicyNumber = string.IsNullOrWhiteSpace(request.InsurancePolicyNumber) ? null : request.InsurancePolicyNumber.Trim();
            if (insuranceProvider?.Length > 160 || insurancePolicyNumber?.Length > 120)
                return Validation("insurance", "Sigorta şirketi en fazla 160, poliçe numarası en fazla 120 karakter olabilir.");
            if (request.PurchaseCost is < 0 or > 9999999999999999.99m || currency.Length != 3 || currency.Any(c => !char.IsAsciiLetter(c)))
                return Validation("purchaseCost", "Alış bedeli negatif olamaz ve para birimi üç harfli kod olmalıdır.");
            if (request.ExpectedUpdatedAt == default)
                return Validation("expectedUpdatedAt", "Eşzamanlı düzenleme koruması için son okuma yanıtındaki updatedAt değerini gönderin.");
            var current = await db.Assets.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (current is null) return Results.NotFound();
            if (current.UpdatedAt != request.ExpectedUpdatedAt)
                return Results.Conflict(new { message = "Varlık siz okuduktan sonra değişti. Güncel kaydı alın ve tekrar deneyin.", current.UpdatedAt });
            if (await db.Assets.AnyAsync(x => x.CompanyId == companyId && x.Id != id && x.AssetTag == tag, ct))
                return Results.Conflict(new { message = "Bu şirkette bu varlık etiketi zaten kullanılıyor." });
            if (!await db.AssetCategories.AnyAsync(x => x.CompanyId == companyId && x.Id == request.CategoryId, ct))
                return Validation("categoryId", "Kategori bu şirkette bulunamadı.");
            if (request.BrandId is Guid brandId && !await db.AssetBrands.AnyAsync(x => x.CompanyId == companyId && x.Id == brandId, ct))
                return Validation("brandId", "Marka bu şirkette bulunamadı.");
            if (request.ModelId is Guid modelId && !await db.AssetModels.AnyAsync(x => x.CompanyId == companyId && x.Id == modelId && x.BrandId == request.BrandId && (x.CategoryId == null || x.CategoryId == request.CategoryId), ct))
                return Validation("modelId", "Model seçilen marka, kategori ve şirketle eşleşmiyor.");
            if (request.DefaultLocationCatalogId is Guid defaultLocationId && !await db.CompanyLocations.AnyAsync(x => x.CompanyId == companyId && x.Id == defaultLocationId && x.IsActive, ct))
                return Validation("defaultLocationCatalogId", "Varsayılan konum bu şirkette bulunamadı.");

            var customFieldDefinitions = await db.AssetCustomFieldDefinitions.AsNoTracking()
                .Where(field => field.CompanyId == companyId && field.IsActive).ToListAsync(ct);
            if (AssetCustomFieldEndpoints.HasSensitiveInput(customFieldDefinitions, request.CustomFields?.Keys) &&
                !CanReadSensitive(access.Role)) return Results.Forbid();
            var existingCustomFieldValuesJson = JsonSerializer.Serialize(customFieldProtection.Read(
                current.CustomFieldsJson, customFieldDefinitions, companyId, revealSensitive: true));
            var normalizedCustomFields = AssetCustomFieldEndpoints.NormalizeValues(customFieldDefinitions, request.CustomFields,
                existingCustomFieldValuesJson, preserveWhenMissing: true, categoryId: request.CategoryId, modelId: request.ModelId);
            if (normalizedCustomFields.Error is not null)
                return Validation("customFields", normalizedCustomFields.Error);
            var protectedCustomFields = customFieldProtection.Protect(normalizedCustomFields.Json,
                current.CustomFieldsJson, companyId, customFieldDefinitions);
            if (protectedCustomFields.Error is not null)
                return Validation("customFields", protectedCustomFields.Error);

            var locationId = await AssetEndpoints.ResolveLocationCatalogIdAsync(db, companyId, location, ct);
            if (allowedLocations is not null &&
                (current.LocationCatalogId is not Guid currentLocationId || !allowedLocations.Contains(currentLocationId) ||
                 locationId is not Guid targetLocationId || !allowedLocations.Contains(targetLocationId) ||
                 request.DefaultLocationCatalogId is Guid requestedDefault && !allowedLocations.Contains(requestedDefault)))
                return Results.NotFound();
            if (locationId is Guid resolvedId && !await db.CompanyLocations.AnyAsync(x =>
                    x.CompanyId == companyId && x.Id == resolvedId && x.IsActive, ct))
                return Validation("location", "Konum şirket içinde etkin olmalıdır.");
            var isRequestable = request.IsRequestable ?? current.IsRequestable;
            var isByod = request.IsByod ?? current.IsByod;
            var now = DatabaseTimestampNow();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var affected = await db.Assets.Where(x => x.CompanyId == companyId && x.Id == id && x.UpdatedAt == request.ExpectedUpdatedAt)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.AssetTag, tag!)
                        .SetProperty(x => x.Name, name!).SetProperty(x => x.CategoryId, request.CategoryId)
                        .SetProperty(x => x.BrandId, request.BrandId).SetProperty(x => x.ModelId, request.ModelId)
                        .SetProperty(x => x.SerialNumber, serial).SetProperty(x => x.Location, location)
                        .SetProperty(x => x.LocationCatalogId, locationId).SetProperty(x => x.DefaultLocationCatalogId, request.DefaultLocationCatalogId)
                        .SetProperty(x => x.PurchaseDate, request.PurchaseDate).SetProperty(x => x.WarrantyExpiresAt, request.WarrantyExpiresAt)
                        .SetProperty(x => x.InsuranceProvider, insuranceProvider).SetProperty(x => x.InsurancePolicyNumber, insurancePolicyNumber)
                        .SetProperty(x => x.InsuranceExpiresAt, request.InsuranceExpiresAt)
                        .SetProperty(x => x.PurchaseCost, request.PurchaseCost).SetProperty(x => x.Currency, currency)
                        .SetProperty(x => x.IsRequestable, isRequestable).SetProperty(x => x.IsByod, isByod)
                        .SetProperty(x => x.Notes, notes).SetProperty(x => x.CustomFieldsJson, protectedCustomFields.Json)
                        .SetProperty(x => x.UpdatedAt, now), ct);
                if (affected == 0)
                {
                    await transaction.RollbackAsync(ct);
                    return Results.Conflict(new { message = "Varlık eşzamanlı olarak değişti. Güncel kaydı alıp tekrar deneyin." });
                }
                db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = id, ActorUserId = token.UserId,
                    EventType = "Updated", Details = $"Varlık müşteri API anahtarıyla güncellendi ({token.TokenPrefix})." });
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = token.UserId,
                    TargetType = "Asset", TargetId = id, Action = "asset.api-updated", Details = $"API anahtarı: {token.Name}" });
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                await transaction.RollbackAsync(ct);
                return Results.Conflict(new { message = "Bu şirket içinde bu varlık etiketi zaten kullanılıyor." });
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                await transaction.RollbackAsync(ct);
                return Results.Conflict(new { message = "Bu şirket içinde bu varlık etiketi zaten kullanılıyor." });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(ct);
                return Results.Conflict(new { message = "Kayıt çakıştı; etiket ve ilişkili katalogları kontrol edin." });
            }
            return Results.Ok(new { id, updatedAt = now, changed = true });
        }).AllowAnonymous().RequireRateLimiting("customer-api").WithMetadata(new RequestSizeLimitAttribute(65_536));

        app.MapPut("/api/v1/assets/{id:guid}/installed-software", async (Guid id, HttpContext context, AppDbContext db,
            [FromBody] SoftwareInventoryEndpoints.SoftwareInventorySnapshotRequest request, CancellationToken ct) =>
        {
            var access = await AuthenticateAsync(context, db, "assets:write", ct);
            if (access.Error is not null) return access.Error;
            var token = access.Token!;
            var asset = await db.Assets.AsNoTracking().Where(x => x.CompanyId == token.CompanyId && x.Id == id && x.DeletedAt == null)
                .Select(x => new { x.Id, x.LocationCatalogId }).SingleOrDefaultAsync(ct);
            if (asset is null) return Results.NotFound();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, token.CompanyId, token.UserId, ct);
            if (allowedLocations is not null && (asset.LocationCatalogId is not Guid locationId || !allowedLocations.Contains(locationId)))
                return Results.NotFound();
            return await SoftwareInventoryEndpoints.ReplaceSnapshotAsync(db, token.CompanyId, id, token.UserId,
                request.Source, request.IsComplete, request.Items, ct) ?? Results.Problem();
        }).AllowAnonymous().RequireRateLimiting("customer-api").WithMetadata(new RequestSizeLimitAttribute(4_194_304));

        app.MapGet("/api/v1/assets", async (HttpContext context, AppDbContext db,
            AssetCustomFieldProtection customFieldProtection, [FromQuery] string? q,
            [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct) =>
        {
            var access = await AuthenticateAsync(context, db, "assets:read", ct);
            if (access.Error is not null) return access.Error;
            var token = access.Token!;
            var companyId = token.CompanyId;
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, token.UserId, ct);
            var query = db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId && x.DeletedAt == null);
            if (allowedLocations is not null)
                query = query.Where(x => x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value));
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
                    x.InsuranceProvider, x.InsurancePolicyNumber, x.InsuranceExpiresAt, x.UpdatedAt,
                    x.CustomFieldsJson,
                    Category = db.AssetCategories.Where(c => c.CompanyId == companyId && c.Id == x.CategoryId).Select(c => c.Name).FirstOrDefault(),
                    Model = db.AssetModels.Where(m => m.CompanyId == companyId && m.Id == x.ModelId).Select(m => m.Name).FirstOrDefault(),
                    Brand = db.AssetBrands.Where(b => b.CompanyId == companyId && b.Id == x.BrandId).Select(b => b.Name).FirstOrDefault(),
                    AssignedEmployee = db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.AssignedEmployeeId &&
                        (allowedLocations == null || e.LocationCatalogId.HasValue && allowedLocations.Contains(e.LocationCatalogId.Value)))
                        .Select(e => e.DisplayName).FirstOrDefault()
                }).ToListAsync(ct);
            var fieldDefinitions = await db.AssetCustomFieldDefinitions.AsNoTracking()
                .Where(field => field.CompanyId == companyId).ToListAsync(ct);
            var visibleFieldKeys = fieldDefinitions.Where(field => field.IsActive).Select(field => field.Key).ToHashSet(StringComparer.Ordinal);
            var canReadSensitive = CanReadSensitive(access.Role);
            var responseItems = items.Select(item =>
            {
                var values = customFieldProtection.Read(item.CustomFieldsJson, fieldDefinitions, companyId, revealSensitive: canReadSensitive)
                    .Where(pair => visibleFieldKeys.Contains(pair.Key))
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
                return new { item.Id, item.AssetTag, item.Name, item.SerialNumber, item.Status, item.Location,
                    item.PurchaseDate, item.WarrantyExpiresAt, item.InsuranceProvider, item.InsurancePolicyNumber,
                    item.InsuranceExpiresAt, item.UpdatedAt, item.Category, item.Model, item.Brand,
                    item.AssignedEmployee, CustomFields = values };
            }).ToArray();
            return Results.Ok(new { items = responseItems, total, page = safePage, pageSize = safeSize });
        }).AllowAnonymous().RequireRateLimiting("customer-api");

        app.MapGet("/api/v1/asset-custom-fields", async (HttpContext context, AppDbContext db, CancellationToken ct) =>
        {
            var access = await AuthenticateAsync(context, db, "assets:read", ct);
            if (access.Error is not null) return access.Error;
            var companyId = access.Token!.CompanyId;
            var fields = await db.AssetCustomFieldDefinitions.AsNoTracking()
                .Where(field => field.CompanyId == companyId && field.IsActive)
                .OrderBy(field => field.SortOrder).ThenBy(field => field.Name)
                .Select(field => new { field.Key, field.Name, field.FieldType, field.OptionsJson, field.IsRequired, field.SortOrder })
                .ToListAsync(ct);
            var hasWriteScope = (JsonSerializer.Deserialize<string[]>(access.Token.ScopesJson) ?? [])
                .Contains("assets:write", StringComparer.Ordinal);
            var permissions = await CompanyRoleAuthorization.ResolveAsync(db, companyId, access.Role!, ct);
            var canWrite = hasWriteScope && permissions.Valid && IsScopeAllowed("assets:write", permissions.PermissionKeys);
            return Results.Ok(fields.Select(field => new
            {
                field.Key, field.Name, field.FieldType,
                Options = field.FieldType == "select" ? ParseOptions(field.OptionsJson) : [],
                field.IsRequired, field.SortOrder,
                CanWriteValue = canWrite && (field.FieldType != "sensitive" || CanReadSensitive(access.Role))
            }));
        }).AllowAnonymous().RequireRateLimiting("customer-api");

        app.MapGet("/api/v1/accessories", async (HttpContext context, AppDbContext db, [FromQuery] string? q,
            [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct) =>
        {
            var access = await AuthenticateAsync(context, db, "accessories:read", ct);
            if (access.Error is not null) return access.Error;
            var token = access.Token!;
            var companyId = token.CompanyId;
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, token.UserId, ct);
            var allowedLocationPaths = allowedLocations is null ? Array.Empty<string>() :
                (await AssetEndpoints.GetActiveLocationPathMapAsync(db, companyId, ct))
                .Where(path => allowedLocations.Contains(path.Value)).Select(path => path.Key).ToArray();
            var query = db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (allowedLocations is not null)
                query = query.Where(x => x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value));
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
                    CheckedOutQuantity = db.AccessoryAssignments.Where(a => a.CompanyId == companyId && a.AccessoryId == x.Id &&
                        (allowedLocations == null || a.TargetType == "Employee" && db.Employees.Any(e => e.CompanyId == companyId && e.Id == a.EmployeeId &&
                            e.LocationCatalogId.HasValue && allowedLocations.Contains(e.LocationCatalogId.Value)) ||
                         a.TargetType == "Asset" && db.Assets.Any(asset => asset.CompanyId == companyId && asset.Id == a.AssetId &&
                            asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value)) ||
                         a.TargetType == "Location" && a.Location != null && allowedLocationPaths.Contains(a.Location)))
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
            var token = access.Token!;
            var companyId = token.CompanyId;
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, token.UserId, ct);
            var allowedLocations = employeeScope.AllowedLocations;
            var allowedCompanyUnits = employeeScope.AllowedCompanyUnits;
            var query = employeeScope.Apply(db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId));
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Validation("q", "Arama en fazla 120 karakter olabilir.");
                var searchPattern = $"%{term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
                query = query.Where(x => EF.Functions.ILike(x.EmployeeNumber, searchPattern, "\\") || EF.Functions.ILike(x.DisplayName, searchPattern, "\\") ||
                    (x.Email != null && EF.Functions.ILike(x.Email, searchPattern, "\\")) || (x.Department != null && EF.Functions.ILike(x.Department, searchPattern, "\\")) ||
                    (x.CompanyUnitId.HasValue && db.CompanyUnits.Any(unit => unit.CompanyId == companyId && unit.Id == x.CompanyUnitId &&
                        (allowedCompanyUnits == null || allowedCompanyUnits.Contains(unit.Id)) && EF.Functions.ILike(unit.Name, searchPattern, "\\"))) ||
                    db.CompanyLocations.Any(location => location.CompanyId == companyId && location.Id == x.LocationCatalogId &&
                        (allowedLocations == null || allowedLocations.Contains(location.Id)) && location.CompanyUnitId.HasValue &&
                        db.CompanyUnits.Any(unit => unit.CompanyId == companyId && unit.Id == location.CompanyUnitId && EF.Functions.ILike(unit.Name, searchPattern, "\\"))));
            }
            var total = await query.CountAsync(ct);
            var (safePage, safeSize) = NormalizePage(page ?? 1, pageSize ?? 50);
            var items = await query.OrderBy(x => x.DisplayName).Skip((safePage - 1) * safeSize).Take(safeSize)
                .Select(x => new
                {
                    x.Id, x.EmployeeNumber, x.DisplayName, x.Email, x.Department, x.JobTitle,
                    Location = allowedLocations == null ? x.Location : db.CompanyLocations
                        .Where(location => location.CompanyId == companyId && location.Id == x.LocationCatalogId && allowedLocations.Contains(location.Id))
                        .Select(location => location.Name).FirstOrDefault(),
                    CompanyUnitId = x.CompanyUnitId ?? db.CompanyLocations.Where(location => location.CompanyId == companyId && location.Id == x.LocationCatalogId &&
                            (allowedLocations == null || allowedLocations.Contains(location.Id)))
                        .Select(location => location.CompanyUnitId).FirstOrDefault(),
                    CompanyUnitName = x.CompanyUnitId.HasValue
                        ? db.CompanyUnits.Where(unit => unit.CompanyId == companyId && unit.Id == x.CompanyUnitId &&
                            (allowedCompanyUnits == null || allowedCompanyUnits.Contains(unit.Id))).Select(unit => unit.Name).FirstOrDefault()
                        : db.CompanyLocations.Where(location => location.CompanyId == companyId && location.Id == x.LocationCatalogId &&
                            (allowedLocations == null || allowedLocations.Contains(location.Id)))
                            .Select(location => db.CompanyUnits.Where(unit => unit.CompanyId == companyId && unit.Id == location.CompanyUnitId)
                                .Select(unit => unit.Name).FirstOrDefault()).FirstOrDefault(),
                    x.StartedOn, x.IsActive
                })
                .ToListAsync(ct);
            return Results.Ok(new { items, total, page = safePage, pageSize = safeSize });
        }).AllowAnonymous().RequireRateLimiting("customer-api");
    }

    private static async Task<(CompanyApiToken? Token, string? Role, IResult? Error)> AuthenticateAsync(HttpContext context, AppDbContext db,
        string scope, CancellationToken ct)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer etr_live_", StringComparison.Ordinal))
            return (null, null, Results.Unauthorized());
        var raw = header["Bearer ".Length..].Trim();
        if (raw.Length is < 40 or > 80) return (null, null, Results.Unauthorized());
        var hash = Hash(raw);
        var token = await db.CompanyApiTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        var now = DateTimeOffset.UtcNow;
        if (token is null || token.RevokedAt is not null || (token.ExpiresAt is DateTimeOffset expiry && expiry <= now))
            return (null, null, Results.Unauthorized());
        string[] scopes;
        try { scopes = JsonSerializer.Deserialize<string[]>(token.ScopesJson) ?? []; }
        catch (JsonException) { return (null, null, Results.Unauthorized()); }
        if (!scopes.Contains(scope, StringComparer.Ordinal)) return (null, null, Results.Forbid());

        var membership = await (from member in db.CompanyMemberships.AsNoTracking()
            join company in db.Companies.AsNoTracking() on member.CompanyId equals company.Id
            join user in db.Users.AsNoTracking() on member.UserId equals user.Id
            where member.CompanyId == token.CompanyId && member.UserId == token.UserId && company.IsActive &&
                (!user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd <= now)
            select member.RoleKey).SingleOrDefaultAsync(ct);
        if (membership is null) return (null, null, Results.Unauthorized());
        var permissions = await CompanyRoleAuthorization.ResolveAsync(db, token.CompanyId, membership, ct);
        if (!permissions.Valid || !IsScopeAllowed(scope, permissions.PermissionKeys))
            return (null, null, Results.Unauthorized());
        if (!await TenantPlanFeatures.IsEnabledAsync(token.CompanyId, TenantPlanFeatures.CustomerApi, db, ct))
            return (null, null, TenantPlanFeatures.FeatureUnavailable(TenantPlanFeatures.CustomerApi));
        token.LastUsedAt = now;
        await db.SaveChangesAsync(ct);
        return (token, membership, null);
    }

    private static (int Page, int PageSize) NormalizePage(int page, int pageSize) =>
        (Math.Max(page, 1), Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 100));

    private static DateTimeOffset DatabaseTimestampNow()
    {
        var utcNow = DateTimeOffset.UtcNow;
        return new DateTimeOffset(utcNow.Ticks - utcNow.Ticks % 10, TimeSpan.Zero);
    }

    private static bool CanReadSensitive(string? role) => role is "owner" or "admin";
    private static bool IsScopeAllowed(string scope, IEnumerable<string> permissions) => scope switch
    {
        "assets:read" or "accessories:read" => permissions.Contains(CompanyRoleAuthorization.AssetsRead, StringComparer.Ordinal),
        "assets:write" => permissions.Contains(CompanyRoleAuthorization.AssetsWrite, StringComparer.Ordinal),
        "people:read" => permissions.Contains(CompanyRoleAuthorization.PeopleRead, StringComparer.Ordinal),
        _ => false
    };
    private static string[] ParseOptions(string? json)
    {
        try { return string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private static string Hash(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret))).ToLowerInvariant();
    private static bool TryIdentityScope(ClaimsPrincipal user, out Guid companyId, out Guid userId)
    {
        companyId = Guid.Empty;
        userId = Guid.Empty;
        return Guid.TryParse(user.FindFirstValue("company_id"), out companyId) &&
            Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
    private static bool TryIdentityScope(ClaimsPrincipal user, out Guid companyId)
    {
        companyId = Guid.Empty;
        return Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
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
