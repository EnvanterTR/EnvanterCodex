using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AccessoryImportEndpoints
{
    private const int MaxBytes = 32 * 1024 * 1024;
    private const int MaxRows = 5000;

    public static RouteGroupBuilder MapAccessoryImportEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/accessories/import/preview.csv", (HttpContext c, AppDbContext db, IAntiforgery a, CancellationToken ct) => Import(c, db, a, false, ct))
            .RequireAuthorization("asset.write").WithMetadata(new RequestSizeLimitAttribute(MaxBytes));
        api.MapPost("/accessories/import/preview.xlsx", (HttpContext c, AppDbContext db, IAntiforgery a, CancellationToken ct) => Import(c, db, a, false, ct))
            .RequireAuthorization("asset.write").WithMetadata(new RequestSizeLimitAttribute(MaxBytes));
        api.MapPost("/accessories/import.csv", (HttpContext c, AppDbContext db, IAntiforgery a, CancellationToken ct) => Import(c, db, a, true, ct))
            .RequireAuthorization("asset.write").WithMetadata(new RequestSizeLimitAttribute(MaxBytes));
        api.MapPost("/accessories/import.xlsx", (HttpContext c, AppDbContext db, IAntiforgery a, CancellationToken ct) => Import(c, db, a, true, ct))
            .RequireAuthorization("asset.write").WithMetadata(new RequestSizeLimitAttribute(MaxBytes));
        return api;
    }

    private static async Task<IResult> Import(HttpContext c, AppDbContext db, IAntiforgery antiforgery, bool commit, CancellationToken ct)
    {
        if (!Guid.TryParse(c.User.FindFirstValue("company_id"), out var companyId) || !Guid.TryParse(c.User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
        try { await antiforgery.ValidateRequestAsync(c); } catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
        var isXlsx = DataExportEndpoints.IsXlsxRequest(c.Request);
        if (!isXlsx && c.Request.ContentType?.StartsWith("text/csv", StringComparison.OrdinalIgnoreCase) != true) return Results.StatusCode(415);
        if (c.Request.ContentLength > MaxBytes) return Results.StatusCode(413);
        using var buffer = new MemoryStream(); var chunk = new byte[65536];
        while (true) { var n = await c.Request.Body.ReadAsync(chunk.AsMemory(), ct); if (n == 0) break; if (buffer.Length + n > MaxBytes) return Results.StatusCode(413); await buffer.WriteAsync(chunk.AsMemory(0, n), ct); }
        List<List<string>> rows;
        try
        {
            if (isXlsx) rows = AssetXlsxImportParser.Parse(buffer, MaxRows + 2);
            else { var text = new UTF8Encoding(false, true).GetString(buffer.ToArray()).TrimStart('\uFEFF'); rows = Parse(text, DetectDelimiter(text)); }
        }
        catch (DecoderFallbackException) { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "CSV UTF-8 biçiminde olmalıdır." } }); }
        catch (FormatException) { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "CSV tırnak veya ayraç biçimi bozuk." } }); }
        catch (Exception exception) when (isXlsx && exception is InvalidDataException or InvalidOperationException or System.Xml.XmlException or OverflowException)
        { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { exception.Message } }); }
        rows = AccessoryCsvImportMapping.Normalize(rows, out var unsupportedColumns);
        if (unsupportedColumns.Length > 0)
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { $"Bu dosyada henüz eşlenmeyen ve dolu veri içeren sütunlar var: {string.Join(", ", unsupportedColumns)}. Veri kaybını önlemek için aktarım durduruldu." } });
        string[] header = ["Aksesuar", "Kategori", "Model", "Ürün no", "Tedarikçi", "Konum", "Toplam", "Minimum", "Birim maliyeti", "Para birimi", "Alış tarihi", "İstek yapılabilir", "Notlar"];
        if (rows.Count is < 2 || rows.Count > MaxRows + 1 || !rows[0].SequenceEqual(header, StringComparer.OrdinalIgnoreCase))
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { $"Şablon başlığı ve en fazla {MaxRows:N0} aksesuar satırı kullanın." } });
        var errors = new List<string>(); var items = new List<Accessory>(rows.Count - 1); var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var itemNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (r, ix) in rows.Skip(1).Select((r, i) => (r, i)))
        {
            var line = ix + 2; if (r.Count != header.Length) { AddError(errors, $"{line}. satırda {header.Length} sütun olmalıdır."); continue; }
            string? Clean(int i) => string.IsNullOrWhiteSpace(r[i]) ? null : r[i].Trim();
            var name = r[0].Trim(); var itemNo = Clean(3); var supplier = Clean(4);
            if (name.Length is 0 or > 200) AddError(errors, $"{line}. satırdaki aksesuar adı boş veya 200 karakterden uzun.");
            if (!int.TryParse(r[6], NumberStyles.None, CultureInfo.InvariantCulture, out var total) || total is < 0 or > 100000) AddError(errors, $"{line}. satırdaki toplam 0–100.000 arasında tam sayı olmalıdır.");
            if (!int.TryParse(r[7], NumberStyles.None, CultureInfo.InvariantCulture, out var minimum) || minimum is < 0 or > 100000) AddError(errors, $"{line}. satırdaki minimum 0–100.000 arasında tam sayı olmalıdır.");
            decimal? cost = null; if (!string.IsNullOrWhiteSpace(r[8])) { if (!decimal.TryParse(r[8], NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || value is < 0 or > 9999999999999999.99m) AddError(errors, $"{line}. satırdaki birim maliyeti geçersiz."); else cost = value; }
            var currency = r[9].Trim().ToUpperInvariant(); if (currency.Length != 3 || !currency.All(char.IsAsciiLetter)) AddError(errors, $"{line}. satırdaki para birimi üç harfli kod olmalıdır.");
            DateOnly? date = null; if (!string.IsNullOrWhiteSpace(r[10])) { if (DateOnly.TryParseExact(r[10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) date = d; else AddError(errors, $"{line}. satırdaki alış tarihi yyyy-AA-gg olmalıdır."); }
            bool requestable; if (r[11].Equals("true", StringComparison.OrdinalIgnoreCase) || r[11].Equals("evet", StringComparison.OrdinalIgnoreCase)) requestable = true; else if (r[11].Equals("false", StringComparison.OrdinalIgnoreCase) || r[11].Equals("hayır", StringComparison.OrdinalIgnoreCase) || r[11].Equals("hayir", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(r[11])) requestable = false; else { requestable = false; AddError(errors, $"{line}. satırdaki istek yapılabilir alanı true/false veya evet/hayır olmalıdır."); }
            var fields = new[] { Clean(1), Clean(2), itemNo, supplier, Clean(5), Clean(12) };
            int[] limits = [120, 120, 120, 200, 160, 2000]; if (fields.Where((v, i) => v?.Length > limits[i]).Any()) AddError(errors, $"{line}. satırdaki metin alanlarından biri uzunluk sınırını aşıyor.");
            if (!names.Add(name)) AddError(errors, $"{line}. satırdaki aksesuar adı dosyada yineleniyor."); if (itemNo is not null && !itemNos.Add(itemNo)) AddError(errors, $"{line}. satırdaki ürün no dosyada yineleniyor.");
            items.Add(new Accessory { CompanyId = companyId, Name = name, Category = fields[0], ModelNumber = fields[1], ItemNumber = itemNo, Supplier = supplier, Location = fields[4], TotalQuantity = total, MinimumQuantity = minimum, UnitCost = cost, Currency = currency, PurchaseDate = date, IsRequestable = requestable, Notes = fields[5] });
        }
        if (errors.Count > 0) return Results.UnprocessableEntity(new { isValid = false, rowCount = rows.Count - 1, errors = errors.Distinct().Take(30), message = "CSV'de düzeltilmesi gereken alanlar bulundu. Hiçbir aksesuar eklenmedi." });
        var upperNames = items.Select(x => x.Name.ToUpper()).Distinct().ToArray(); var upperNos = items.Where(x => x.ItemNumber != null).Select(x => x.ItemNumber!.ToUpper()).Distinct().ToArray();
        var existing = await db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId && (upperNames.Contains(x.Name.ToUpper()) || x.ItemNumber != null && upperNos.Contains(x.ItemNumber.ToUpper()))).Select(x => new { x.Name, x.ItemNumber }).ToListAsync(ct);
        if (existing.Any(x => items.Any(y => string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase) || y.ItemNumber != null && string.Equals(x.ItemNumber, y.ItemNumber, StringComparison.OrdinalIgnoreCase))))
            return Results.Conflict(new { isValid = false, errors = new[] { "Şirkette aynı ad veya ürün no ile aksesuar bulundu. Yinelenen kayıt eklenmedi." } });
        var locationPaths = await AssetEndpoints.GetActiveLocationPathMapAsync(db, companyId, ct);
        var defaultLocation = await AssetEndpoints.GetDefaultLocationPathAsync(db, companyId, ct);
        var defaultLocationApplied = defaultLocation is null ? 0 : items.Count(x => x.Location is null);
        foreach (var item in items)
        {
            item.Location ??= defaultLocation;
            if (item.Location is not null && locationPaths.TryGetValue(item.Location, out var locationCatalogId)) item.LocationCatalogId = locationCatalogId;
        }
        if (!commit) return Results.Ok(new { isValid = true, rowCount = items.Count, defaultLocationApplied, errors = Array.Empty<string>(), message = $"{items.Count:N0} yeni aksesuar doğrulandı. Varsayılan konum {defaultLocationApplied:N0} boş satıra uygulanacak. Önizleme kayıt eklemedi." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var current = await db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId && (upperNames.Contains(x.Name.ToUpper()) || x.ItemNumber != null && upperNos.Contains(x.ItemNumber.ToUpper()))).Select(x => new { x.Name, x.ItemNumber }).ToListAsync(ct);
        if (current.Any(x => items.Any(y => string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase) || y.ItemNumber != null && string.Equals(x.ItemNumber, y.ItemNumber, StringComparison.OrdinalIgnoreCase)))) return Results.Conflict(new { message = "Önizlemeden sonra aynı aksesuar eklendi. Hiçbir kayıt aktarılmadı; önizlemeyi yenileyin." });
        var supplierEntries = await db.SupplierCatalogEntries.AsNoTracking().Where(x => x.CompanyId == companyId).Select(x => new { x.Id, x.NormalizedName }).ToListAsync(ct);
        var suppliers = supplierEntries.GroupBy(x => x.NormalizedName, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Select(y => y.Id).ToArray(), StringComparer.OrdinalIgnoreCase);
        foreach (var item in items) { var key = item.Supplier?.Normalize(NormalizationForm.FormKC).ToUpperInvariant(); if (key is not null && suppliers.TryGetValue(key, out var ids) && ids.Length == 1) item.SupplierId = ids[0]; db.Accessories.Add(item); if (item.TotalQuantity > 0) db.AccessoryMovements.Add(new AccessoryMovement { CompanyId = companyId, AccessoryId = item.Id, ActorUserId = actorId, MovementType = "Received", Quantity = item.TotalQuantity, Notes = $"{(isXlsx ? "Excel" : "CSV")} içe aktarımındaki başlangıç stoku" }); db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Accessory", TargetId = item.Id, Action = "accessory.created", Details = item.Name + $"; {(isXlsx ? "Excel" : "CSV")} içe aktarımı." }); }
        db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Accessory", Action = isXlsx ? "XlsxImported" : "CsvImported", Details = $"{(isXlsx ? "Excel" : "CSV")} dosyasından {items.Count} aksesuar içe aktarıldı." });
        try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); } catch (DbUpdateException) { await tx.RollbackAsync(ct); return Results.Conflict(new { message = "CSV aktarılmadı; bir kayıt çakıştı. Önizlemeyi yenileyip tekrar deneyin." }); }
        return Results.Ok(new { imported = items.Count, defaultLocationApplied, message = $"{items.Count:N0} aksesuar ve başlangıç stok hareketleri eklendi; varsayılan konum {defaultLocationApplied:N0} kayda uygulandı." });
    }

    private static void AddError(List<string> errors, string message) { if (errors.Count < 100) errors.Add(message); }
    private static char DetectDelimiter(string text)
    {
        var commas = 0;
        var semicolons = 0;
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var value = text[i];
            if (value == '"')
            {
                if (quoted && i + 1 < text.Length && text[i + 1] == '"') { i++; continue; }
                quoted = !quoted;
                continue;
            }
            if (quoted) continue;
            if (value is '\r' or '\n') break;
            if (value == ',') commas++;
            else if (value == ';') semicolons++;
        }
        return semicolons > commas ? ';' : ',';
    }

    private static List<List<string>> Parse(string text, char delimiter)
    {
        var records = new List<List<string>>(); var row = new List<string>(); var cell = new StringBuilder(); bool quoted = false, afterQuote = false;
        for (int i = 0; i < text.Length; i++) { var ch = text[i]; if (quoted) { if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; } else if (ch == '"') { quoted = false; afterQuote = true; } else cell.Append(ch); continue; } if (afterQuote && ch != delimiter && ch is not ('\r' or '\n')) throw new FormatException(); if (ch == '"') { if (cell.Length != 0) throw new FormatException(); quoted = true; } else if (ch == delimiter) { row.Add(cell.ToString()); cell.Clear(); afterQuote = false; } else if (ch is '\r' or '\n') { if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++; row.Add(cell.ToString()); cell.Clear(); afterQuote = false; if (row.Any(x => x.Length > 0)) { records.Add(row); if (records.Count > MaxRows + 1) throw new FormatException(); } row = []; } else { cell.Append(ch); afterQuote = false; } }
        if (quoted) throw new FormatException(); if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); if (row.Any(x => x.Length > 0)) records.Add(row); } return records;
    }
}
