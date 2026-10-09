using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AccessoryPhotoEndpoints
{
    private const int MaxBytes = 2 * 1024 * 1024;

    public static RouteGroupBuilder MapAccessoryPhotoEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/accessories/{id:guid}/photo", async (Guid id, HttpRequest request, ClaimsPrincipal user,
            AppDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (!request.HasFormContentType) return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
            var upload = (await request.ReadFormAsync(ct)).Files.GetFile("file");
            if (upload is null || upload.Length is < 1 or > MaxBytes)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Aksesuar fotoğrafı 1 bayt ile 2 MiB arasında olmalıdır."] });
            await using var buffer = new MemoryStream((int)upload.Length);
            await upload.CopyToAsync(buffer, ct);
            var bytes = buffer.ToArray();
            var image = Detect(bytes);
            if (image is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Yalnızca gerçek PNG, JPEG veya WebP görselleri yüklenebilir."] });

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var item = await db.Accessories.FromSqlInterpolated(
                    $"SELECT * FROM \"Accessories\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (item is null) return Results.NotFound();
            var directory = PhotoDirectory(configuration, companyId, id);
            if (HasReparsePoint(configuration, companyId, id))
                return Results.Problem("Aksesuar fotoğrafı deposu güvenli bir dizin olmalıdır.", statusCode: StatusCodes.Status500InternalServerError);
            Directory.CreateDirectory(directory);
            if (HasReparsePoint(configuration, companyId, id))
                return Results.Problem("Aksesuar fotoğrafı deposu güvenli bir dizin olmalıdır.", statusCode: StatusCodes.Status500InternalServerError);
            var fileName = $"{Guid.NewGuid():N}.{image.Value.Extension}";
            var path = Path.Combine(directory, fileName);
            var previousFileName = item.PhotoFileName;
            try
            {
                await System.IO.File.WriteAllBytesAsync(path, bytes, ct);
                item.PhotoFileName = fileName;
                item.UpdatedAt = DateTimeOffset.UtcNow;
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                    TargetType = "Accessory", TargetId = id, Action = "PhotoUpdated",
                    Details = $"Aksesuar fotoğrafı eklendi ({image.Value.ContentType}, {bytes.Length} bayt)." });
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
                catch (IOException) { } catch (UnauthorizedAccessException) { }
                throw;
            }
            DeletePhotoIfSafe(configuration, companyId, id, previousFileName);
            return Results.Ok(new { photoUrl = $"/api/accessories/{id}/photo?v={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" });
        }).RequireAuthorization("asset.write");

        api.MapGet("/accessories/{id:guid}/photo", async (Guid id, ClaimsPrincipal user, AppDbContext db,
            IConfiguration configuration, CancellationToken ct) =>
        {
            if (!TryCompany(user, out var companyId)) return Results.Forbid();
            var fileName = await db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => x.PhotoFileName).SingleOrDefaultAsync(ct);
            if (!IsSafePhotoName(fileName) || HasReparsePoint(configuration, companyId, id)) return Results.NotFound();
            var path = Path.Combine(PhotoDirectory(configuration, companyId, id), fileName!);
            if (!System.IO.File.Exists(path)) return Results.NotFound();
            var contentType = Path.GetExtension(fileName) switch
            { ".png" => "image/png", ".jpg" => "image/jpeg", ".webp" => "image/webp", _ => "application/octet-stream" };
            return Results.File(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), contentType,
                enableRangeProcessing: false);
        }).RequireAuthorization("asset.read");

        api.MapDelete("/accessories/{id:guid}/photo", async (Guid id, ClaimsPrincipal user, AppDbContext db,
            IConfiguration configuration, CancellationToken ct) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var item = await db.Accessories.FromSqlInterpolated(
                    $"SELECT * FROM \"Accessories\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (item is null || item.PhotoFileName is null) return Results.NotFound();
            var previousFileName = item.PhotoFileName;
            item.PhotoFileName = null;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Accessory", TargetId = id, Action = "PhotoRemoved", Details = "Aksesuar fotoğrafı kaldırıldı." });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            DeletePhotoIfSafe(configuration, companyId, id, previousFileName);
            return Results.NoContent();
        }).RequireAuthorization("asset.write");

        return api;
    }

    private static bool TryCompany(ClaimsPrincipal user, out Guid companyId) =>
        Guid.TryParse(user.FindFirstValue("company_id"), out companyId);

    private static bool TryScope(ClaimsPrincipal user, out Guid companyId, out Guid actorId)
    {
        var validCompany = TryCompany(user, out companyId);
        var validActor = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out actorId);
        return validCompany && validActor;
    }

    private static string PhotoDirectory(IConfiguration configuration, Guid companyId, Guid accessoryId) =>
        Path.Combine(configuration["Storage:CompanyMediaPath"] ?? "/var/lib/envantertr/company-media",
            companyId.ToString("N"), "accessories", accessoryId.ToString("N"), "photo");

    private static bool HasReparsePoint(IConfiguration configuration, Guid companyId, Guid accessoryId)
    {
        var root = Path.GetFullPath(configuration["Storage:CompanyMediaPath"] ?? "/var/lib/envantertr/company-media");
        var company = Path.GetFullPath(Path.Combine(root, companyId.ToString("N")));
        var directory = Path.GetFullPath(Path.Combine(company, "accessories", accessoryId.ToString("N"), "photo"));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!company.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, comparison) ||
            !directory.StartsWith(company.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, comparison)) return true;
        var paths = new[] { company, Path.Combine(company, "accessories"), Path.Combine(company, "accessories", accessoryId.ToString("N")), directory };
        return paths.Any(path => Directory.Exists(path) && (System.IO.File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0);
    }

    private static bool IsSafePhotoName(string? fileName) => fileName is not null &&
        Regex.IsMatch(fileName, "^[A-Za-z0-9][A-Za-z0-9._-]{0,79}\\.(png|jpg|webp)$", RegexOptions.CultureInvariant);

    private static void DeletePhotoIfSafe(IConfiguration configuration, Guid companyId, Guid accessoryId, string? fileName)
    {
        if (!IsSafePhotoName(fileName) || HasReparsePoint(configuration, companyId, accessoryId)) return;
        var path = Path.Combine(PhotoDirectory(configuration, companyId, accessoryId), fileName!);
        try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
        catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private static (string ContentType, string Extension)? Detect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ("image/png", "png");
        if (bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff) return ("image/jpeg", "jpg");
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8)) return ("image/webp", "webp");
        return null;
    }
}
