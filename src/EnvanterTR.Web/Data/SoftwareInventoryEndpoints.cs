using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class SoftwareInventoryEndpoints
{
    public static RouteGroupBuilder MapSoftwareInventoryEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/assets/{id:guid}/installed-software", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Guid.TryParse(user.FindFirstValue("company_id"), out var companyId)) return Results.Forbid();
            var asset = await db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id && x.DeletedAt == null)
                .Select(x => new { x.Id, x.AssetTag, x.Name }).SingleOrDefaultAsync(ct);
            if (asset is null) return Results.NotFound();
            var items = await db.AssetInstalledSoftware.AsNoTracking().Where(x => x.CompanyId == companyId && x.AssetId == id && x.IsPresent)
                .OrderBy(x => x.Name).ThenBy(x => x.Publisher).ThenBy(x => x.Version)
                .Select(x => new { x.Id, x.Name, x.Publisher, x.Version, x.Architecture, x.InstallDate, x.Source, x.FirstSeenAt, x.LastSeenAt })
                .Take(5000).ToListAsync(ct);
            var lastSeen = await db.AssetInstalledSoftware.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.AssetId == id).Select(x => (DateTimeOffset?)x.LastSeenAt)
                .MaxAsync(ct);
            return Results.Ok(new { asset, items, total = items.Count, lastSeen });
        }).RequireAuthorization("asset.read");
        return api;
    }

    public static async Task<IResult?> ReplaceSnapshotAsync(AppDbContext db, Guid companyId, Guid assetId, Guid actorId,
        string source, bool isComplete, SoftwareInventoryItemRequest[]? requestItems, CancellationToken ct)
    {
        if (!isComplete) return Results.ValidationProblem(new Dictionary<string, string[]> { ["isComplete"] = ["Eksik tarama tam envanter olarak kaydedilemez."] });
        if (requestItems is null || requestItems.Length > 3000)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Tam tarama 0–3.000 uygulama kaydı içermelidir."] });
        if (string.IsNullOrWhiteSpace(source) || source.Length > 40 || source.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["source"] = ["Kaynak 1–40 karakterli harf, rakam, nokta, tire veya alt çizgiden oluşmalıdır."] });

        var normalized = new List<(string Fingerprint, string Name, string? Publisher, string? Version, string? Architecture, DateOnly? InstallDate)>(requestItems.Length);
        foreach (var item in requestItems)
        {
            var name = item.Name?.Trim();
            var publisher = Clean(item.Publisher);
            var version = Clean(item.Version);
            var architecture = Clean(item.Architecture);
            if (string.IsNullOrWhiteSpace(name) || name.Length > 200 || publisher?.Length > 160 || version?.Length > 120 || architecture?.Length > 32)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Her uygulama adı zorunludur; ad/yayıncı/sürüm/mimari alanı uzunluk sınırını aşıyor."] });
            var fingerprintInput = string.Join('\u001f', name.ToUpperInvariant(), publisher?.ToUpperInvariant() ?? "", version?.ToUpperInvariant() ?? "", architecture?.ToUpperInvariant() ?? "");
            var fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintInput)));
            normalized.Add((fingerprint, name, publisher, version, architecture, item.InstallDate));
        }
        if (normalized.Select(x => x.Fingerprint).Distinct(StringComparer.Ordinal).Count() != normalized.Count)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Aynı uygulama kimliği tarama içinde birden fazla kez bulunuyor."] });

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var existing = await db.AssetInstalledSoftware.Where(x => x.CompanyId == companyId && x.AssetId == assetId)
            .ToDictionaryAsync(x => x.Fingerprint, StringComparer.Ordinal, ct);
        var now = DateTimeOffset.UtcNow;
        foreach (var row in existing.Values) row.IsPresent = false;
        foreach (var item in normalized)
        {
            if (existing.TryGetValue(item.Fingerprint, out var row))
            {
                row.Name = item.Name; row.Publisher = item.Publisher; row.Version = item.Version; row.Architecture = item.Architecture;
                row.InstallDate = item.InstallDate; row.Source = source; row.LastSeenAt = now; row.IsPresent = true;
            }
            else
            {
                db.AssetInstalledSoftware.Add(new AssetInstalledSoftware { CompanyId = companyId, AssetId = assetId,
                    Fingerprint = item.Fingerprint, Name = item.Name, Publisher = item.Publisher, Version = item.Version,
                    Architecture = item.Architecture, InstallDate = item.InstallDate, Source = source, FirstSeenAt = now, LastSeenAt = now });
            }
        }
        db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Asset", TargetId = assetId,
            Action = "software-inventory.synced", Details = $"Tam yazılım envanteri eşitlendi; {normalized.Count} uygulama mevcut, kaynak: {source}." });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Ok(new { assetId, itemCount = normalized.Count, lastSeen = now });
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed record SoftwareInventorySnapshotRequest(bool IsComplete, string Source, SoftwareInventoryItemRequest[]? Items);
    public sealed record SoftwareInventoryItemRequest(string? Name, string? Publisher, string? Version, string? Architecture, DateOnly? InstallDate);
}
