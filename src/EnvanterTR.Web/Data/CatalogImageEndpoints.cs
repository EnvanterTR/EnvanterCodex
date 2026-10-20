using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class CatalogImageEndpoints
{
    private const int MaxBytes = 2 * 1024 * 1024;

    public static RouteGroupBuilder MapCatalogImageEndpoints(this RouteGroupBuilder api)
    {
        MapImage(api, "brands", "logo", (db, companyId, id, ct) => db.AssetBrands
            .Where(x => x.CompanyId == companyId && x.Id == id).Select(x => x.LogoFileName).SingleOrDefaultAsync(ct),
            async (db, companyId, id, fileName, actorId, ct) =>
            {
                var row = await db.AssetBrands.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
                if (row is null) return null;
                var previous = row.LogoFileName; row.LogoFileName = fileName;
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetBrand", TargetId = id, Action = "LogoUpdated", Details = "Marka logosu güncellendi." });
                await db.SaveChangesAsync(ct); return previous;
            }, async (db, companyId, id, actorId, ct) =>
            {
                var row = await db.AssetBrands.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
                if (row is null || row.LogoFileName is null) return null;
                var previous = row.LogoFileName; row.LogoFileName = null;
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetBrand", TargetId = id, Action = "LogoRemoved", Details = "Marka logosu kaldırıldı." });
                await db.SaveChangesAsync(ct); return previous;
            });
        MapImage(api, "suppliers", "logo", (db, companyId, id, ct) => db.SupplierCatalogEntries
            .Where(x => x.CompanyId == companyId && x.Id == id).Select(x => x.LogoFileName).SingleOrDefaultAsync(ct),
            async (db, companyId, id, fileName, actorId, ct) =>
            {
                var row = await db.SupplierCatalogEntries.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
                if (row is null) return null;
                var previous = row.LogoFileName; row.LogoFileName = fileName;
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Supplier", TargetId = id, Action = "LogoUpdated", Details = "Tedarikçi logosu güncellendi." });
                await db.SaveChangesAsync(ct); return previous;
            }, async (db, companyId, id, actorId, ct) =>
            {
                var row = await db.SupplierCatalogEntries.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
                if (row is null || row.LogoFileName is null) return null;
                var previous = row.LogoFileName; row.LogoFileName = null;
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Supplier", TargetId = id, Action = "LogoRemoved", Details = "Tedarikçi logosu kaldırıldı." });
                await db.SaveChangesAsync(ct); return previous;
            });
        MapImage(api, "models", "photo", (db, companyId, id, ct) => db.AssetModels
            .Where(x => x.CompanyId == companyId && x.Id == id).Select(x => x.PhotoFileName).SingleOrDefaultAsync(ct),
            async (db, companyId, id, fileName, actorId, ct) =>
            {
                var row = await db.AssetModels.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
                if (row is null) return null;
                var previous = row.PhotoFileName; row.PhotoFileName = fileName;
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetModel", TargetId = id, Action = "PhotoUpdated", Details = "Model görseli güncellendi." });
                await db.SaveChangesAsync(ct); return previous;
            }, async (db, companyId, id, actorId, ct) =>
            {
                var row = await db.AssetModels.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
                if (row is null || row.PhotoFileName is null) return null;
                var previous = row.PhotoFileName; row.PhotoFileName = null;
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetModel", TargetId = id, Action = "PhotoRemoved", Details = "Model görseli kaldırıldı." });
                await db.SaveChangesAsync(ct); return previous;
            });
        MapImage(api, "accessory-models", "photo", (db, companyId, id, ct) => db.AccessoryModels
            .Where(x => x.CompanyId == companyId && x.Id == id).Select(x => x.PhotoFileName).SingleOrDefaultAsync(ct),
            async (db, companyId, id, fileName, actorId, ct) =>
            {
                var row = await db.AccessoryModels.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
                if (row is null) return null;
                var previous = row.PhotoFileName; row.PhotoFileName = fileName;
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AccessoryModel", TargetId = id, Action = "PhotoUpdated", Details = "Aksesuar modeli görseli güncellendi." });
                await db.SaveChangesAsync(ct); return previous;
            }, async (db, companyId, id, actorId, ct) =>
            {
                var row = await db.AccessoryModels.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
                if (row is null || row.PhotoFileName is null) return null;
                var previous = row.PhotoFileName; row.PhotoFileName = null;
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AccessoryModel", TargetId = id, Action = "PhotoRemoved", Details = "Aksesuar modeli görseli kaldırıldı." });
                await db.SaveChangesAsync(ct); return previous;
            });
        return api;
    }

    private static void MapImage(RouteGroupBuilder api, string kind, string imageKind,
        Func<AppDbContext, Guid, Guid, CancellationToken, Task<string?>> getName,
        Func<AppDbContext, Guid, Guid, string, Guid, CancellationToken, Task<string?>> setName,
        Func<AppDbContext, Guid, Guid, Guid, CancellationToken, Task<string?>> removeName)
    {
        api.MapPost($"/{kind}/{{id:guid}}/{imageKind}", async (Guid id, HttpRequest request, ClaimsPrincipal user,
            AppDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (kind == "suppliers" && await SupplierCatalogEndpoints.IsLocationRestrictedAsync(db, companyId, actorId, ct))
                return Results.Json(new { message = "Tedarikçi kataloğu şirket genelinde paylaşıldığı için bu işlem yalnızca tüm konumlara erişimi olan üyelerce yapılabilir." }, statusCode: StatusCodes.Status403Forbidden);
            if (kind != "suppliers" && await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct) is not null)
                return Results.Forbid();
            if (!request.HasFormContentType) return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
            var upload = (await request.ReadFormAsync(ct)).Files.GetFile("file");
            if (upload is null || upload.Length is < 1 or > MaxBytes)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Görsel 1 bayt ile 2 MiB arasında olmalıdır."] });
            await using var buffer = new MemoryStream((int)upload.Length);
            await upload.CopyToAsync(buffer, ct); var bytes = buffer.ToArray();
            var image = Detect(bytes);
            if (image is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Yalnızca gerçek PNG, JPEG veya WebP görselleri yüklenebilir."] });
            var directory = ImageDirectory(configuration, companyId, kind, id);
            if (HasReparsePoint(configuration, companyId, kind, id)) return Results.Problem("Katalog görseli deposu güvenli bir dizin olmalıdır.", statusCode: 500);
            Directory.CreateDirectory(directory);
            if (HasReparsePoint(configuration, companyId, kind, id)) return Results.Problem("Katalog görseli deposu güvenli bir dizin olmalıdır.", statusCode: 500);
            var fileName = $"{Guid.NewGuid():N}.{image}"; var path = Path.Combine(directory, fileName);
            await System.IO.File.WriteAllBytesAsync(path, bytes, ct);
            string? previous;
            try { previous = await setName(db, companyId, id, fileName, actorId, ct); }
            catch { try { System.IO.File.Delete(path); } catch { } throw; }
            if (previous is null && await getName(db, companyId, id, ct) != fileName)
            { try { System.IO.File.Delete(path); } catch { } return Results.NotFound(); }
            DeleteIfSafe(configuration, companyId, kind, id, previous);
            return Results.Ok(new { imageUrl = $"/api/{kind}/{id}/{imageKind}?v={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" });
        }).RequireAuthorization("asset.write");

        api.MapGet($"/{kind}/{{id:guid}}/{imageKind}", async (Guid id, ClaimsPrincipal user, AppDbContext db,
            IConfiguration configuration, CancellationToken ct) =>
        {
            if (!TryCompany(user, out var companyId)) return Results.Forbid();
            if (kind is "models" or "accessory-models")
            {
                if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var readerId))
                    return Results.Forbid();
                var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, readerId, ct);
                if (allowedLocations is not null)
                {
                    var hasAccessibleItem = kind == "models"
                        ? await db.Assets.AsNoTracking().AnyAsync(asset => asset.CompanyId == companyId && asset.ModelId == id &&
                            asset.DeletedAt == null && asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value), ct)
                        : await db.Accessories.AsNoTracking().AnyAsync(accessory => accessory.CompanyId == companyId && accessory.ModelId == id &&
                            accessory.LocationCatalogId.HasValue && allowedLocations.Contains(accessory.LocationCatalogId.Value), ct);
                    if (!hasAccessibleItem) return Results.NotFound();
                }
            }
            var fileName = await getName(db, companyId, id, ct);
            if (!SafeName(fileName) || HasReparsePoint(configuration, companyId, kind, id)) return Results.NotFound();
            var path = Path.Combine(ImageDirectory(configuration, companyId, kind, id), fileName!);
            if (!System.IO.File.Exists(path)) return Results.NotFound();
            var contentType = Path.GetExtension(fileName) switch { ".png" => "image/png", ".jpg" => "image/jpeg", ".webp" => "image/webp", _ => "application/octet-stream" };
            return Results.File(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), contentType);
        }).RequireAuthorization("asset.read");

        api.MapDelete($"/{kind}/{{id:guid}}/{imageKind}", async (Guid id, ClaimsPrincipal user, AppDbContext db,
            IConfiguration configuration, CancellationToken ct) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (kind == "suppliers" && await SupplierCatalogEndpoints.IsLocationRestrictedAsync(db, companyId, actorId, ct))
                return Results.Json(new { message = "Tedarikçi kataloğu şirket genelinde paylaşıldığı için bu işlem yalnızca tüm konumlara erişimi olan üyelerce yapılabilir." }, statusCode: StatusCodes.Status403Forbidden);
            if (kind != "suppliers" && await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct) is not null)
                return Results.Forbid();
            var previous = await removeName(db, companyId, id, actorId, ct);
            if (previous is null) return Results.NotFound();
            DeleteIfSafe(configuration, companyId, kind, id, previous); return Results.NoContent();
        }).RequireAuthorization("asset.write");
    }

    private static bool TryCompany(ClaimsPrincipal user, out Guid companyId) => Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
    private static bool TryScope(ClaimsPrincipal user, out Guid companyId, out Guid actorId)
    {
        var validCompany = TryCompany(user, out companyId);
        var validActor = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out actorId);
        return validCompany && validActor;
    }
    private static string ImageDirectory(IConfiguration config, Guid companyId, string kind, Guid id) => Path.Combine(config["Storage:CompanyMediaPath"] ?? "/var/lib/envantertr/company-media", companyId.ToString("N"), "catalog", kind, id.ToString("N"));
    private static bool HasReparsePoint(IConfiguration config, Guid companyId, string kind, Guid id)
    {
        var root = Path.GetFullPath(config["Storage:CompanyMediaPath"] ?? "/var/lib/envantertr/company-media");
        var company = Path.GetFullPath(Path.Combine(root, companyId.ToString("N"))); var directory = Path.GetFullPath(ImageDirectory(config, companyId, kind, id));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!company.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, comparison) || !directory.StartsWith(company.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, comparison)) return true;
        return new[] { company, Path.Combine(company, "catalog"), Path.Combine(company, "catalog", kind), directory }.Any(path => Directory.Exists(path) && (System.IO.File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0);
    }
    // Existing catalog/demo imports use descriptive names such as "catalog-prodesk.png".
    // Keep serving those while rejecting separators and every character that could escape the item folder.
    private static bool SafeName(string? name) => name is not null &&
        Regex.IsMatch(name, "^[A-Za-z0-9][A-Za-z0-9._-]{0,79}\\.(png|jpg|webp)$", RegexOptions.CultureInvariant);
    private static void DeleteIfSafe(IConfiguration config, Guid companyId, string kind, Guid id, string? name)
    {
        if (!SafeName(name) || HasReparsePoint(config, companyId, kind, id)) return;
        try { System.IO.File.Delete(Path.Combine(ImageDirectory(config, companyId, kind, id), name!)); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
    private static string? Detect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "png";
        if (bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff) return "jpg";
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8)) return "webp";
        return null;
    }
}
