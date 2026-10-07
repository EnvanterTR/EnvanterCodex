using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class SoftwareLicenseImportEndpoints
{
    private const int MaxBytes = 16 * 1024 * 1024;
    private const int MaxRows = 5000;
    private static readonly string[] Header = ["Lisans", "Yayıncı", "Ürün sürümü", "Tür", "Tedarikçi", "Koltuk", "Birim maliyeti", "Para birimi", "Alış tarihi", "Bitiş tarihi", "Otomatik yenileme", "Notlar"];
    private static readonly string[] LicenseTypes = ["Subscription", "Perpetual", "Device", "Site", "Trial", "Other"];

    public static RouteGroupBuilder MapSoftwareLicenseImportEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/software-licenses/import/preview.csv", (HttpContext c, AppDbContext db, CancellationToken ct) => Import(c, db, false, ct))
            .RequireAuthorization("asset.write").WithMetadata(new RequestSizeLimitAttribute(MaxBytes));
        api.MapPost("/software-licenses/import.csv", (HttpContext c, AppDbContext db, CancellationToken ct) => Import(c, db, true, ct))
            .RequireAuthorization("asset.write").WithMetadata(new RequestSizeLimitAttribute(MaxBytes));
        return api;
    }

    private static async Task<IResult> Import(HttpContext context, AppDbContext db, bool commit, CancellationToken ct)
    {
        if (!Guid.TryParse(context.User.FindFirstValue("company_id"), out var companyId) || !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
        if (context.Request.ContentType?.StartsWith("text/csv", StringComparison.OrdinalIgnoreCase) != true) return Results.StatusCode(415);
        if (context.Request.ContentLength > MaxBytes) return Results.StatusCode(413);
        using var buffer = new MemoryStream(); var chunk = new byte[65536];
        while (true) { var n = await context.Request.Body.ReadAsync(chunk.AsMemory(), ct); if (n == 0) break; if (buffer.Length + n > MaxBytes) return Results.StatusCode(413); await buffer.WriteAsync(chunk.AsMemory(0, n), ct); }
        string text;
        try { text = new UTF8Encoding(false, true).GetString(buffer.ToArray()).TrimStart('\uFEFF'); }
        catch (DecoderFallbackException) { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "CSV UTF-8 biçiminde olmalıdır." } }); }
        List<List<string>> rows;
        try { rows = Parse(text); } catch (FormatException) { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "CSV tırnak veya ayraç biçimi bozuk ya da satır sınırı aşıldı." } }); }
        if (rows.Count is < 2 || rows.Count > MaxRows + 1 || !rows[0].SequenceEqual(Header, StringComparer.OrdinalIgnoreCase))
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { $"Şablon başlığı ve en fazla {MaxRows:N0} lisans satırı kullanın." } });
        var errors = new List<string>(); var licenses = new List<SoftwareLicense>(rows.Count - 1); var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (r, i) in rows.Skip(1).Select((r, i) => (r, i)))
        {
            var line = i + 2; if (r.Count != Header.Length) { Error(errors, $"{line}. satırda {Header.Length} sütun olmalıdır."); continue; }
            string? C(int n) => string.IsNullOrWhiteSpace(r[n]) ? null : r[n].Trim();
            var name = r[0].Trim(); var publisher = C(1); var version = C(2); var type = r[3].Trim(); var supplier = C(4);
            if (name.Length is 0 or > 200) Error(errors, $"{line}. satırdaki lisans adı boş veya 200 karakterden uzun.");
            if (!LicenseTypes.Contains(type, StringComparer.Ordinal)) Error(errors, $"{line}. satırdaki tür geçersiz; Subscription, Perpetual, Device, Site, Trial veya Other kullanın.");
            if (!int.TryParse(r[5], NumberStyles.None, CultureInfo.InvariantCulture, out var seats) || seats is < 1 or > 100000) Error(errors, $"{line}. satırdaki koltuk sayısı 1–100.000 arasında tam sayı olmalıdır.");
            decimal? cost = null; if (!string.IsNullOrWhiteSpace(r[6])) { if (!decimal.TryParse(r[6], NumberStyles.Number, CultureInfo.InvariantCulture, out var v) || v is < 0 or > 9999999999999999.99m) Error(errors, $"{line}. satırdaki maliyet geçersiz."); else cost = v; }
            var currency = r[7].Trim().ToUpperInvariant(); if (currency.Length != 3 || !currency.All(char.IsAsciiLetter)) Error(errors, $"{line}. satırdaki para birimi üç harfli olmalıdır.");
            DateOnly? purchase = Date(8, "alış tarihi"); DateOnly? expiry = Date(9, "bitiş tarihi");
            if (purchase is not null && expiry is not null && expiry < purchase) Error(errors, $"{line}. satırdaki bitiş tarihi alış tarihinden önce olamaz.");
            bool autoRenew; if (Bool(10, out autoRenew)) { } else { autoRenew = false; Error(errors, $"{line}. satırdaki otomatik yenileme evet/hayır olmalıdır."); }
            if (publisher?.Length > 160 || version?.Length > 120 || supplier?.Length > 200 || C(11)?.Length > 2000) Error(errors, $"{line}. satırdaki metin alanlarından biri uzunluk sınırını aşıyor.");
            var key = $"{name}\u001f{publisher}\u001f{version}"; if (!keys.Add(key)) Error(errors, $"{line}. satırdaki lisans adı/yayıncı/sürüm bileşimi dosyada yineleniyor.");
            licenses.Add(new SoftwareLicense { CompanyId = companyId, Name = name, Publisher = publisher, ProductVersion = version, LicenseType = type,
                Supplier = supplier, Seats = seats, UnitCost = cost, Currency = currency, PurchaseDate = purchase, ExpirationDate = expiry, AutoRenew = autoRenew, Notes = C(11) });

            DateOnly? Date(int column, string label) { if (string.IsNullOrWhiteSpace(r[column])) return null; if (DateOnly.TryParseExact(r[column].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return d; Error(errors, $"{line}. satırdaki {label} yyyy-AA-gg biçiminde olmalıdır."); return null; }
            bool Bool(int column, out bool value) { if (r[column].Equals("true", StringComparison.OrdinalIgnoreCase) || r[column].Equals("evet", StringComparison.OrdinalIgnoreCase)) { value = true; return true; } if (r[column].Equals("false", StringComparison.OrdinalIgnoreCase) || r[column].Equals("hayır", StringComparison.OrdinalIgnoreCase) || r[column].Equals("hayir", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(r[column])) { value = false; return true; } value = false; return false; }
        }
        if (errors.Count > 0) return Results.UnprocessableEntity(new { isValid = false, rowCount = rows.Count - 1, errors = errors.Distinct().Take(30), message = "CSV'de düzeltilmesi gereken alanlar bulundu. Hiçbir lisans eklenmedi." });
        var names = licenses.Select(x => x.Name.ToUpper()).Distinct().ToArray();
        var existing = await db.SoftwareLicenses.AsNoTracking().Where(x => x.CompanyId == companyId && names.Contains(x.Name.ToUpper())).Select(x => new { x.Name, x.Publisher, x.ProductVersion }).ToListAsync(ct);
        bool Conflict(IEnumerable<(string Name, string? Publisher, string? Version)> values) => values.Any(x => licenses.Any(y => string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Publisher, y.Publisher, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Version, y.ProductVersion, StringComparison.OrdinalIgnoreCase)));
        if (Conflict(existing.Select(x => (x.Name, x.Publisher, x.ProductVersion)))) return Results.Conflict(new { isValid = false, errors = new[] { "Şirkette aynı lisans adı/yayıncı/sürüm bileşimi bulundu; tekrar eklenmedi." } });
        if (!commit) return Results.Ok(new { isValid = true, rowCount = licenses.Count, errors = Array.Empty<string>(), message = $"{licenses.Count:N0} yeni lisans doğrulandı. Önizleme kayıt eklemedi." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var latest = await db.SoftwareLicenses.AsNoTracking().Where(x => x.CompanyId == companyId && names.Contains(x.Name.ToUpper())).Select(x => new { x.Name, x.Publisher, x.ProductVersion }).ToListAsync(ct);
        if (Conflict(latest.Select(x => (x.Name, x.Publisher, x.ProductVersion)))) return Results.Conflict(new { message = "Önizlemeden sonra aynı lisans eklendi. Hiçbir kayıt aktarılmadı; önizlemeyi yenileyin." });
        var suppliers = await db.SupplierCatalogEntries.AsNoTracking().Where(x => x.CompanyId == companyId).Select(x => new { x.Id, x.NormalizedName }).ToListAsync(ct);
        var supplierMap = suppliers.GroupBy(x => x.NormalizedName, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Select(y => y.Id).ToArray(), StringComparer.OrdinalIgnoreCase);
        foreach (var license in licenses) { var key = license.Supplier?.Normalize(NormalizationForm.FormKC).ToUpperInvariant(); if (key is not null && supplierMap.TryGetValue(key, out var ids) && ids.Length == 1) license.SupplierId = ids[0]; db.SoftwareLicenses.Add(license); db.SoftwareLicenseEvents.Add(new SoftwareLicenseEvent { CompanyId = companyId, LicenseId = license.Id, ActorUserId = actorId, EventType = "Created", Details = "CSV içe aktarımı ile lisans kaydı oluşturuldu." }); }
        db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "SoftwareLicense", Action = "CsvImported", Details = $"CSV dosyasından {licenses.Count} lisans içe aktarıldı." });
        try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); } catch (DbUpdateException) { await tx.RollbackAsync(ct); return Results.Conflict(new { message = "CSV aktarılmadı; bir kayıt çakıştı. Önizlemeyi yenileyip tekrar deneyin." }); }
        return Results.Ok(new { imported = licenses.Count, message = $"{licenses.Count:N0} lisans ve denetim geçmişi eklendi." });
    }

    private static void Error(List<string> errors, string message) { if (errors.Count < 100) errors.Add(message); }
    private static List<List<string>> Parse(string text)
    {
        var rows = new List<List<string>>(); var row = new List<string>(); var cell = new StringBuilder(); bool quoted = false, afterQuote = false;
        for (int i = 0; i < text.Length; i++) { var ch = text[i]; if (quoted) { if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; } else if (ch == '"') { quoted = false; afterQuote = true; } else cell.Append(ch); continue; } if (afterQuote && ch != ',' && ch is not ('\r' or '\n')) throw new FormatException(); if (ch == '"') { if (cell.Length != 0) throw new FormatException(); quoted = true; } else if (ch == ',') { row.Add(cell.ToString()); cell.Clear(); afterQuote = false; } else if (ch is '\r' or '\n') { if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++; row.Add(cell.ToString()); cell.Clear(); afterQuote = false; if (row.Any(x => x.Length > 0)) { rows.Add(row); if (rows.Count > MaxRows + 1) throw new FormatException(); } row = []; } else { cell.Append(ch); afterQuote = false; } }
        if (quoted) throw new FormatException(); if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); if (row.Any(x => x.Length > 0)) rows.Add(row); } return rows;
    }
}
