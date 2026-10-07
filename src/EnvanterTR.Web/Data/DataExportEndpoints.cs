using System.Data;
using System.Security.Claims;
using System.Linq;
using System.Text.Json;
using System.Text;
using System.IO.Compression;
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
        endpoints.MapGet("/api/company/export.zip", ExportCompanyArchiveAsync)
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
        endpoints.MapPost("/api/employees/import/preview.csv", (HttpContext context, AppDbContext db,
            IAntiforgery antiforgery, CancellationToken cancellationToken) => ImportEmployeeCsvAsync(context, db, antiforgery, false, cancellationToken))
            .RequireAuthorization("people.manage")
            .WithMetadata(new RequestSizeLimitAttribute(PreviewMaxBytes));
        endpoints.MapPost("/api/employees/import.csv", (HttpContext context, AppDbContext db,
            IAntiforgery antiforgery, CancellationToken cancellationToken) => ImportEmployeeCsvAsync(context, db, antiforgery, true, cancellationToken))
            .RequireAuthorization("people.manage")
            .WithMetadata(new RequestSizeLimitAttribute(PreviewMaxBytes));
        return endpoints;
    }

    private static async Task<IResult> ImportEmployeeCsvAsync(HttpContext context, AppDbContext db,
        IAntiforgery antiforgery, bool import, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(context.User.FindFirstValue("company_id"), out var companyId) ||
            !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
        var role = context.User.FindFirstValue("company_role");
        if (!await db.Companies.AsNoTracking().AnyAsync(c => c.Id == companyId && c.IsActive &&
            db.CompanyMemberships.Any(m => m.CompanyId == companyId && m.UserId == actorId && m.RoleKey == role), cancellationToken)) return Results.Forbid();
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
        if (context.Request.ContentType?.StartsWith("text/csv", StringComparison.OrdinalIgnoreCase) != true)
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        if (context.Request.ContentLength > PreviewMaxBytes) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

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
        List<List<string>> rows;
        try { rows = ParseSemicolonCsv(text, CsvImportMaxRows + 2, ','); }
        catch (FormatException)
        { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "CSV tırnak/ayraç biçimi bozuk." } }); }
        string[] header = ["Personel No", "Ad Soyad", "E-posta", "Departman", "Görev Unvanı", "Telefon", "Konum", "İşe Başlama Tarihi"];
        if (rows.Count is < 2 || rows.Count > CsvImportMaxRows + 1 || !rows[0].SequenceEqual(header, StringComparer.OrdinalIgnoreCase))
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { $"İçe aktarma şablonunu kullanın: başlık satırı ve en fazla {CsvImportMaxRows:N0} çalışan satırı." } });

        var errors = new List<string>();
        var seenNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var employees = new List<Employee>(rows.Count - 1);
        foreach (var (row, index) in rows.Skip(1).Select((row, index) => (row, index)))
        {
            var line = index + 2;
            if (row.Count != header.Length) { AddError(errors, $"{line}. satırda {header.Length} sütun olmalıdır."); continue; }
            var number = row[0].Trim().ToUpperInvariant();
            var name = row[1].Trim();
            var email = CleanCsv(row[2]); var department = CleanCsv(row[3]); var jobTitle = CleanCsv(row[4]);
            var phone = CleanCsv(row[5]); var location = CleanCsv(row[6]);
            DateOnly? startedOn = null;
            if (string.IsNullOrWhiteSpace(number) || number.Length > 80 || !seenNumbers.Add(number)) AddError(errors, $"{line}. satırdaki personel numarası boş, 80 karakterden uzun veya dosyada yineleniyor.");
            if (string.IsNullOrWhiteSpace(name) || name.Length > 200) AddError(errors, $"{line}. satırdaki ad soyad boş veya 200 karakterden uzun.");
            if (email is { Length: > 256 } || email is not null && (!System.Net.Mail.MailAddress.TryCreate(email, out var parsedEmail) || !string.Equals(parsedEmail.Address, email, StringComparison.OrdinalIgnoreCase)))
                AddError(errors, $"{line}. satırdaki e-posta biçimi geçersiz veya 256 karakterden uzun.");
            if (department is { Length: > 160 } || jobTitle is { Length: > 160 } || phone is { Length: > 40 } || location is { Length: > 160 })
                AddError(errors, $"{line}. satırdaki departman, görev, telefon veya konum alanı sınırı aşıyor.");
            if (!string.IsNullOrWhiteSpace(row[7]))
            {
                if (DateOnly.TryParseExact(row[7].Trim(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedDate)) startedOn = parsedDate;
                else AddError(errors, $"{line}. satırdaki işe başlama tarihi yyyy-AA-gg biçiminde olmalıdır.");
            }
            employees.Add(new Employee { CompanyId = companyId, EmployeeNumber = number, DisplayName = name,
                Email = email, Department = department, JobTitle = jobTitle, Phone = phone, Location = location, StartedOn = startedOn });
        }
        var numbers = seenNumbers.ToArray();
        var existingNumbers = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && numbers.Contains(x.EmployeeNumber.ToUpper()))
            .Select(x => x.EmployeeNumber).ToListAsync(cancellationToken);
        if (existingNumbers.Count > 0)
            AddError(errors, $"Şirkette zaten kayıtlı personel numaraları var: {string.Join(", ", existingNumbers.Take(10))}{(existingNumbers.Count > 10 ? "…" : "")}");
        var cleanErrors = errors.Distinct(StringComparer.Ordinal).Take(30).ToArray();
        if (cleanErrors.Length > 0)
            return Results.UnprocessableEntity(new { isValid = false, rowCount = rows.Count - 1, errors = cleanErrors, message = "Dosyada düzeltilmesi gereken alanlar bulundu. Hiçbir çalışan eklenmedi." });

        var employeeLocationCatalogPaths = await AssetEndpoints.GetActiveLocationPathMapAsync(db, companyId, cancellationToken);
        var defaultEmployeeLocation = await AssetEndpoints.GetDefaultLocationPathAsync(db, companyId, cancellationToken);
        var defaultEmployeeLocationApplied = defaultEmployeeLocation is null ? 0 : employees.Count(x => x.Location is null);
        foreach (var employee in employees)
        {
            employee.Location ??= defaultEmployeeLocation;
            if (employee.Location is not null && employeeLocationCatalogPaths.TryGetValue(employee.Location, out var locationCatalogId))
                employee.LocationCatalogId = locationCatalogId;
        }

        if (!import) return Results.Ok(new { isValid = true, rowCount = employees.Count, errors = Array.Empty<string>(),
            linkedLocations = employees.Count(x => x.LocationCatalogId is not null),
            defaultLocationApplied = defaultEmployeeLocationApplied,
            message = $"{employees.Count:N0} yeni çalışan kaydı doğrulandı. Varsayılan konum {defaultEmployeeLocationApplied:N0} boş kayda uygulanacak; katalogla eşleşen {employees.Count(x => x.LocationCatalogId is not null):N0} kayıt bağlanacak. Önizleme hiçbir kayıt eklemedi." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        // Recheck under the write transaction so duplicate employee numbers never partially import.
        if (await db.Employees.AnyAsync(x => x.CompanyId == companyId && numbers.Contains(x.EmployeeNumber.ToUpper()), cancellationToken))
            return Results.Conflict(new { message = "Dosyadaki bir personel numarası önizlemeden sonra eklenmiş. Hiçbir kayıt aktarılmadı; önizlemeyi yenileyin." });
        db.Employees.AddRange(employees);
        foreach (var employee in employees)
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Employee",
                TargetId = employee.Id, Action = "Created", Details = $"Personel no: {employee.EmployeeNumber}; CSV içe aktarımı." });
        db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Employee",
            Action = "CsvImported", Details = $"CSV dosyasından {employees.Count} çalışan içe aktarıldı." });
        try { await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.Conflict(new { message = "CSV aktarılmadı. Kayıt çakışması oluştu; önizlemeyi yenileyip tekrar deneyin." });
        }
        return Results.Ok(new { imported = employees.Count, message = $"{employees.Count:N0} çalışan başarıyla içe aktarıldı." });
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
        string[] legacyHeader = ["Etiket", "Ad", "Durum", "Kategori", "Marka", "Model", "Seri No", "Konum", "Personel No", "Çalışan", "Alış Tarihi", "Alış Bedeli", "Para Birimi"];
        string[] warrantyHeader = ["Etiket", "Ad", "Durum", "Kategori", "Marka", "Model", "Seri No", "Konum", "Personel No", "Çalışan", "Alış Tarihi", "Garanti Bitiş Tarihi", "Alış Bedeli", "Para Birimi"];
        var hasWarrantyColumn = rows.Count > 0 && rows[0].SequenceEqual(warrantyHeader, StringComparer.OrdinalIgnoreCase);
        var columnCount = hasWarrantyColumn ? warrantyHeader.Length : legacyHeader.Length;
        var costIndex = hasWarrantyColumn ? 12 : 11;
        var currencyIndex = hasWarrantyColumn ? 13 : 12;
        if (rows.Count is < 2 || rows.Count > CsvImportMaxRows + 1 ||
            (!hasWarrantyColumn && !rows[0].SequenceEqual(legacyHeader, StringComparer.OrdinalIgnoreCase)))
            return Results.UnprocessableEntity(new { message = $"EnvanterTR başlıklarıyla {CsvImportMaxRows:N0} veya daha az satır içeren bir CSV yükleyin." });

        var errors = new List<string>();
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (row, index) in rows.Skip(1).Select((row, index) => (row, index)))
        {
            if (row.Count != columnCount) { AddError(errors, $"{index + 2}. satırda {columnCount} sütun olmalıdır."); continue; }
            var tag = row[0].Trim();
            if (string.IsNullOrWhiteSpace(tag) || tag.Length > 80 || !tags.Add(tag)) AddError(errors, $"{index + 2}. satırda varlık etiketi boş, uzun veya yineleniyor.");
            if (string.IsNullOrWhiteSpace(row[1].Trim()) || row[1].Trim().Length > 240) AddError(errors, $"{index + 2}. satırda varlık adı boş veya çok uzun.");
            if (row[2] is not (AssetStatuses.Available or AssetStatuses.Deployed or AssetStatuses.Maintenance or AssetStatuses.Archived)) AddError(errors, $"{index + 2}. satırdaki durum desteklenmiyor.");
            if ((row[2] == AssetStatuses.Deployed) != !string.IsNullOrWhiteSpace(row[8])) AddError(errors, $"{index + 2}. satırda durum ve personel numarası eşleşmiyor.");
            if (string.IsNullOrWhiteSpace(row[3]) || row[3].Length > 120 || row[4].Length > 120 || row[5].Length > 120 || row[6].Length > 160 || row[7].Length > 160 || row[8].Length > 80 || row[9].Length > 200 || row[currencyIndex].Length != 3 || !row[currencyIndex].All(char.IsAsciiLetter))
                AddError(errors, $"{index + 2}. satırda alan uzunluğu, kategori veya para birimi geçersiz.");
            if (!string.IsNullOrWhiteSpace(row[10]) && !DateOnly.TryParseExact(row[10], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                AddError(errors, $"{index + 2}. satırdaki alış tarihi yyyy-AA-gg biçiminde olmalı.");
            if (hasWarrantyColumn && !string.IsNullOrWhiteSpace(row[11]) && !DateOnly.TryParseExact(row[11], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                AddError(errors, $"{index + 2}. satırdaki garanti bitiş tarihi yyyy-AA-gg biçiminde olmalı.");
            if (!string.IsNullOrWhiteSpace(row[costIndex]) && (!decimal.TryParse(row[costIndex], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var cost) || cost < 0 || cost > 9999999999999999.99m))
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
        var locationCatalogPaths = await AssetEndpoints.GetActiveLocationPathMapAsync(db, companyId, cancellationToken);
        var defaultAssetLocation = await AssetEndpoints.GetDefaultLocationPathAsync(db, companyId, cancellationToken);
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
                DateOnly? warrantyExpiresAt = hasWarrantyColumn && !string.IsNullOrWhiteSpace(row[11]) ? DateOnly.ParseExact(row[11], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : null;
                decimal? purchaseCost = string.IsNullOrWhiteSpace(row[costIndex]) ? null : decimal.Parse(row[costIndex], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture);
                var location = CleanCsv(row[7]) ?? defaultAssetLocation;
                var locationCatalogId = location is not null && locationCatalogPaths.TryGetValue(location, out var catalogLocationId)
                    ? catalogLocationId : (Guid?)null;
                importRows.Add((new Asset
                {
                    CompanyId = companyId, AssetTag = row[0].Trim().ToUpperInvariant(), Name = row[1].Trim(), Status = row[2],
                    CategoryId = categoryMatches[0].Id, BrandId = brandId, ModelId = modelId, SerialNumber = CleanCsv(row[6]),
                    Location = location, LocationCatalogId = locationCatalogId, AssignedEmployeeId = employeeId, PurchaseDate = purchaseDate, WarrantyExpiresAt = warrantyExpiresAt,
                    PurchaseCost = purchaseCost, Currency = row[currencyIndex].Trim().ToUpperInvariant()
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

        string[] legacyHeader = ["Etiket", "Ad", "Durum", "Kategori", "Marka", "Model", "Seri No", "Konum", "Personel No", "Çalışan", "Alış Tarihi", "Alış Bedeli", "Para Birimi"];
        string[] warrantyHeader = ["Etiket", "Ad", "Durum", "Kategori", "Marka", "Model", "Seri No", "Konum", "Personel No", "Çalışan", "Alış Tarihi", "Garanti Bitiş Tarihi", "Alış Bedeli", "Para Birimi"];
        var hasWarrantyColumn = records[0].SequenceEqual(warrantyHeader, StringComparer.OrdinalIgnoreCase);
        var columnCount = hasWarrantyColumn ? warrantyHeader.Length : legacyHeader.Length;
        var costIndex = hasWarrantyColumn ? 12 : 11;
        var currencyIndex = hasWarrantyColumn ? 13 : 12;
        if (!hasWarrantyColumn && !records[0].SequenceEqual(legacyHeader, StringComparer.OrdinalIgnoreCase))
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "CSV sütun başlıkları EnvanterTR varlık dışa aktarımıyla eşleşmiyor." } });

        var errors = new List<string>();
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (row, index) in records.Skip(1).Select((row, index) => (row, index)))
        {
            if (row.Count != columnCount) { AddError(errors, $"{index + 2}. satırda {columnCount} sütun olmalıdır."); continue; }
            var tag = row[0].Trim();
            if (string.IsNullOrWhiteSpace(tag) || tag.Length > 80) AddError(errors, $"{index + 2}. satırda varlık etiketi boş veya 80 karakterden uzun.");
            else if (!tags.Add(tag)) AddError(errors, "CSV içinde yinelenen varlık etiketi var.");
            if (string.IsNullOrWhiteSpace(row[1].Trim()) || row[1].Trim().Length > 240) AddError(errors, $"{index + 2}. satırda varlık adı boş veya 240 karakterden uzun.");
            if (row[2] is not (AssetStatuses.Available or AssetStatuses.Deployed or AssetStatuses.Maintenance or AssetStatuses.Archived)) AddError(errors, $"{index + 2}. satırdaki durum desteklenmiyor.");
            if ((row[2] == AssetStatuses.Deployed) != !string.IsNullOrWhiteSpace(row[8])) AddError(errors, $"{index + 2}. satırda atanmış durumuyla personel bilgisi eşleşmiyor.");
            if (row[3].Length > 120 || row[4].Length > 120 || row[5].Length > 120 || row[6].Length > 160 || row[7].Length > 160 || row[8].Length > 80 || row[9].Length > 200 || row[currencyIndex].Length != 3 || !row[currencyIndex].All(char.IsAsciiLetter))
                AddError(errors, $"{index + 2}. satırda alan uzunluğu veya para birimi biçimi geçersiz.");
            if (!string.IsNullOrWhiteSpace(row[10]) && !DateOnly.TryParseExact(row[10], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                AddError(errors, $"{index + 2}. satırdaki alış tarihi yyyy-AA-gg biçiminde olmalı.");
            if (hasWarrantyColumn && !string.IsNullOrWhiteSpace(row[11]) && !DateOnly.TryParseExact(row[11], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                AddError(errors, $"{index + 2}. satırdaki garanti bitiş tarihi yyyy-AA-gg biçiminde olmalı.");
            if (!string.IsNullOrWhiteSpace(row[costIndex]) && (!decimal.TryParse(row[costIndex], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var cost) || cost < 0 || cost > 9999999999999999.99m))
                AddError(errors, $"{index + 2}. satırdaki alış bedeli geçersiz veya negatif.");
        }

        var validRows = records.Skip(1).Select((row, index) => (row, index)).Where(item => item.row.Count == columnCount).ToArray();
        var categoryNames = validRows.Select(item => item.row[3].Trim()).Where(value => value.Length > 0).Select(value => value.ToUpperInvariant()).Distinct().ToArray();
        var brandNames = validRows.Select(item => item.row[4].Trim()).Where(value => value.Length > 0).Select(value => value.ToUpperInvariant()).Distinct().ToArray();
        var modelNames = validRows.Select(item => item.row[5].Trim()).Where(value => value.Length > 0).Select(value => value.ToUpperInvariant()).Distinct().ToArray();
        var employeeNumbers = validRows.Select(item => item.row[8].Trim()).Where(value => value.Length > 0).Select(value => value.ToUpperInvariant()).Distinct().ToArray();
        var availableCategories = await db.AssetCategories.AsNoTracking().Where(item => item.CompanyId == companyId && categoryNames.Contains(item.Name.ToUpper()))
            .Select(item => item.Name).ToListAsync(cancellationToken);
        var availableBrands = await db.AssetBrands.AsNoTracking().Where(item => item.CompanyId == companyId && brandNames.Contains(item.Name.ToUpper()))
            .Select(item => item.Name).ToListAsync(cancellationToken);
        var matchedBrandIds = await db.AssetBrands.AsNoTracking().Where(item => item.CompanyId == companyId && brandNames.Contains(item.Name.ToUpper()))
            .Select(item => new { item.Id, item.Name }).ToListAsync(cancellationToken);
        var matchedModels = await db.AssetModels.AsNoTracking().Where(item => item.CompanyId == companyId && modelNames.Contains(item.Name.ToUpper()))
            .Select(item => new { item.BrandId, item.Name }).ToListAsync(cancellationToken);
        var matchedEmployees = await db.Employees.AsNoTracking().Where(item => item.CompanyId == companyId && employeeNumbers.Contains(item.EmployeeNumber.ToUpper()))
            .Select(item => new { item.EmployeeNumber, item.DisplayName }).ToListAsync(cancellationToken);
        var categoryCounts = availableCategories.GroupBy(value => value.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        var brandCounts = availableBrands.GroupBy(value => value.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        var brandIds = matchedBrandIds.GroupBy(item => item.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Id).ToArray(), StringComparer.OrdinalIgnoreCase);
        var employeeLookup = matchedEmployees.GroupBy(item => item.EmployeeNumber.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Select(item => item.DisplayName.Trim()).ToArray(), StringComparer.OrdinalIgnoreCase);
        var modelLookup = matchedModels.Select(item => (item.BrandId, Name: item.Name.Trim())).ToHashSet(new BrandModelComparer());
        foreach (var (row, index) in validRows)
        {
            if (string.IsNullOrWhiteSpace(row[3]) || !categoryCounts.TryGetValue(row[3].Trim(), out var categoryCount) || categoryCount != 1)
                AddError(errors, $"{index + 2}. satırdaki kategori bu şirkette tekil olarak bulunamadı.");
            Guid? brandId = null;
            if (!string.IsNullOrWhiteSpace(row[4]))
            {
                if (!brandCounts.TryGetValue(row[4].Trim(), out var brandCount) || brandCount != 1 || !brandIds.TryGetValue(row[4].Trim(), out var ids) || ids.Length != 1)
                    AddError(errors, $"{index + 2}. satırdaki marka bu şirkette tekil olarak bulunamadı.");
                else brandId = ids[0];
            }
            if (!string.IsNullOrWhiteSpace(row[5]) && (brandId is null || !modelLookup.Contains((brandId, row[5].Trim()))))
                AddError(errors, $"{index + 2}. satırdaki model seçilen marka altında bulunamadı.");
            if (!string.IsNullOrWhiteSpace(row[8]) && (!employeeLookup.TryGetValue(row[8].Trim(), out var names) ||
                names.Length != 1 || !string.Equals(names[0], row[9].Trim(), StringComparison.OrdinalIgnoreCase)))
                AddError(errors, $"{index + 2}. satırdaki çalışan numarası ve adı bu şirkette eşleşmiyor.");
        }

        var normalizedTags = tags.Select(tag => tag.ToUpperInvariant()).ToArray();
        var existingMatches = 0;
        foreach (var batch in normalizedTags.Chunk(500))
            existingMatches += await db.Assets.AsNoTracking()
                .CountAsync(asset => asset.CompanyId == companyId && batch.Contains(asset.AssetTag.ToUpper()), cancellationToken);
        var targetName = await db.Companies.AsNoTracking().Where(c => c.Id == companyId).Select(c => c.Name).SingleAsync(cancellationToken);
        var defaultAssetLocation = await AssetEndpoints.GetDefaultLocationPathAsync(db, companyId, cancellationToken);
        var defaultLocationApplied = defaultAssetLocation is null ? 0 : validRows.Count(item => string.IsNullOrWhiteSpace(item.row[7]));
        return Results.Ok(new
        {
            isValid = errors.Count == 0,
            sourceCompany = "CSV dosyası",
            targetCompany = targetName,
            schemaVersion = hasWarrantyColumn ? 2 : 1,
            counts = new { assets = records.Count - 1 },
            defaultLocationApplied,
            existingMatches = new { assets = existingMatches },
            errors = errors.Take(30).ToArray(),
            warning = $"Varsayılan konum {defaultLocationApplied:N0} boş konumlu varlığa uygulanacak. Önizleme kategori, marka, model ve çalışan eşleşmelerini denetler. Var olan kayıtlar birleştirilmez; bu aşamada hiçbir kayıt değiştirilmez."
        });
    }

    private static List<List<string>> ParseSemicolonCsv(string text, int maximumRecords, char delimiter = ';')
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
            if (afterQuote && value != delimiter && value is not ('\r' or '\n')) throw new FormatException();
            if (value == '"')
            {
                if (cell.Length != 0) throw new FormatException();
                quoted = true;
            }
            else if (value == delimiter) { row.Add(cell.ToString()); cell.Clear(); afterQuote = false; }
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
            var departmentCatalog = ReadOptionalRows(root, "departmentCatalog", errors, counts);
            var locationCatalog = ReadOptionalRows(root, "locationCatalog", errors, counts);
            var locationCatalogIds = ValidateCompanyCatalogs(departmentCatalog, locationCatalog, sourceCompanyId, employees, errors);
            foreach (var employee in EnumerateRows(root, "employees"))
            {
                if (employee.ValueKind != JsonValueKind.Object) continue;
                if (employee.TryGetProperty("locationCatalogId", out var locationCatalogId) && locationCatalogId.ValueKind != JsonValueKind.Null &&
                    (!TryGuid(employee, "locationCatalogId", out var selectedLocationId) || !locationCatalogIds.Contains(selectedLocationId)))
                    AddError(errors, "Bir çalışanın konum kataloğu bağlantısı dosyada bulunamadı veya geçersiz.");
            }
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
                if (asset.TryGetProperty("locationCatalogId", out var locationCatalogId) && locationCatalogId.ValueKind != JsonValueKind.Null &&
                    (!TryGuid(asset, "locationCatalogId", out var selectedLocationId) || !locationCatalogIds.Contains(selectedLocationId)))
                    AddError(errors, "Bir varlığın konum kataloğu bağlantısı dosyada bulunamadı veya geçersiz.");
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
                if (asset.TryGetProperty("warrantyExpiresAt", out var warrantyDate) && warrantyDate.ValueKind != JsonValueKind.Null &&
                    (warrantyDate.ValueKind != JsonValueKind.String || !DateOnly.TryParseExact(warrantyDate.GetString(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _)))
                    AddError(errors, "Bir varlığın garanti bitiş tarihi yyyy-AA-gg biçiminde olmalı.");
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

    private static List<JsonElement> ReadOptionalRows(JsonElement root, string name,
        List<string> errors, Dictionary<string, int> counts)
    {
        if (!root.TryGetProperty(name, out var rows))
        {
            counts[name] = 0; // Older schema-version-1 exports predate these catalogs.
            return [];
        }
        if (rows.ValueKind != JsonValueKind.Array)
        {
            AddError(errors, $"{name} alanı dizi değil.");
            counts[name] = 0;
            return [];
        }
        counts[name] = rows.GetArrayLength();
        return rows.EnumerateArray().ToList();
    }

    private static HashSet<Guid> ValidateCompanyCatalogs(IReadOnlyList<JsonElement> departmentRows,
        IReadOnlyList<JsonElement> locationRows, Guid sourceCompanyId, IReadOnlySet<Guid> employeeIds, List<string> errors)
    {
        var departmentIds = new HashSet<Guid>();
        var departmentNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in departmentRows)
        {
            if (row.ValueKind != JsonValueKind.Object || !TryGuid(row, "id", out var id) ||
                !departmentIds.Add(id) || !TryGuid(row, "companyId", out var companyId) || companyId != sourceCompanyId)
            {
                AddError(errors, "Departman kataloğunda yinelenen kimlik veya kaynak şirketle uyuşmayan kayıt var.");
                continue;
            }
            var name = ReadString(row, "name")?.Trim();
            var normalized = ReadString(row, "normalizedName");
            if (string.IsNullOrWhiteSpace(name) || name.Length > 160 ||
                !string.Equals(normalized, name.ToUpperInvariant(), StringComparison.Ordinal) ||
                !departmentNames.Add(name.ToUpperInvariant()) ||
                !row.TryGetProperty("isActive", out var active) || active.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                AddError(errors, "Departman kataloğunda geçersiz, yinelenen veya etkinlik durumu bozuk kayıt var.");
        }

        var locations = new Dictionary<Guid, (Guid? ParentId, string Name, string NormalizedName)>();
        var siblings = new HashSet<string>(StringComparer.Ordinal);
        var defaultCount = 0;
        foreach (var row in locationRows)
        {
            if (row.ValueKind != JsonValueKind.Object || !TryGuid(row, "id", out var id) ||
                locations.ContainsKey(id) || !TryGuid(row, "companyId", out var companyId) || companyId != sourceCompanyId)
            {
                AddError(errors, "Konum kataloğunda yinelenen kimlik veya kaynak şirketle uyuşmayan kayıt var.");
                continue;
            }
            Guid? parentId = null;
            if (row.TryGetProperty("parentLocationId", out var parent) && parent.ValueKind != JsonValueKind.Null)
            {
                if (parent.ValueKind != JsonValueKind.String || !Guid.TryParse(parent.GetString(), out var parsedParent))
                {
                    AddError(errors, "Konum kataloğunda geçersiz üst konum kimliği var.");
                    continue;
                }
                parentId = parsedParent;
            }
            var name = ReadString(row, "name")?.Trim();
            var normalized = ReadString(row, "normalizedName");
            if (string.IsNullOrWhiteSpace(name) || name.Length > 120 || name.Contains('/') || name.Contains('\\') ||
                name.Any(char.IsControl) || !string.Equals(normalized, name.ToUpperInvariant(), StringComparison.Ordinal) ||
                !row.TryGetProperty("isActive", out var active) || active.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                AddError(errors, "Konum kataloğunda geçersiz ad, normalleştirilmiş ad veya etkinlik durumu var.");
                continue;
            }
            var siblingKey = (parentId?.ToString() ?? "root") + ":" + normalized;
            if (!siblings.Add(siblingKey))
                AddError(errors, "Konum kataloğunda aynı üst düğüm altında yinelenen ad var.");
            if (row.TryGetProperty("isDefault", out var isDefault))
            {
                if (isDefault.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    AddError(errors, "Konum kataloğunda varsayılan durumu geçersiz.");
                else if (isDefault.GetBoolean())
                {
                    defaultCount++;
                    if (!row.TryGetProperty("isActive", out active) || active.ValueKind != JsonValueKind.True)
                        AddError(errors, "Arşivlenmiş konum varsayılan olarak işaretlenemez.");
                }
            }
            if (row.TryGetProperty("managerEmployeeId", out var managerId) && managerId.ValueKind != JsonValueKind.Null &&
                (!TryGuid(row, "managerEmployeeId", out var selectedManagerId) || !employeeIds.Contains(selectedManagerId)))
                AddError(errors, "Konum sorumlusu çalışanı dosyada bulunamadı veya geçersiz.");
            locations.Add(id, (parentId, name, normalized!));
        }
        if (defaultCount > 1) AddError(errors, "Bir şirket konum kataloğunda en fazla bir varsayılan konum olabilir.");

        foreach (var (id, location) in locations)
        {
            var names = new List<string> { location.Name };
            var seen = new HashSet<Guid> { id };
            var parentId = location.ParentId;
            while (parentId is Guid parent)
            {
                if (!seen.Add(parent) || !locations.TryGetValue(parent, out var parentLocation))
                {
                    AddError(errors, "Konum kataloğunda döngü veya dosyada bulunmayan üst konum var.");
                    break;
                }
                names.Add(parentLocation.Name);
                parentId = parentLocation.ParentId;
                if (names.Count > 3)
                {
                    AddError(errors, "Konum kataloğu bina, kat ve oda olmak üzere en fazla üç seviyeli olabilir.");
                    break;
                }
            }
            if (string.Join(" / ", names.AsEnumerable().Reverse()).Length > 160)
                AddError(errors, "Konum kataloğunda tam gösterim yolu 160 karakteri aşamaz.");
        }
        return locations.Keys.ToHashSet();
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

        await WriteCompanyJsonAsync(writer, db, company, companyId, cancellationToken);
        await writer.FlushAsync(cancellationToken);
        return Results.Empty;
    }

    private static async Task<IResult> ExportCompanyArchiveAsync(HttpContext context, AppDbContext db,
        IConfiguration configuration, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(context.User.FindFirstValue("company_id"), out var companyId) ||
            !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var role = context.User.FindFirstValue("company_role");
        if (!await db.Companies.AsNoTracking().AnyAsync(c => c.Id == companyId && c.IsActive &&
            db.CompanyMemberships.Any(m => m.CompanyId == companyId && m.UserId == userId && m.RoleKey == role), cancellationToken)) return Results.Forbid();
        var company = await db.Companies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == companyId, cancellationToken);
        if (company is null) return Results.NotFound();

        var mediaRoot = Path.GetFullPath(configuration["Storage:CompanyMediaPath"] ?? "/var/lib/envantertr/company-media");
        var companyDirectory = Path.GetFullPath(Path.Combine(mediaRoot, companyId.ToString("N")));
        var pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!companyDirectory.StartsWith(mediaRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, pathComparison))
            return Results.Problem("Medya dizini geçersiz.", statusCode: StatusCodes.Status500InternalServerError);
        if (Directory.Exists(companyDirectory) && (File.GetAttributes(companyDirectory) & FileAttributes.ReparsePoint) != 0)
            return Results.Problem("Şirket medya dizini sembolik bağlantı olamaz.", statusCode: StatusCodes.Status500InternalServerError);
        var files = new List<(string Source, string Entry)>(); long totalBytes = 0;
        if (Directory.Exists(companyDirectory))
        {
            foreach (var file in EnumerateMediaFiles(companyDirectory))
            {
                var relative = Path.GetRelativePath(companyDirectory, file).Replace(Path.DirectorySeparatorChar, '/');
                if (relative.Split('/').Any(part => part is ".." or ".")) continue;
                files.Add((file, "media/" + relative)); totalBytes += new FileInfo(file).Length;
                if (files.Count > 100_000 || totalBytes > 4L * 1024 * 1024 * 1024)
                    return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            }
        }
        var accountMediaRoot = Path.Combine(mediaRoot, "accounts");
        var memberUserIds = await db.CompanyMemberships.AsNoTracking().Where(x => x.CompanyId == companyId)
            .Select(x => x.UserId).Distinct().ToListAsync(cancellationToken);
        foreach (var memberUserId in memberUserIds)
        {
            var profileDirectory = Path.Combine(accountMediaRoot, memberUserId.ToString("N"));
            if (!Directory.Exists(profileDirectory) || (File.GetAttributes(profileDirectory) & FileAttributes.ReparsePoint) != 0) continue;
            foreach (var extension in new[] { "png", "jpg", "webp" })
            {
                var profile = Path.Combine(profileDirectory, $"profile.{extension}");
                if (!File.Exists(profile) || (File.GetAttributes(profile) & FileAttributes.ReparsePoint) != 0) continue;
                files.Add((profile, $"media/account-profiles/{memberUserId:N}/profile.{extension}"));
                totalBytes += new FileInfo(profile).Length;
                if (files.Count > 100_000 || totalBytes > 4L * 1024 * 1024 * 1024)
                    return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            }
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/zip";
        context.Response.Headers.CacheControl = "no-store, private";
        context.Response.Headers.ContentDisposition = $"attachment; filename=envantertr-sirket-arsivi-{DateTime.UtcNow:yyyyMMdd}.zip";
        using (var archive = new ZipArchive(context.Response.Body, ZipArchiveMode.Create, leaveOpen: true))
        {
            var jsonEntry = archive.CreateEntry("company-data.json", CompressionLevel.Fastest);
            await using (var jsonStream = jsonEntry.Open())
            using (var writer = new Utf8JsonWriter(jsonStream, new JsonWriterOptions { Indented = false }))
            {
                await WriteCompanyJsonAsync(writer, db, company, companyId, cancellationToken);
                await writer.FlushAsync(cancellationToken);
            }
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = archive.CreateEntry(file.Entry, CompressionLevel.Fastest);
                await using var output = entry.Open();
                await using var input = new FileStream(file.Source, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                await input.CopyToAsync(output, cancellationToken);
            }
            var readme = archive.CreateEntry("README.txt", CompressionLevel.Fastest);
            await using var readmeStream = readme.Open();
            await using var readmeWriter = new StreamWriter(readmeStream, new UTF8Encoding(false));
            await readmeWriter.WriteAsync("EnvanterTR şirket veri arşivi\r\ncompany-data.json şirket kayıtlarının bir bölümünü, departman ve konum kataloglarını içerir. media/ klasörü şirket logolarını, varlık dosyalarını, çalışan fotoğraflarını/belgelerini ve bu şirkete üye hesapların profil fotoğraflarını içerir. Bu arşiv tam sistem yedeği veya geri yükleme paketi değildir. Kimlik bilgileri ve oturumlar dahil edilmez.\r\n");
        }
        return Results.Empty;
    }

    private static async Task WriteCompanyJsonAsync(Utf8JsonWriter writer, AppDbContext db, Company company,
        Guid companyId, CancellationToken cancellationToken)
    {
        writer.WriteStartObject();
        writer.WriteString("format", "envantertr-company-data");
        writer.WriteNumber("schemaVersion", 1);
        writer.WriteString("exportedAt", DateTimeOffset.UtcNow);
        writer.WritePropertyName("company");
        JsonSerializer.Serialize(writer, new { company.Id, company.Name, company.Slug, company.ContactEmail,
            company.BrandLogoPath, company.BrandPrimaryColor, company.LabelPrintSettingsJson, company.CreatedAt }, JsonOptions);
        await WritePagedAsync(writer, "departmentCatalog", db.CompanyDepartments.AsNoTracking()
            .Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        await WritePagedAsync(writer, "locationCatalog", db.CompanyLocations.AsNoTracking()
            .Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        await WritePagedAsync(writer, "employees", db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        await WritePagedAsync(writer, "assetCategories", db.AssetCategories.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        await WritePagedAsync(writer, "assetBrands", db.AssetBrands.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        await WritePagedAsync(writer, "assetModels", db.AssetModels.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        await WritePagedAsync(writer, "assets", db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        await WritePagedAsync(writer, "inventoryRequests", db.InventoryRequests.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        await WritePagedAsync(writer, "assetMaintenances", db.AssetMaintenances.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        await WritePagedAsync(writer, "assetEvents", db.AssetEvents.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        await WritePagedAsync(writer, "auditEntries", db.AuditEntries.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken);
        var members = from membership in db.CompanyMemberships.AsNoTracking().Where(x => x.CompanyId == companyId)
                      join user in db.Users.AsNoTracking() on membership.UserId equals user.Id
                      orderby membership.UserId
                      select new { UserId = user.Id, user.DisplayName, Email = user.Email!, membership.RoleKey, membership.CreatedAt };
        await WritePagedAsync(writer, "members", members, cancellationToken);
        writer.WriteEndObject();
    }

    private static IEnumerable<string> EnumerateMediaFiles(string root)
    {
        var pending = new Stack<string>(); pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var child in Directory.EnumerateDirectories(directory))
                if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0) pending.Push(child);
            foreach (var file in Directory.EnumerateFiles(directory))
                if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) == 0) yield return file;
        }
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
