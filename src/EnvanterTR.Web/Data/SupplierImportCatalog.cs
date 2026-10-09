using System.Text;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

/// <summary>
/// Resolves supplier names from an import against the active tenant's catalog. New entries are
/// only staged when the import is committed, so preview remains read-only.
/// </summary>
internal static class SupplierImportCatalog
{
    internal static string Normalize(string name) => name.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();

    internal static async Task<string[]> FindMissingNamesAsync(AppDbContext db, Guid companyId,
        IEnumerable<string?> supplierNames, CancellationToken ct)
    {
        var candidates = supplierNames.Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .Where(name => name.Length <= 200)
            .GroupBy(Normalize, StringComparer.Ordinal)
            .Select(group => new { Normalized = group.Key, Name = group.First() })
            .ToArray();
        if (candidates.Length == 0) return [];

        var normalizedNames = candidates.Select(x => x.Normalized).ToArray();
        var existing = await db.SupplierCatalogEntries.AsNoTracking()
            .Where(x => x.CompanyId == companyId && normalizedNames.Contains(x.NormalizedName))
            .Select(x => x.NormalizedName).ToListAsync(ct);
        var known = existing.ToHashSet(StringComparer.Ordinal);
        return candidates.Where(x => !known.Contains(x.Normalized)).Select(x => x.Name).Order(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>Stages missing company-scoped suppliers and links the imported records.</summary>
    internal static async Task<int> LinkAndCreateAsync(AppDbContext db, Guid companyId, Guid actorId,
        IEnumerable<(string? Name, Action<Guid> SetSupplierId)> links, CancellationToken ct)
    {
        var entries = (await db.SupplierCatalogEntries
                .Where(x => x.CompanyId == companyId).ToListAsync(ct))
            .GroupBy(x => x.NormalizedName, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
        var created = 0;
        foreach (var (nameValue, setSupplierId) in links)
        {
            if (string.IsNullOrWhiteSpace(nameValue)) continue;
            var name = nameValue.Trim();
            if (name.Length > 200) continue;
            var normalized = Normalize(name);
            if (!entries.TryGetValue(normalized, out var entry))
            {
                entry = new SupplierCatalogEntry { CompanyId = companyId, Name = name, NormalizedName = normalized };
                db.SupplierCatalogEntries.Add(entry);
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                    TargetType = "Supplier", TargetId = entry.Id, Action = "supplier.created.import", Details = name });
                entries.Add(normalized, entry);
                created++;
            }
            setSupplierId(entry.Id);
        }
        return created;
    }
}
