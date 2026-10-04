using System.Data;
using System.Security.Claims;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class DataExportEndpoints
{
    private const int BatchSize = 500;
    private const int PreviewMaxBytes = 32 * 1024 * 1024;
    private const int PreviewMaxRows = 250_000;
    private const int CsvImportMaxRows = 5_000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapDataExportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/company/export.json", ExportCompanyAsync)
            .RequireAuthorization("settings.manage");
        endpoints.MapPost("/api/company/import/preview", PreviewImportAsync)
            .RequireAuthorization("settings.manage")
            .WithMetadata(new RequestSizeLimitAttribute(PreviewMaxBytes));
        endpoints.MapPost("/api/assets/import/preview.csv", PreviewAssetCsvAsync)
            .RequireAuthorization("asset.write")
            .WithMetadata(new RequestSizeLimitAttribute(PreviewMaxBytes));
        endpoints.MapPost("/api/assets/import.csv", ImportAssetCsvAsync)
            .RequireAuthorization("asset.write")
            .WithMetadata(new RequestSizeLimitAttribute(PreviewMaxBytes));
        return endpoints;
    }

    private static async Task<IResult> ImportAssetCsvAsync(HttpContext context, AppDbContext db,
        IAntiforgery antiforgery, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(context.User.FindFirstValue("company_id"), out var companyId) ||
            !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Forbid();
        var role = context.User.FindFirstValue("company_role");
        if (!await db.Companies.AsNoTracking().AnyAsync(c => c.Id == companyId && c.IsActive &&
            db.CompanyMemberships.Any(m => m.CompanyId == companyId && m.UserId == userId && m.RoleKey == role), cancellationToken))
            return Results.Forbid();
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
        if (context.Request.ContentType?.StartsWith("text/csv", StringComparison.OrdinalIgnoreCase) != true)
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);

        using var buffer = new MemoryStream();
        var chunk = new byte[64 * 1024];
        while (true)
        {
            var read = await context.Request.Body.ReadAsync(chunk.AsMemory(), cancellationToken);
            if (read == 0) break;
            if (buffer.Length + read > PreviewMaxBytes) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }
        string text;
        try { text = new System.Text.UTF8Encoding(false, true).GetString(buffer.ToArray()).TrimStart('\uFEFF'); }
        catch (System.Text.DecoderFallbackException)
        { return Results.UnprocessableEntity(new { message = "CSV dosyası UTF-8 kodlamasında olmalıdır." }); }
        List<List<string>> rows;
        try { rows = ParseSemicolonCsv(text, CsvImportMaxRows + 2); }
        catch (FormatException)
        { return Results.UnprocessableEntity(new { message = "CSV tırnak/ayraç biçimi bozuk." }); }
        string[] expectedHeader = ["Etiket", "Ad", "Durum", "Kategori", "Marka", "Model", "Seri No", "Konum", "Personel No", "Çalışan", "Alış Tarihi", "Alış Bedeli", "Para Birimi"];
        if (rows.Count is < 2 || rows.Count > CsvImportMaxRows + 1 || !rows[0].SequenceEqual(expectedHeader, StringComparer.OrdinalIgnoreCase))
            return Results.UnprocessableEntity(new { message = $"EnvanterTR başlıklarıyla {CsvImportMaxRows:N0} veya daha az satır içeren bir CSV yükleyin." });

        var errors = new List<string>();
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (row, index) in rows.Skip(1).Select((row, index) => (row, index)))
        {
            if (row.Count != expectedHeader.Length) { AddError(errors, $"{index + 2}. satırda 13 sütun olmalıdır."); continue; }
            var tag = row[0].Trim();
            if (string.IsNullOrWhiteSpace(tag) || tag.Length > 80 || !tags.Add(tag)) AddError(errors, $"{index + 2}. satırda varlık etiketi boş, uzun veya yineleniyor.");
            if (string.IsNullOrWhiteSpace(row[1].Trim()) || row[1].Trim().Length > 240) AddError(errors, $"{index + 2}. satırda varlık adı boş veya çok uzun.");
            if (row[2] is not (AssetStatuses.Available or AssetStatuses.Deployed or AssetStatuses.Maintenance or AssetStatuses.Archived)) AddError(errors, $"{index + 2}. satırdaki durum desteklenmiyor.");
            if ((row[2] == AssetStatuses.Deployed) != !string.IsNullOrWhiteSpace(row[8])) AddError(errors, $"{index + 2}. satırda durum ve personel numarası eşleşmiyor.");
            if (string.IsNullOrWhiteSpace(row[3]) || row[3].Length > 120 || row[4].Length > 120 || row[5].Length > 120 || row[6].Length > 160 || row[7].Length > 160 || row[8].Length > 80 || row[9].Length > 200 || row[12].Length != 3 || !row[12].All(char.IsAsciiLetter))
                AddError(errors, $"{index + 2}. satırda alan uzunluğu, kategori veya para birimi geçersiz.");
            if (!string.IsNullOrWhiteSpace(row[10]) && !DateOnly.TryParseExact(row[10], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                AddError(errors, $"{index + 2}. satırdaki alış tarihi yyyy-AA-gg biçiminde olmalı.");
            if (!string.IsNullOrWhiteSpace(row[11]) && (!decimal.TryParse(row[11], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var cost) || cost < 0 || cost > 9999999999999999.99m))
                AddError(errors, $"{index + 2}. satırdaki alış bedeli geçersiz.");
        }
        var cleanErrors = errors.Distinct(StringComparer.Ordinal).Take(30).ToArray();
        if (cleanErrors.Length > 0) return Results.UnprocessableEntity(new { message = "CSV içe alınamadı; dosya sorunlarını düzeltin.", errors = cleanErrors });

        var categories = await db.AssetCategories.AsNoTracking().Where(x => x.CompanyId == companyId).ToListAsync(cancellationToken);
        var brands = await db.AssetBrands.AsNoTracking().Where(x => x.CompanyId == companyId).ToListAsync(cancellationToken);
        var models = await db.AssetModels.AsNoTracking().Where(x => x.CompanyId == companyId).ToListAsync(cancellationToken);
        var employees = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId).ToListAsync(cancellationToken);
        var categoryLookup = categories.GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.OrdinalIgnoreCase);
        var brandLookup = brands.GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.OrdinalIgnoreCase);
        var modelLookup = models.GroupBy(x => (x.BrandId, Name: x.Name.Trim()), new BrandModelComparer()).ToDictionary(x => x.Key, x => x.ToArray(), new BrandModelComparer());
        var employeeLookup = employees.GroupBy(x => x.EmployeeNumber.Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.OrdinalIgnoreCase);
        var importRows = new List<(Asset Asset, Guid? EmployeeId)>(rows.Count - 1);
        foreach (var (row, index) in rows.Skip(1).Select((row, index) => (row, index)))
        {
            if (!categoryLookup.TryGetValue(row[3].Trim(), out var categoryMatches) || categoryMatches.Length != 1)
                AddError(errors, $"{index + 2}. satırdaki kategori mevcut şirkette yok veya birden fazla kez tanımlı.");
            Guid? brandId = null; Guid? modelId = null;
            if (!string.IsNullOrWhiteSpace(row[4]))
            {
                if (!brandLookup.TryGetValue(row[4].Trim(), out var brandMatches) || brandMatches.Length != 1)
                    AddError(errors, $"{index + 2}. satırdaki marka mevcut şirkette yok veya birden fazla kez tanımlı.");
                else brandId = brandMatches[0].Id;
            }
            if (!string.IsNullOrWhiteSpace(row[5]))
            {
                if (brandId is null || !modelLookup.TryGetValue((brandId, row[5].Trim()), out var modelMatches) || modelMatches.Length != 1)
                    AddError(errors, $"{index + 2}. satırdaki model seçilen markayla mevcut şirkette bulunamadı.");
                else modelId = modelMatches[0].Id;
            }
            Guid? employeeId = null;
            if (!string.IsNullOrWhiteSpace(row[8]))
            {
                if (!employeeLookup.TryGetValue(row[8].Trim(), out var employeeMatches) || employeeMatches.Length != 1 ||
                    !string.Equals(employeeMatches[0].DisplayName.Trim(), row[9].Trim(), StringComparison.OrdinalIgnoreCase))
                    AddError(errors, $"{index + 2}. satırdaki çalışan numarası ve adı mevcut şirkette eşleşmiyor.");
                else employeeId = employeeMatches[0].Id;
            }
            if (categoryLookup.TryGetValue(row[3].Trim(), out categoryMatches) && categoryMatches.Length == 1)
            {
                DateOnly? purchaseDate = string.IsNullOrWhiteSpace(row[10]) ? null : DateOnly.ParseExact(row[10], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                decimal? purchaseCost = string.IsNullOrWhiteSpace(row[11]) ? null : decimal.Parse(row[11], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture);
                importRows.Add((new Asset
                {
                    CompanyId = companyId, AssetTag = row[0].Trim().ToUpperInvariant(), Name = row[1].Trim(), Status = row[2],
                    CategoryId = categoryMatches[0].Id, BrandId = brandId, ModelId = modelId, SerialNumber = CleanCsv(row[6]),
                    Location = CleanCsv(row[7]), AssignedEmployeeId = employeeId, PurchaseDate = purchaseDate,
                    PurchaseCost = purchaseCost, Currency = row[12].Trim().ToUpperInvariant()
                }, employeeId));
            }
        }
        var normalizedTags = tags.Select(x => x.ToUpperInvariant()).ToArray();
        var alreadyPresent = await db.Assets.AsNoTracking().AnyAsync(x => x.CompanyId == companyId && normalizedTags.Contains(x.AssetTag.ToUpper()), cancellationToken);
        if (alreadyPresent) AddError(errors, "Dosyadaki en az bir varlık etiketi bu şirkette zaten kayıtlı.");
        cleanErrors = errors.Distinct(StringComparer.Ordinal).Take(30).ToArray();
        if (cleanErrors.Length > 0) return Results.UnprocessableEntity(new { message = "CSV içe alınamadı; mevcut şirket kayıtlarıyla eşleşme sorunlarını düzeltin.", errors = cleanErrors });

        var actorId = userId;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Assets.AddRange(importRows.Select(x => x.Asset));
        foreach (var item in importRows)
            db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = item.Asset.Id, ActorUserId = actorId,
                EventType = "Created", Details = "Varlık CSV dosyasından içe aktarıldı.", EmployeeId = item.EmployeeId });
        db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Asset", Action = "CsvImported",
            Details = $"CSV dosyasından {importRows.Count} varlık içe aktarıldı." });
        try { await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.Conflict(new { message = "CSV kaydedilmedi. Etiket veya ilişkili kayıtlar işlem sırasında değişmiş olabilir; önizlemeyi yenileyin." });
        }
        return Results.Ok(new { imported = importRows.Count, message = $"{importRows.Count:N0} varlık başarıyla içe aktarıldı." });
    }

    private static string? CleanCsv(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class BrandModelComparer : IEqualityComparer<(Guid? BrandId, string Name)>
    {
        public bool Equals((Guid? BrandId, string Name) x, (Guid? BrandId, string Name) y) => x.BrandId == y.BrandId && StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name);
        public int GetHashCode((Guid? BrandId, string Name) value) => HashCode.Combine(value.BrandId, StringComparer.OrdinalIgnoreCase.GetHashCode(value.Name));
    }

    private static async Task<IResult> PreviewAssetCsvAsync(HttpContext context, AppDbContext db,
        IAntiforgery antiforgery, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(context.User.FindFirstValue("company_id"), out var companyId) ||
            !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Forbid();
        var role = context.User.FindFirstValue("company_role");
        var activeMembership = await db.Companies.AsNoTracking().AnyAsync(c => c.Id == companyId && c.IsActive &&
            db.CompanyMemberships.Any(m => m.CompanyId == companyId && m.UserId == userId && m.RoleKey == role), cancellationToken);
        if (!activeMembership) return Results.Forbid();
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
        if (context.Request.ContentType?.StartsWith("text/csv", StringComparison.OrdinalIgnoreCase) != true)
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);

        using var buffer = new MemoryStream();
        var chunk = new byte[64 * 1024];
        while (true)
        {
            var read = await context.Request.Body.ReadAsync(chunk.AsMemory(), cancellationToken);
            if (read == 0) break;
            if (buffer.Length + read > PreviewMaxBytes) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        string text;
        try { text = new System.Text.UTF8Encoding(false, true).GetString(buffer.ToArray()).TrimStart('\uFEFF'); }
        catch (System.Text.DecoderFallbackException)
        { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "CSV dosyası UTF-8 kodlamasında olmalıdır." } }); }
        List<List<string>> records;
        try { records = ParseSemicolonCsv(text, CsvImportMaxRows + 2); }
        catch (FormatException)
        { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "CSV tırnak/ayraç biçimi bozuk." } }); }
        if (records.Count is < 2 || records.Count > CsvImportMaxRows + 1)
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { $"CSV başlık ve en fazla {CsvImportMaxRows:N0} varlık satırı içermelidir." } });

        string[] expectedHeader = ["Etiket", "Ad", "Durum", "Kategori", "Marka", "Model", "Seri No", "Konum", "Personel No", "Çalışan", "Alış Tarihi", "Alış Bedeli", "Para Birimi"];
        if (!records[0].SequenceEqual(expectedHeader, StringComparer.OrdinalIgnoreCase))
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "CSV sütun başlıkları EnvanterTR varlık dışa aktarımıyla eşleşmiyor." } });

        var errors = new List<string>();
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (row, index) in records.Skip(1).Select((row, index) => (row, index)))
        {
            if (row.Count != expectedHeader.Length) { AddError(errors, $"{index + 2}. satırda 13 sütun olmalıdır."); continue; }
            var tag = row[0].Trim();
            if (string.IsNullOrWhiteSpace(tag) || tag.Length > 80) AddError(errors, $"{index + 2}. satırda varlık etiketi boş veya 80 karakterden uzun.");
            else if (!tags.Add(tag)) AddError(errors, "CSV içinde yinelenen varlık etiketi var.");
            if (string.IsNullOrWhiteSpace(row[1].Trim()) || row[1].Trim().Length > 240) AddError(errors, $"{index + 2}. satırda varlık adı boş veya 240 karakterden uzun.");
            if (row[2] is not (AssetStatuses.Available or AssetStatuses.Deployed or AssetStatuses.Maintenance or AssetStatuses.Archived)) AddError(errors, $"{index + 2}. satırdaki durum desteklenmiyor.");
            if ((row[2] == AssetStatuses.Deployed) != !string.IsNullOrWhiteSpace(row[8])) AddError(errors, $"{index + 2}. satırda atanmış durumuyla personel bilgisi eşleşmiyor.");
            if (row[3].Length > 120 || row[4].Length > 120 || row[5].Length > 120 || row[6].Length > 160 || row[7].Length > 160 || row[8].Length > 80 || row[9].Length > 200 || row[12].Length != 3)
                AddError(errors, $"{index + 2}. satırda alan uzunluğu veya para birimi biçimi geçersiz.");
            if (!string.IsNullOrWhiteSpace(row[10]) && !DateOnly.TryParseExact(row[10], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                AddError(errors, $"{index + 2}. satırdaki alış tarihi yyyy-AA-gg biçiminde olmalı.");
            if (!string.IsNullOrWhiteSpace(row[11]) && (!decimal.TryParse(row[11], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var cost) || cost < 0))
                AddError(errors, $"{index + 2}. satırdaki alış bedeli geçersiz veya negatif.");
        }

        var normalizedTags = tags.Select(tag => tag.ToUpperInvariant()).ToArray();
        var existingMatches = 0;
        foreach (var batch in normalizedTags.Chunk(500))
            existingMatches += await db.Assets.AsNoTracking()
                .CountAsync(asset => asset.CompanyId == companyId && batch.Contains(asset.AssetTag.ToUpper()), cancellationToken);
        var targetName = await db.Companies.AsNoTracking().Where(c => c.Id == companyId).Select(c => c.Name).SingleAsync(cancellationToken);
        return Results.Ok(new
        {
            isValid = errors.Count == 0,
            sourceCompany = "CSV dosyası",
            targetCompany = targetName,
            schemaVersion = 1,
            counts = new { assets = records.Count - 1 },
            existingMatches = new { assets = existingMatches },
            errors = errors.Take(30).ToArray(),
            warning = "Bu önizleme yalnızca EnvanterTR CSV sütunlarını ve varlık etiketlerini denetler; mevcut eşleşmeler otomatik birleştirilmez ve hiçbir kayıt değiştirilmez."
        });
    }

    private static List<List<string>> ParseSemicolonCsv(string text, int maximumRecords)
    {
        var records = new List<List<string>>();
        var row = new List<string>();
        var cell = new System.Text.StringBuilder();
        var quoted = false;
        var afterQuote = false;
        for (var i = 0; i < text.Length; i++)
        {
            var value = text[i];
            if (quoted)
            {
                if (value == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                else if (value == '"') { quoted = false; afterQuote = true; }
                else cell.Append(value);
                continue;
            }
            if (afterQuote && value is not (';' or '\r' or '\n')) throw new FormatException();
            if (value == '"')
            {
                if (cell.Length != 0) throw new FormatException();
                quoted = true;
            }
            else if (value == ';') { row.Add(cell.ToString()); cell.Clear(); afterQuote = false; }
            else if (value is '\r' or '\n')
            {
                if (value == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(cell.ToString()); cell.Clear();
                if (row.Any(field => field.Length > 0)) { records.Add(row); if (records.Count > maximumRecords) return records; }
                row = []; afterQuote = false;
            }
            else cell.Append(value);
        }
        if (quoted) throw new FormatException();
        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            if (row.Any(field => field.Length > 0)) records.Add(row);
        }
        return records;
    }

    private static async Task<IResult> PreviewImportAsync(HttpContext context, AppDbContext db,
        IAntiforgery antiforgery, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(context.User.FindFirstValue("company_id"), out var targetCompanyId) ||
            !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Forbid();

        var role = context.User.FindFirstValue("company_role");
        var activeMembership = await db.Companies.AsNoTracking().AnyAsync(c => c.Id == targetCompanyId && c.IsActive &&
            db.CompanyMemberships.Any(m => m.CompanyId == targetCompanyId && m.UserId == userId && m.RoleKey == role), cancellationToken);
        if (!activeMembership) return Results.Forbid();
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }

        if (context.Request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) != true)
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        if (context.Request.ContentLength > PreviewMaxBytes)
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        using var buffer = new MemoryStream();
        var chunk = new byte[64 * 1024];
        while (true)
        {
            var read = await context.Request.Body.ReadAsync(chunk.AsMemory(), cancellationToken);
            if (read == 0) break;
            if (buffer.Length + read > PreviewMaxBytes)
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        try
        {
            using var document = JsonDocument.Parse(buffer.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            var errors = new List<string>();
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("format", out var format) ||
                format.ValueKind != JsonValueKind.String || format.GetString() != "envantertr-company-data")
                return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "Dosya EnvanterTR şirket veri biçiminde değil." } });
            if (!root.TryGetProperty("schemaVersion", out var version) || !version.TryGetInt32(out var schemaVersion) || schemaVersion != 1)
                return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "Dosyanın biçim sürümü desteklenmiyor." } });
            if (!root.TryGetProperty("company", out var company) || company.ValueKind != JsonValueKind.Object ||
                !TryGuid(company, "id", out var sourceCompanyId) || !company.TryGetProperty("name", out var companyName) ||
                companyName.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(companyName.GetString()) || companyName.GetString()!.Length > 200)
                return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "Şirket bilgisi eksik veya bozuk." } });

            var employees = ReadRows(root, "employees", sourceCompanyId, errors, counts);
            var categories = ReadRows(root, "assetCategories", sourceCompanyId, errors, counts);
            var brands = ReadRows(root, "assetBrands", sourceCompanyId, errors, counts);
            var models = ReadRows(root, "assetModels", sourceCompanyId, errors, counts);
            var assets = ReadRows(root, "assets", sourceCompanyId, errors, counts);
            var events = ReadRows(root, "assetEvents", sourceCompanyId, errors, counts);
            _ = ReadRows(root, "auditEntries", sourceCompanyId, errors, counts);
            if (!root.TryGetProperty("members", out var members) || members.ValueKind != JsonValueKind.Array)
                AddError(errors, "members alanı eksik veya dizi değil.");
            else
            {
                counts["members"] = members.GetArrayLength();
                foreach (var member in members.EnumerateArray())
                {
                    if (member.ValueKind != JsonValueKind.Object ||
                        string.IsNullOrWhiteSpace(ReadString(member, "displayName")) ||
                        ReadString(member, "displayName")!.Length > 160 ||
                        string.IsNullOrWhiteSpace(ReadString(member, "email")) ||
                        ReadString(member, "email")!.Length > 256 ||
                        ReadString(member, "roleKey") is not ("owner" or "admin" or "asset_manager" or "asset_operator" or "auditor" or "viewer" or "hr_operator"))
                        AddError(errors, "Bir uygulama üyesinin adı, e-postası veya rolü geçersiz.");
                }
            }

            CheckUniqueText(root, "employees", "employeeNumber", "Çalışan numarası", errors, normalizeUpper: true);
            CheckUniqueText(root, "assetCategories", "name", "Kategori adı", errors);
            CheckUniqueText(root, "assetBrands", "name", "Marka adı", errors);
            CheckUniqueText(root, "assets", "assetTag", "Varlık etiketi", errors, normalizeUpper: true);
            CheckRequiredText(root, "employees", "employeeNumber", "Çalışan numarası", 80, errors);
            CheckRequiredText(root, "employees", "displayName", "Çalışan adı", 200, errors);
            CheckRequiredText(root, "assetCategories", "name", "Kategori adı", 120, errors);
            CheckRequiredText(root, "assetBrands", "name", "Marka adı", 120, errors);
            CheckRequiredText(root, "assetModels", "name", "Model adı", 120, errors);
            CheckRequiredText(root, "assets", "assetTag", "Varlık etiketi", 80, errors);
            CheckRequiredText(root, "assets", "name", "Varlık adı", 240, errors);
            CheckRequiredText(root, "assets", "status", "Varlık durumu", 40, errors);
            CheckRequiredText(root, "assetEvents", "eventType", "Hareket türü", 60, errors);
            CheckRequiredText(root, "auditEntries", "targetType", "Denetim hedef türü", 80, errors);
            CheckRequiredText(root, "auditEntries", "action", "Denetim işlemi", 80, errors);
            CheckOptionalText(root, "employees", "email", "Çalışan e-postası", 256, errors);
            CheckOptionalText(root, "employees", "department", "Departman", 160, errors);
            CheckOptionalText(root, "assetEvents", "details", "Hareket açıklaması", 2000, errors);
            CheckOptionalText(root, "auditEntries", "details", "Denetim açıklaması", 2000, errors);

            foreach (var model in EnumerateRows(root, "assetModels"))
            {
                if (model.ValueKind != JsonValueKind.Object) continue;
                if (TryGuid(model, "brandId", out var brandId) && !brands.Contains(brandId))
                    AddError(errors, "Bir model, dosyada bulunmayan bir markaya bağlı.");
            }
            var modelBrands = new Dictionary<Guid, Guid?>();
            foreach (var model in EnumerateRows(root, "assetModels"))
                if (model.ValueKind == JsonValueKind.Object && TryGuid(model, "id", out var modelId))
                    modelBrands.TryAdd(modelId, TryGuid(model, "brandId", out var brandId) ? brandId : null);
            foreach (var asset in EnumerateRows(root, "assets"))
            {
                if (asset.ValueKind != JsonValueKind.Object) continue;
                if (TryGuid(asset, "categoryId", out var categoryId) && !categories.Contains(categoryId)) AddError(errors, "Bir varlığın kategorisi dosyada bulunamadı.");
                if (TryGuid(asset, "brandId", out var brandId) && !brands.Contains(brandId)) AddError(errors, "Bir varlığın markası dosyada bulunamadı.");
                if (TryGuid(asset, "modelId", out var modelId) && !models.Contains(modelId)) AddError(errors, "Bir varlığın modeli dosyada bulunamadı.");
                else if (TryGuid(asset, "modelId", out modelId) && modelBrands.TryGetValue(modelId, out var modelBrand) &&
                    ((modelBrand is null && TryGuid(asset, "brandId", out _)) ||
                     (modelBrand is not null && (!TryGuid(asset, "brandId", out var assetBrand) || assetBrand != modelBrand))))
                    AddError(errors, "Bir varlığın seçilen modeliyle markası eşleşmiyor.");
                if (TryGuid(asset, "assignedEmployeeId", out var employeeId) && !employees.Contains(employeeId)) AddError(errors, "Bir varlığın zimmetli çalışanı dosyada bulunamadı.");
                var status = ReadString(asset, "status");
                var hasEmployee = TryGuid(asset, "assignedEmployeeId", out _);
                if (status is not (AssetStatuses.Available or AssetStatuses.Deployed or AssetStatuses.Maintenance or AssetStatuses.Archived))
                    AddError(errors, "Bir varlığın durumu desteklenmiyor.");
                if ((status == AssetStatuses.Deployed) != hasEmployee)
                    AddError(errors, "Atanmış durumundaki varlıkta çalışan olmalı; diğer durumlarda çalışan alanı boş olmalı.");
                if (ReadString(asset, "currency") is not { Length: 3 }) AddError(errors, "Bir varlığın para birimi 3 harfli kod olmalı.");
                if (asset.TryGetProperty("purchaseCost", out var cost) && cost.ValueKind != JsonValueKind.Null &&
                    (cost.ValueKind != JsonValueKind.Number || !cost.TryGetDecimal(out var amount) || amount < 0))
                    AddError(errors, "Bir varlığın alış bedeli geçersiz veya negatif.");
            }
            foreach (var entry in EnumerateRows(root, "assetEvents"))
            {
                if (entry.ValueKind != JsonValueKind.Object) continue;
                if (TryGuid(entry, "assetId", out var assetId) && !assets.Contains(assetId)) AddError(errors, "Bir hareket, dosyada bulunmayan bir varlığa bağlı.");
                if (TryGuid(entry, "employeeId", out var employeeId) && !employees.Contains(employeeId)) AddError(errors, "Bir hareket, dosyada bulunmayan bir çalışana bağlı.");
            }

            if (counts.Values.Sum() > PreviewMaxRows) AddError(errors, $"Dosya en fazla {PreviewMaxRows:N0} kayıt içerebilir.");
            var targetName = await db.Companies.AsNoTracking().Where(c => c.Id == targetCompanyId)
                .Select(c => c.Name).SingleAsync(cancellationToken);
            var existingMatches = await CountExistingMatchesAsync(db, targetCompanyId, root, cancellationToken);
            var cleanErrors = errors.Distinct(StringComparer.Ordinal).Take(30).ToArray();
            return Results.Ok(new
            {
                isValid = cleanErrors.Length == 0,
                sourceCompany = companyName.GetString(),
                targetCompany = targetName,
                schemaVersion,
                counts,
                existingMatches,
                errors = cleanErrors,
                warning = "Eşleşme sayıları yalnızca aynı etiket/numara/adı taşıyan mevcut kayıtları gösterir; otomatik birleştirme kararı değildir. Bu işlem hiçbir kayıt değiştirmedi."
            });
        }
        catch (JsonException)
        {
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "Dosya geçerli JSON değil veya iç içe veri sınırını aşıyor." } });
        }
    }

    private static HashSet<Guid> ReadRows(JsonElement root, string name, Guid sourceCompanyId,
        List<string> errors, Dictionary<string, int> counts)
    {
        var ids = new HashSet<Guid>();
        if (!root.TryGetProperty(name, out var rows) || rows.ValueKind != JsonValueKind.Array)
        {
            AddError(errors, $"{name} alanı eksik veya dizi değil.");
            counts[name] = 0;
            return ids;
        }
        counts[name] = rows.GetArrayLength();
        foreach (var row in rows.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object || !TryGuid(row, "id", out var id))
            {
                AddError(errors, $"{name} içinde kimliği olmayan veya bozuk kayıt var.");
                continue;
            }
            if (!ids.Add(id)) AddError(errors, $"{name} içinde yinelenen kayıt kimliği var.");
            if (!TryGuid(row, "companyId", out var rowCompanyId) || rowCompanyId != sourceCompanyId)
                AddError(errors, $"{name} içinde başka şirket kimliğine ait kayıt var.");
        }
        return ids;
    }

    private static bool TryGuid(JsonElement element, string property, out Guid value)
    {
        value = Guid.Empty;
        return element.TryGetProperty(property, out var candidate) && candidate.ValueKind == JsonValueKind.String &&
            Guid.TryParse(candidate.GetString(), out value);
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static void CheckRequiredText(JsonElement root, string rowsProperty, string valueProperty,
        string label, int maxLength, List<string> errors)
    {
        foreach (var row in EnumerateRows(root, rowsProperty))
        {
            if (row.ValueKind != JsonValueKind.Object) continue;
            var value = ReadString(row, valueProperty)?.Trim();
            if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
                AddError(errors, $"{label} boş olamaz ve {maxLength} karakteri aşamaz.");
        }
    }

    private static void CheckOptionalText(JsonElement root, string rowsProperty, string valueProperty,
        string label, int maxLength, List<string> errors)
    {
        foreach (var row in EnumerateRows(root, rowsProperty))
        {
            if (row.ValueKind != JsonValueKind.Object) continue;
            var value = ReadString(row, valueProperty);
            if (value is not null && value.Length > maxLength)
                AddError(errors, $"{label} {maxLength} karakteri aşamaz.");
        }
    }

    private static void CheckUniqueText(JsonElement root, string rowsProperty, string valueProperty,
        string label, List<string> errors, bool normalizeUpper = false)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in EnumerateRows(root, rowsProperty))
        {
            if (row.ValueKind != JsonValueKind.Object) continue;
            var value = ReadString(row, valueProperty)?.Trim();
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (normalizeUpper) value = value.ToUpperInvariant();
            if (!seen.Add(value)) AddError(errors, $"Dosyada yinelenen {label.ToLowerInvariant()} değeri var.");
        }
    }

    private static async Task<object> CountExistingMatchesAsync(AppDbContext db, Guid companyId,
        JsonElement root, CancellationToken cancellationToken)
    {
        static List<string> Values(JsonElement rootElement, string array, string property, bool upper = false) =>
            EnumerateRows(rootElement, array).Where(row => row.ValueKind == JsonValueKind.Object)
                .Select(row => ReadString(row, property)?.Trim()).Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => upper ? value!.ToUpperInvariant() : value!).Distinct(StringComparer.Ordinal).ToList();

        async Task<int> CountAsync(List<string> values, Func<string[], IQueryable<string>> query)
        {
            var count = 0;
            foreach (var batch in values.Chunk(500))
                count += await query(batch).CountAsync(cancellationToken);
            return count;
        }

        var employees = Values(root, "employees", "employeeNumber", upper: true);
        var tags = Values(root, "assets", "assetTag", upper: true);
        var categories = Values(root, "assetCategories", "name", upper: true);
        var brands = Values(root, "assetBrands", "name", upper: true);
        return new
        {
            employees = await CountAsync(employees, batch => db.Employees.AsNoTracking()
                .Where(x => x.CompanyId == companyId && batch.Contains(x.EmployeeNumber.ToUpper())).Select(x => x.EmployeeNumber)),
            assets = await CountAsync(tags, batch => db.Assets.AsNoTracking()
                .Where(x => x.CompanyId == companyId && batch.Contains(x.AssetTag.ToUpper())).Select(x => x.AssetTag)),
            categories = await CountAsync(categories, batch => db.AssetCategories.AsNoTracking()
                .Where(x => x.CompanyId == companyId && batch.Contains(x.Name.ToUpper())).Select(x => x.Name)),
            brands = await CountAsync(brands, batch => db.AssetBrands.AsNoTracking()
                .Where(x => x.CompanyId == companyId && batch.Contains(x.Name.ToUpper())).Select(x => x.Name))
        };
    }

    private static IEnumerable<JsonElement> EnumerateRows(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var rows) || rows.ValueKind != JsonValueKind.Array) yield break;
        foreach (var row in rows.EnumerateArray()) yield return row;
    }

    private static void AddError(List<string> errors, string message)
    {
        if (errors.Count < 30 && !errors.Contains(message, StringComparer.Ordinal)) errors.Add(message);
    }

    private static async Task<IResult> ExportCompanyAsync(
        HttpContext context, AppDbContext db, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(context.User.FindFirstValue("company_id"), out var companyId) ||
            !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Forbid();

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);
        var role = context.User.FindFirstValue("company_role");
        var activeMembership = await db.Companies.AsNoTracking().AnyAsync(c => c.Id == companyId && c.IsActive &&
            db.CompanyMemberships.Any(m => m.CompanyId == companyId && m.UserId == userId && m.RoleKey == role), cancellationToken);
        if (!activeMembership) return Results.Forbid();
        var company = await db.Companies.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == companyId, cancellationToken);
        if (company is null) return Results.NotFound();

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store, private";
        context.Response.Headers.ContentDisposition =
            $"attachment; filename=envantertr-veri-{DateTime.UtcNow:yyyyMMdd}.json";

        using var writer = new Utf8JsonWriter(context.Response.Body, new JsonWriterOptions
        {
            Indented = false,
            SkipValidation = false
        });

        writer.WriteStartObject();
        writer.WriteString("format", "envantertr-company-data");
        writer.WriteNumber("schemaVersion", 1);
        writer.WriteString("exportedAt", DateTimeOffset.UtcNow);
        writer.WritePropertyName("company");
        JsonSerializer.Serialize(writer, new
        {
            company.Id, company.Name, company.Slug, company.ContactEmail,
            company.BrandLogoPath, company.BrandPrimaryColor, company.CreatedAt
        }, JsonOptions);

        await WritePagedAsync(writer, "employees", db.Employees.AsNoTracking()
            .Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        await WritePagedAsync(writer, "assetCategories", db.AssetCategories.AsNoTracking()
            .Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        await WritePagedAsync(writer, "assetBrands", db.AssetBrands.AsNoTracking()
            .Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        await WritePagedAsync(writer, "assetModels", db.AssetModels.AsNoTracking()
            .Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        await WritePagedAsync(writer, "assets", db.Assets.AsNoTracking()
            .Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        await WritePagedAsync(writer, "assetEvents", db.AssetEvents.AsNoTracking()
            .Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        await WritePagedAsync(writer, "auditEntries", db.AuditEntries.AsNoTracking()
            .Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);

        var members = from membership in db.CompanyMemberships.AsNoTracking()
                          .Where(x => x.CompanyId == companyId)
                      join user in db.Users.AsNoTracking() on membership.UserId equals user.Id
                      orderby membership.UserId
                      select new
                      {
                          user.DisplayName,
                          Email = user.Email!,
                          membership.RoleKey,
                          membership.CreatedAt
                      };
        await WritePagedAsync(writer, "members", members, cancellationToken);

        writer.WriteEndObject();
        await writer.FlushAsync(cancellationToken);
        return Results.Empty;
    }

    private static async Task WritePagedAsync<T>(Utf8JsonWriter writer, string propertyName,
        IQueryable<T> query, CancellationToken cancellationToken)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStartArray();
        var offset = 0;
        while (true)
        {
            var batch = await query.Skip(offset).Take(BatchSize).ToListAsync(cancellationToken);
            foreach (var item in batch)
                JsonSerializer.Serialize(writer, item, JsonOptions);
            await writer.FlushAsync(cancellationToken);
            if (batch.Count < BatchSize) break;
            offset += batch.Count;
        }
        writer.WriteEndArray();
    }
}
