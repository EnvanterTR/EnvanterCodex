using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AccessoryPurchaseOrderAttachmentEndpoints
{
    private const long MaxBytes = 10 * 1024 * 1024;
    private const int MaxFiles = 30;

    public static RouteGroupBuilder MapAccessoryPurchaseOrderAttachmentEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/accessory-orders/{id:guid}/attachments", async (Guid id, ClaimsPrincipal user,
            AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompany(user, out var companyId)) return Results.Forbid();
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            if (!await PurchaseOrderLocationAccess.CanAccessAccessoryOrderAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            var rows = await db.AccessoryPurchaseOrderAttachments.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.OriginalFileName, x.ContentType, x.Length, x.CreatedAt,
                    x.UploadedByUserId,
                    uploadedBy = db.Users.Where(actor => actor.Id == x.UploadedByUserId).Select(actor => actor.DisplayName).FirstOrDefault() })
                .ToListAsync(ct);
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, ct);
            var hiddenActorIds = await EmployeeAccessIdentityScope.HiddenUserIdsAsync(db, companyId, employeeScope,
                rows.Select(row => row.UploadedByUserId), ct);
            var items = rows.Select(row => new
            {
                row.Id, row.OriginalFileName, row.ContentType, row.Length, row.CreatedAt,
                uploadedBy = row.UploadedByUserId is Guid uploaderId && hiddenActorIds.Contains(uploaderId) ? null : row.uploadedBy
            }).ToList();
            return Results.Ok(new { items, maxFileCount = MaxFiles, maxFileSizeBytes = MaxBytes });
        }).RequireAuthorization("asset.read");

        api.MapPost("/accessory-orders/{id:guid}/attachments", async (Guid id, HttpRequest request, ClaimsPrincipal user,
            AppDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!TryCompany(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
                return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            if (!request.HasFormContentType) return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
            var form = await request.ReadFormAsync(ct);
            var upload = form.Files.GetFile("file");
            if (upload is null || upload.Length is < 1 or > MaxBytes)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Sipariş eki 1 bayt ile 10 MiB arasında olmalıdır."] });
            await using var buffer = new MemoryStream((int)upload.Length);
            await upload.CopyToAsync(buffer, ct);
            var bytes = buffer.ToArray();
            var detected = Detect(bytes);
            if (detected is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Yalnızca gerçek PDF, PNG, JPEG veya WebP dosyaları yüklenebilir."] });

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var order = await db.AccessoryPurchaseOrders.FromSqlInterpolated(
                    $"SELECT * FROM \"AccessoryPurchaseOrders\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            if (!await PurchaseOrderLocationAccess.CanAccessAccessoryOrderAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            if (await db.AccessoryPurchaseOrderAttachments.CountAsync(x => x.CompanyId == companyId && x.PurchaseOrderId == id, ct) >= MaxFiles)
                return Results.Conflict(new { message = "Bir siparişe en fazla 30 dosya eklenebilir." });

            var attachmentId = Guid.NewGuid();
            var storageName = $"{attachmentId:N}.{detected.Value.Extension}";
            var originalName = CleanName(upload.FileName, detected.Value.Extension);
            var directory = AttachmentDirectory(configuration, companyId, id);
            var path = Path.Combine(directory, storageName);
            if (!CompanyMediaPathGuard.IsSafePath(configuration, companyId, path))
                return Results.Conflict(new { message = "Dosya alanı güvenli biçimde kullanılamıyor. Şirket yöneticinize başvurun." });
            Directory.CreateDirectory(directory);
            var attachment = new AccessoryPurchaseOrderAttachment { Id = attachmentId, CompanyId = companyId, PurchaseOrderId = id,
                UploadedByUserId = actorId, OriginalFileName = originalName, StorageFileName = storageName,
                ContentType = detected.Value.ContentType, Length = bytes.LongLength };
            try
            {
                await System.IO.File.WriteAllBytesAsync(path, bytes, ct);
                db.AccessoryPurchaseOrderAttachments.Add(attachment);
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AccessoryPurchaseOrder",
                    TargetId = id, Action = "AttachmentAdded", Details = $"Aksesuar sipariş eki eklendi ({detected.Value.ContentType}, {bytes.LongLength} bayt)." });
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
                catch (IOException) { } catch (UnauthorizedAccessException) { }
                throw;
            }
            return Results.Created($"/api/accessory-orders/{id}/attachments/{attachmentId}/file", new
            { attachment.Id, attachment.OriginalFileName, attachment.ContentType, attachment.Length, attachment.CreatedAt });
        }).RequireAuthorization("asset.write").WithMetadata(new RequestSizeLimitAttribute(MaxBytes + 65_536));

        api.MapGet("/accessory-orders/{id:guid}/attachments/{attachmentId:guid}/file", async (Guid id, Guid attachmentId,
            ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!TryCompany(user, out var companyId)) return Results.Forbid();
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            if (!await PurchaseOrderLocationAccess.CanAccessAccessoryOrderAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            var item = await db.AccessoryPurchaseOrderAttachments.AsNoTracking().SingleOrDefaultAsync(
                x => x.CompanyId == companyId && x.PurchaseOrderId == id && x.Id == attachmentId, ct);
            if (item is null || !IsSafeStorageName(item.StorageFileName)) return Results.NotFound();
            var path = Path.Combine(AttachmentDirectory(configuration, companyId, id), item.StorageFileName);
            if (!CompanyMediaPathGuard.IsSafePath(configuration, companyId, path) || !System.IO.File.Exists(path)) return Results.NotFound();
            return Results.File(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), item.ContentType,
                item.OriginalFileName, enableRangeProcessing: false);
        }).RequireAuthorization("asset.read");

        api.MapDelete("/accessory-orders/{id:guid}/attachments/{attachmentId:guid}", async (Guid id, Guid attachmentId,
            ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!TryCompany(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
                return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            if (!await PurchaseOrderLocationAccess.CanAccessAccessoryOrderAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            var item = await db.AccessoryPurchaseOrderAttachments.SingleOrDefaultAsync(
                x => x.CompanyId == companyId && x.PurchaseOrderId == id && x.Id == attachmentId, ct);
            if (item is null) return Results.NotFound();
            if (!IsSafeStorageName(item.StorageFileName)) return Results.NotFound();
            var path = Path.Combine(AttachmentDirectory(configuration, companyId, id), item.StorageFileName);
            if (!CompanyMediaPathGuard.IsSafePath(configuration, companyId, path)) return Results.NotFound();
            db.AccessoryPurchaseOrderAttachments.Remove(item);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AccessoryPurchaseOrder",
                TargetId = id, Action = "AttachmentRemoved", Details = $"Aksesuar sipariş eki kaldırıldı ({item.ContentType}, {item.Length} bayt)." });
            await db.SaveChangesAsync(ct);
            try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
            return Results.NoContent();
        }).RequireAuthorization("asset.write");
        return api;
    }

    private static bool TryCompany(ClaimsPrincipal user, out Guid companyId) =>
        Guid.TryParse(user.FindFirstValue("company_id"), out companyId);

    private static string AttachmentDirectory(IConfiguration configuration, Guid companyId, Guid orderId) =>
        Path.Combine(configuration["Storage:CompanyMediaPath"] ?? "/var/lib/envantertr/company-media",
            companyId.ToString("N"), "accessory-purchase-orders", orderId.ToString("N"), "attachments");

    private static string CleanName(string? fileName, string extension)
    {
        var baseName = Path.GetFileNameWithoutExtension(Path.GetFileName((fileName ?? "dosya").Replace('\\', '/')));
        var filtered = new string(baseName.Where(c => !char.IsControl(c) && c is not '/' and not '\\').ToArray()).Trim();
        if (filtered.Length > 175) filtered = filtered[..175];
        return $"{(string.IsNullOrWhiteSpace(filtered) ? "dosya" : filtered)}.{extension}";
    }

    private static bool IsSafeStorageName(string name) =>
        Regex.IsMatch(name, "^[a-fA-F0-9]{32}\\.(pdf|png|jpg|webp)$", RegexOptions.CultureInvariant);

    private static (string ContentType, string Extension)? Detect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 5 && bytes[..5].SequenceEqual("%PDF-"u8)) return ("application/pdf", "pdf");
        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ("image/png", "png");
        if (bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff) return ("image/jpeg", "jpg");
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8)) return ("image/webp", "webp");
        return null;
    }
}
