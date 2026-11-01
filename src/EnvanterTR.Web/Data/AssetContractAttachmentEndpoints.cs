using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static partial class AssetContractAttachmentEndpoints
{
    private const long MaxBytes = 10 * 1024 * 1024;
    private const int MaxFiles = 30;

    public static RouteGroupBuilder MapAssetContractAttachmentEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/contracts/{id:guid}/attachments", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, ct);
            if (!await AssetContractLocationAccess.CanAccessAsync(db, companyId, id, employeeScope, ct)) return Results.NotFound();
            var files = await db.AssetContractAttachments.AsNoTracking().Where(x => x.CompanyId == companyId && x.ContractId == id)
                .OrderByDescending(x => x.CreatedAt).Select(x => new
                {
                    x.Id, x.OriginalFileName, x.ContentType, x.Length, x.Sha256, x.CreatedAt,
                    fileUrl = $"/api/contracts/{id}/attachments/{x.Id}/file"
                }).ToListAsync(ct);
            return Results.Ok(new { items = files, maxFileCount = MaxFiles, maxFileSizeBytes = MaxBytes });
        }).RequireAuthorization("asset.read");

        api.MapPost("/contracts/{id:guid}/attachments", async (Guid id, HttpRequest request, ClaimsPrincipal user,
            AppDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
                return Results.Forbid();
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, ct);
            if (!request.HasFormContentType) return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
            var form = await request.ReadFormAsync(ct);
            var upload = form.Files.GetFile("file");
            if (upload is null || upload.Length is < 1 or > MaxBytes)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Sözleşme eki 1 bayt ile 10 MiB arasında olmalıdır."] });
            await using var buffer = new MemoryStream((int)upload.Length);
            await upload.CopyToAsync(buffer, ct);
            var bytes = buffer.ToArray();
            var detected = Detect(bytes);
            if (detected is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Yalnızca gerçek PDF, PNG, JPEG veya WebP dosyaları yüklenebilir."] });

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var contract = await db.AssetContracts.FromSqlInterpolated(
                    $"SELECT * FROM \"AssetContracts\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (contract is null) return Results.NotFound();
            if (!await AssetContractLocationAccess.CanAccessAsync(db, companyId, id, employeeScope, ct)) return Results.NotFound();
            if (await db.AssetContractAttachments.CountAsync(x => x.CompanyId == companyId && x.ContractId == id, ct) >= MaxFiles)
                return Results.Conflict(new { message = "Bir sözleşmeye en fazla 30 dosya eklenebilir." });

            var attachmentId = Guid.NewGuid();
            var storageName = $"{attachmentId:N}.{detected.Value.Extension}";
            var originalName = CleanName(upload.FileName, detected.Value.Extension);
            var directory = AttachmentDirectory(configuration, companyId, id);
            var path = Path.Combine(directory, storageName);
            if (!CompanyMediaPathGuard.IsSafePath(configuration, companyId, path))
                return Results.Conflict(new { message = "Dosya alanı güvenli biçimde kullanılamıyor. Şirket yöneticinize başvurun." });
            Directory.CreateDirectory(directory);
            var attachment = new AssetContractAttachment
            {
                Id = attachmentId, CompanyId = companyId, ContractId = id, UploadedByUserId = actorId,
                OriginalFileName = originalName, StorageFileName = storageName, ContentType = detected.Value.ContentType,
                Length = bytes.LongLength, Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()
            };
            try
            {
                await System.IO.File.WriteAllBytesAsync(path, bytes, ct);
                db.AssetContractAttachments.Add(attachment);
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetContract",
                    TargetId = id, Action = "contract.attachment-added", Details = $"Sözleşme eki eklendi ({detected.Value.ContentType}, {bytes.LongLength} bayt)." });
                contract.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
                catch (IOException) { } catch (UnauthorizedAccessException) { }
                throw;
            }
            return Results.Created($"/api/contracts/{id}/attachments/{attachmentId}/file", new
            { attachment.Id, attachment.OriginalFileName, attachment.ContentType, attachment.Length, attachment.Sha256, attachment.CreatedAt });
        }).RequireAuthorization("asset.write").WithMetadata(new RequestSizeLimitAttribute(MaxBytes + 65_536));

        api.MapGet("/contracts/{id:guid}/attachments/{attachmentId:guid}/file", async (Guid id, Guid attachmentId,
            ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, ct);
            if (!await AssetContractLocationAccess.CanAccessAsync(db, companyId, id, employeeScope, ct)) return Results.NotFound();
            var item = await db.AssetContractAttachments.AsNoTracking().SingleOrDefaultAsync(
                x => x.CompanyId == companyId && x.ContractId == id && x.Id == attachmentId, ct);
            if (item is null || !SafeStorageName(item.StorageFileName)) return Results.NotFound();
            var path = Path.Combine(AttachmentDirectory(configuration, companyId, id), item.StorageFileName);
            if (!CompanyMediaPathGuard.IsSafePath(configuration, companyId, path) || !System.IO.File.Exists(path)) return Results.NotFound();
            var bytes = await System.IO.File.ReadAllBytesAsync(path, ct);
            if (bytes.LongLength != item.Length || !ValidSha256(item.Sha256) ||
                !CryptographicOperations.FixedTimeEquals(SHA256.HashData(bytes), Convert.FromHexString(item.Sha256!)))
                return Results.Problem("Sözleşme dosyası bütünlük denetimini geçemedi.", statusCode: StatusCodes.Status500InternalServerError);
            return Results.File(bytes, item.ContentType, item.OriginalFileName);
        }).RequireAuthorization("asset.read");

        api.MapDelete("/contracts/{id:guid}/attachments/{attachmentId:guid}", async (Guid id, Guid attachmentId,
            ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
                return Results.Forbid();
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, ct);
            if (!await AssetContractLocationAccess.CanAccessAsync(db, companyId, id, employeeScope, ct)) return Results.NotFound();
            var item = await db.AssetContractAttachments.SingleOrDefaultAsync(
                x => x.CompanyId == companyId && x.ContractId == id && x.Id == attachmentId, ct);
            if (item is null) return Results.NotFound();
            if (!SafeStorageName(item.StorageFileName)) return Results.NotFound();
            var path = Path.Combine(AttachmentDirectory(configuration, companyId, id), item.StorageFileName);
            if (!CompanyMediaPathGuard.IsSafePath(configuration, companyId, path)) return Results.NotFound();
            db.AssetContractAttachments.Remove(item);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetContract",
                TargetId = id, Action = "contract.attachment-removed", Details = $"Sözleşme eki kaldırıldı ({item.ContentType}, {item.Length} bayt)." });
            await db.SaveChangesAsync(ct);
            try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
            return Results.NoContent();
        }).RequireAuthorization("asset.write");
        return api;
    }

    private static bool Company(ClaimsPrincipal user, out Guid companyId) => Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
    private static string AttachmentDirectory(IConfiguration configuration, Guid companyId, Guid contractId) =>
        Path.Combine(configuration["Storage:CompanyMediaPath"] ?? "/var/lib/envantertr/company-media",
            companyId.ToString("N"), "contracts", contractId.ToString("N"), "attachments");

    private static bool SafeStorageName(string name) => SafeStorageNameRegex().IsMatch(name);
    private static bool ValidSha256(string? value) => value is { Length: 64 } && value.All(Uri.IsHexDigit);
    [GeneratedRegex("^[0-9a-f]{32}\\.(pdf|png|jpe?g|webp)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SafeStorageNameRegex();

    private static string CleanName(string? fileName, string extension)
    {
        var baseName = Path.GetFileNameWithoutExtension(Path.GetFileName((fileName ?? "dosya").Replace('\\', '/')));
        var filtered = new string(baseName.Where(c => !char.IsControl(c) && c is not '/' and not '\\').ToArray()).Trim();
        if (filtered.Length > 175) filtered = filtered[..175];
        return $"{(string.IsNullOrWhiteSpace(filtered) ? "dosya" : filtered)}.{extension}";
    }

    private static (string Extension, string ContentType)? Detect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 5 && bytes[..5].SequenceEqual("%PDF-"u8)) return ("pdf", "application/pdf");
        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ("png", "image/png");
        if (bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff) return ("jpg", "image/jpeg");
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8)) return ("webp", "image/webp");
        return null;
    }
}
