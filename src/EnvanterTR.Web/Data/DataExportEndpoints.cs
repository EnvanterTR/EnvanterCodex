using System.Data;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Linq;
using System.Text.Json;
using System.Text;
using System.IO.Compression;
using System.IO.Pipelines;
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
        endpoints.MapPost("/api/company/import/apply", ApplyCompanyImportAsync)
            .RequireAuthorization("settings.manage")
            .WithMetadata(new RequestSizeLimitAttribute(PreviewMaxBytes));
        endpoints.MapPost("/api/assets/import/preview.csv", PreviewAssetCsvAsync)
            .RequireAuthorization("asset.write")
            .WithMetadata(new RequestSizeLimitAttribute(PreviewMaxBytes));
        endpoints.MapPost("/api/assets/import/preview.xlsx", PreviewAssetCsvAsync)
            .RequireAuthorization("asset.write")
            .WithMetadata(new RequestSizeLimitAttribute(PreviewMaxBytes));
        endpoints.MapPost("/api/assets/import.csv", ImportAssetCsvAsync)
            .RequireAuthorization("asset.write")
            .WithMetadata(new RequestSizeLimitAttribute(PreviewMaxBytes));
        endpoints.MapPost("/api/assets/import.xlsx", ImportAssetCsvAsync)
            .RequireAuthorization("asset.write")
            .WithMetadata(new RequestSizeLimitAttribute(PreviewMaxBytes));
        endpoints.MapPost("/api/employees/import/preview.csv", (HttpContext context, AppDbContext db,
            IAntiforgery antiforgery, CancellationToken cancellationToken) => ImportEmployeeCsvAsync(context, db, antiforgery, false, cancellationToken))
            .RequireAuthorization("people.manage")
            .WithMetadata(new RequestSizeLimitAttribute(PreviewMaxBytes));
        endpoints.MapPost("/api/employees/import/preview.xlsx", (HttpContext context, AppDbContext db,
            IAntiforgery antiforgery, CancellationToken cancellationToken) => ImportEmployeeCsvAsync(context, db, antiforgery, false, cancellationToken))
            .RequireAuthorization("people.manage")
            .WithMetadata(new RequestSizeLimitAttribute(PreviewMaxBytes));
        endpoints.MapPost("/api/employees/import.csv", (HttpContext context, AppDbContext db,
            IAntiforgery antiforgery, CancellationToken cancellationToken) => ImportEmployeeCsvAsync(context, db, antiforgery, true, cancellationToken))
            .RequireAuthorization("people.manage")
            .WithMetadata(new RequestSizeLimitAttribute(PreviewMaxBytes));
        endpoints.MapPost("/api/employees/import.xlsx", (HttpContext context, AppDbContext db,
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
        var isXlsx = IsXlsxRequest(context.Request);
        if (!isXlsx && context.Request.ContentType?.StartsWith("text/csv", StringComparison.OrdinalIgnoreCase) != true)
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
        List<List<string>> rows;
        try
        {
            if (isXlsx) rows = AssetXlsxImportParser.Parse(buffer, CsvImportMaxRows + 2);
            else
            {
                var text = new System.Text.UTF8Encoding(false, true).GetString(buffer.ToArray()).TrimStart('\uFEFF');
                rows = ParseSemicolonCsv(text, CsvImportMaxRows + 2, ',');
            }
        }
        catch (System.Text.DecoderFallbackException)
        { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "CSV dosyası UTF-8 kodlamasında olmalıdır." } }); }
        catch (FormatException)
        { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "CSV tırnak/ayraç biçimi bozuk." } }); }
        catch (Exception exception) when (isXlsx && exception is InvalidDataException or InvalidOperationException or System.Xml.XmlException or OverflowException)
        { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { exception.Message } }); }
        string[] baseHeader = ["Personel No", "Ad Soyad", "E-posta", "Departman", "Görev Unvanı", "Telefon", "Konum", "İşe Başlama Tarihi"];
        string[] legacyExportHeader = [.. baseHeader, "Yönetici", "Durum", "Kayıt Tarihi"];
        string[] exportHeader = [.. legacyExportHeader, "Yönetici Personel No"];
        var hasRoundTripManagerColumn = rows.Count > 0 && rows[0].SequenceEqual(exportHeader, StringComparer.OrdinalIgnoreCase);
        var hasExportColumns = hasRoundTripManagerColumn || rows.Count > 0 && rows[0].SequenceEqual(legacyExportHeader, StringComparer.OrdinalIgnoreCase);
        if (rows.Count is < 2 || rows.Count > CsvImportMaxRows + 1 ||
            (!hasExportColumns && !rows[0].SequenceEqual(baseHeader, StringComparer.OrdinalIgnoreCase)))
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { $"Çalışan şablonunu kullanın veya uygulamadan alınan çalışan CSV'sini yükleyin. En fazla {CsvImportMaxRows:N0} satır aktarılabilir." } });

        var errors = new List<string>();
        var seenNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var employees = new List<Employee>(rows.Count - 1);
        var managerNumbers = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (row, index) in rows.Skip(1).Select((row, index) => (row, index)))
        {
            var line = index + 2;
            var expectedColumns = hasExportColumns ? exportHeader.Length : baseHeader.Length;
            if (row.Count != expectedColumns) { AddError(errors, $"{line}. satırda {expectedColumns} sütun olmalıdır."); continue; }
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
            var employee = new Employee { CompanyId = companyId, EmployeeNumber = number, DisplayName = name,
                Email = email, Department = department, JobTitle = jobTitle, Phone = phone, Location = location, StartedOn = startedOn };
            if (hasExportColumns)
            {
                var activeText = row[9].Trim();
                if (activeText.Equals("Etkin", StringComparison.OrdinalIgnoreCase)) employee.IsActive = true;
                else if (activeText.Equals("Pasif", StringComparison.OrdinalIgnoreCase)) employee.IsActive = false;
                else AddError(errors, $"{line}. satırdaki durum Etkin veya Pasif olmalıdır.");
                var managerNumber = hasRoundTripManagerColumn ? CleanCsv(row[11])?.ToUpperInvariant() : null;
                if (managerNumber is { Length: > 80 }) AddError(errors, $"{line}. satırdaki yönetici personel numarası 80 karakteri aşamaz.");
                managerNumbers[number] = managerNumber;
            }
            employees.Add(employee);
        }
        var numbers = seenNumbers.ToArray();
        var existingNumbers = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && numbers.Contains(x.EmployeeNumber.ToUpper()))
            .Select(x => x.EmployeeNumber).ToListAsync(cancellationToken);
        if (existingNumbers.Count > 0)
            AddError(errors, $"Şirkette zaten kayıtlı personel numaraları var: {string.Join(", ", existingNumbers.Take(10))}{(existingNumbers.Count > 10 ? "…" : "")}");

        if (hasExportColumns && hasRoundTripManagerColumn)
        {
            var managerNumberList = managerNumbers.Values.Where(x => x is not null).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var existingManagers = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && managerNumberList.Contains(x.EmployeeNumber.ToUpper()))
                .Select(x => new { x.Id, x.EmployeeNumber, x.IsActive, x.ManagerEmployeeId }).ToListAsync(cancellationToken);
            var importedByNumber = employees.GroupBy(x => x.EmployeeNumber, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var existingByNumber = existingManagers.ToDictionary(x => x.EmployeeNumber, StringComparer.OrdinalIgnoreCase);
            foreach (var employee in employees)
            {
                if (!managerNumbers.TryGetValue(employee.EmployeeNumber, out var managerNumber) || managerNumber is null) continue;
                if (string.Equals(employee.EmployeeNumber, managerNumber, StringComparison.OrdinalIgnoreCase))
                { AddError(errors, $"{employee.EmployeeNumber} numaralı çalışan kendisini yönetici seçemez."); continue; }
                if (importedByNumber.TryGetValue(managerNumber, out var importedManager))
                {
                    if (!importedManager.IsActive) { AddError(errors, $"{employee.EmployeeNumber} için yönetici olarak seçilen {managerNumber} pasif."); continue; }
                    employee.ManagerEmployeeId = importedManager.Id;
                }
                else if (existingByNumber.TryGetValue(managerNumber, out var existingManager) && existingManager.IsActive)
                    employee.ManagerEmployeeId = existingManager.Id;
                else AddError(errors, $"{employee.EmployeeNumber} için {managerNumber} numaralı etkin yönetici aynı şirket dosyasında veya kayıtlarında bulunamadı.");
            }
            if (errors.Count == 0)
            {
                var hierarchy = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId)
                    .Select(x => new { x.Id, x.ManagerEmployeeId }).ToDictionaryAsync(x => x.Id, x => x.ManagerEmployeeId, cancellationToken);
                foreach (var employee in employees) hierarchy[employee.Id] = employee.ManagerEmployeeId;
                var cycleDetected = false;
                foreach (var employee in employees)
                {
                    var visited = new HashSet<Guid>();
                    Guid? ancestor = employee.Id;
                    while (ancestor is Guid ancestorId && hierarchy.TryGetValue(ancestorId, out var next))
                    {
                        if (!visited.Add(ancestorId)) { cycleDetected = true; break; }
                        ancestor = next;
                    }
                    if (cycleDetected) break;
                }
                if (cycleDetected) AddError(errors, "Yönetici personel numaraları çalışan-yönetici döngüsü oluşturuyor.");
            }
        }
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
        if (hasRoundTripManagerColumn)
        {
            var importedIds = employees.Select(x => x.Id).ToHashSet();
            var externalManagerIds = employees.Where(x => x.ManagerEmployeeId.HasValue && !importedIds.Contains(x.ManagerEmployeeId.Value))
                .Select(x => x.ManagerEmployeeId!.Value).Distinct().ToArray();
            var activeManagerCount = await db.Employees.CountAsync(x => x.CompanyId == companyId && x.IsActive && externalManagerIds.Contains(x.Id), cancellationToken);
            if (activeManagerCount != externalManagerIds.Length)
                return Results.Conflict(new { message = "Önizlemeden sonra bir yönetici kaydı pasife alınmış veya kaldırılmış. Hiçbir kayıt aktarılmadı; önizlemeyi yenileyin." });
        }
        db.Employees.AddRange(employees);
        foreach (var employee in employees)
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Employee",
                TargetId = employee.Id, Action = "Created", Details = $"Personel no: {employee.EmployeeNumber}; {(isXlsx ? "Excel" : "CSV")} içe aktarımı." });
        db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Employee",
            Action = isXlsx ? "XlsxImported" : "CsvImported", Details = $"{(isXlsx ? "Excel" : "CSV")} dosyasından {employees.Count} çalışan içe aktarıldı." });
        try { await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.Conflict(new { message = $"{(isXlsx ? "Excel" : "CSV")} aktarılmadı. Kayıt çakışması oluştu; önizlemeyi yenileyip tekrar deneyin." });
        }
        return Results.Ok(new { imported = employees.Count, message = $"{employees.Count:N0} çalışan başarıyla {(isXlsx ? "Excel" : "CSV")} dosyasından içe aktarıldı." });
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
        var isXlsx = IsXlsxRequest(context.Request);
        if (!isXlsx && context.Request.ContentType?.StartsWith("text/csv", StringComparison.OrdinalIgnoreCase) != true)
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
        List<List<string>> rows;
        try
        {
            if (isXlsx) rows = AssetXlsxImportParser.Parse(buffer, CsvImportMaxRows + 2);
            else
            {
                var text = new System.Text.UTF8Encoding(false, true).GetString(buffer.ToArray()).TrimStart('\uFEFF');
                rows = ParseAssetCsv(text, CsvImportMaxRows + 2);
            }
        }
        catch (System.Text.DecoderFallbackException)
        { return Results.UnprocessableEntity(new { message = "CSV dosyası UTF-8 kodlamasında olmalıdır." }); }
        catch (FormatException)
        { return Results.UnprocessableEntity(new { message = "CSV tırnak/ayraç biçimi bozuk." }); }
        catch (Exception exception) when (isXlsx && exception is InvalidDataException or InvalidOperationException or System.Xml.XmlException or OverflowException)
        { return Results.UnprocessableEntity(new { message = exception.Message }); }
        var customFields = await db.AssetCustomFieldDefinitions.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        rows = AssetCsvImportMapping.Normalize(rows, customFields, out var unsupportedColumns);
        if (unsupportedColumns.Length > 0)
            return Results.UnprocessableEntity(new { message = $"Bu dosyada henüz eşlenmeyen ve dolu veri içeren sütunlar var: {string.Join(", ", unsupportedColumns)}. Veri kaybını önlemek için aktarım durduruldu; önce bu sütunları eşleyin veya boş olduklarını doğrulayın." });
        string[] legacyHeader = ["Etiket", "Ad", "Durum", "Kategori", "Marka", "Model", "Seri No", "Konum", "Personel No", "Çalışan", "Alış Tarihi", "Alış Bedeli", "Para Birimi"];
        string[] warrantyHeader = ["Etiket", "Ad", "Durum", "Kategori", "Marka", "Model", "Seri No", "Konum", "Personel No", "Çalışan", "Alış Tarihi", "Garanti Bitiş Tarihi", "Alış Bedeli", "Para Birimi"];
        var legacyWithParent = legacyHeader.Append("Üst Varlık Etiketi").ToArray();
        var warrantyWithParent = warrantyHeader.Append("Üst Varlık Etiketi").ToArray();
        bool HeaderStartsWith(string[] expected) => rows.Count > 0 && rows[0].Count >= expected.Length && rows[0].Take(expected.Length).SequenceEqual(expected, StringComparer.OrdinalIgnoreCase);
        var hasParentColumn = HeaderStartsWith(legacyWithParent) || HeaderStartsWith(warrantyWithParent);
        var hasWarrantyColumn = HeaderStartsWith(warrantyHeader) || HeaderStartsWith(warrantyWithParent);
        var columnCount = hasParentColumn ? (hasWarrantyColumn ? warrantyWithParent.Length : legacyWithParent.Length) : (hasWarrantyColumn ? warrantyHeader.Length : legacyHeader.Length);
        var parentIndex = hasParentColumn ? columnCount - 1 : -1;
        var costIndex = hasWarrantyColumn ? 12 : 11;
        var currencyIndex = hasWarrantyColumn ? 13 : 12;
        var expectedBaseHeader = hasParentColumn ? (hasWarrantyColumn ? warrantyWithParent : legacyWithParent) : (hasWarrantyColumn ? warrantyHeader : legacyHeader);
        var expectedHeader = expectedBaseHeader.Concat(customFields.Select(field => $"Özel alan: {field.Key}")).ToArray();
        if (rows.Count is < 2 || rows.Count > CsvImportMaxRows + 1 || !rows[0].SequenceEqual(expectedHeader, StringComparer.OrdinalIgnoreCase))
            return Results.UnprocessableEntity(new { message = $"EnvanterTR başlıklarıyla {CsvImportMaxRows:N0} veya daha az satır içeren bir CSV yükleyin." });

        var errors = new List<string>();
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var customFieldsByRow = new Dictionary<int, string>();
        foreach (var (row, index) in rows.Skip(1).Select((row, index) => (row, index)))
        {
            if (row.Count != expectedHeader.Length) { AddError(errors, $"{index + 2}. satırda {expectedHeader.Length} sütun olmalıdır."); continue; }
            var submittedCustomFields = customFields.Select((field, fieldIndex) => (field.Key, Value: row[columnCount + fieldIndex].Trim()))
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            var normalizedCustomFields = AssetCustomFieldEndpoints.NormalizeValues(customFields, submittedCustomFields);
            if (normalizedCustomFields.Error is not null) AddError(errors, $"{index + 2}. satır: {normalizedCustomFields.Error}");
            else customFieldsByRow[index] = normalizedCustomFields.Json ?? "{}";
            var tag = row[0].Trim();
            if (string.IsNullOrWhiteSpace(tag) || tag.Length > 80 || !tags.Add(tag)) AddError(errors, $"{index + 2}. satırda varlık etiketi boş, uzun veya yineleniyor.");
            if (string.IsNullOrWhiteSpace(row[1].Trim()) || row[1].Trim().Length > 240) AddError(errors, $"{index + 2}. satırda varlık adı boş veya çok uzun.");
            if (row[2] is not (AssetStatuses.Available or AssetStatuses.Deployed or AssetStatuses.Pending or AssetStatuses.Maintenance or AssetStatuses.Unusable or AssetStatuses.Archived)) AddError(errors, $"{index + 2}. satırdaki durum desteklenmiyor.");
            if ((row[2] == AssetStatuses.Deployed) != !string.IsNullOrWhiteSpace(row[8])) AddError(errors, $"{index + 2}. satırda durum ve personel numarası eşleşmiyor.");
            if (string.IsNullOrWhiteSpace(row[3]) || row[3].Length > 120 || row[4].Length > 120 || row[5].Length > 120 || row[6].Length > 160 || row[7].Length > 160 || row[8].Length > 80 || row[9].Length > 200 || row[currencyIndex].Length != 3 || !row[currencyIndex].All(char.IsAsciiLetter))
                AddError(errors, $"{index + 2}. satırda alan uzunluğu, kategori veya para birimi geçersiz.");
            if (hasParentColumn && row[parentIndex].Length > 80) AddError(errors, $"{index + 2}. satırdaki üst varlık etiketi 80 karakteri aşamaz.");
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
        var parentTags = hasParentColumn ? rows.Skip(1).Select(row => row[parentIndex].Trim().ToUpperInvariant()).Where(tag => tag.Length > 0).Distinct(StringComparer.Ordinal).ToArray() : [];
        var parentLookup = parentTags.Length == 0 ? new Dictionary<string, (Guid Id, string Status)>(StringComparer.OrdinalIgnoreCase) :
            await db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId && x.Status != AssetStatuses.Archived && parentTags.Contains(x.AssetTag.ToUpper()))
                .Select(x => new { x.Id, x.AssetTag, x.Status }).ToDictionaryAsync(x => x.AssetTag.ToUpper(), x => (x.Id, x.Status), StringComparer.OrdinalIgnoreCase, cancellationToken);
        var locationCatalogPaths = await AssetEndpoints.GetActiveLocationPathMapAsync(db, companyId, cancellationToken);
        var defaultAssetLocation = await AssetEndpoints.GetDefaultLocationPathAsync(db, companyId, cancellationToken);
        var importRows = new List<(Asset Asset, Guid? EmployeeId, string? ParentTag)>(rows.Count - 1);
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
            var parentTag = hasParentColumn ? CleanCsv(row[parentIndex])?.ToUpperInvariant() : null;
            if (parentTag is not null && string.Equals(parentTag, row[0].Trim(), StringComparison.OrdinalIgnoreCase))
                AddError(errors, $"{index + 2}. satırdaki varlık kendisini üst varlık seçemez.");
            else if (parentTag is not null && !parentLookup.ContainsKey(parentTag) && !tags.Contains(parentTag))
                AddError(errors, $"{index + 2}. satırdaki üst varlık etiketi bu şirkette veya içe aktarma dosyasında bulunamadı.");
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
                    PurchaseCost = purchaseCost, Currency = row[currencyIndex].Trim().ToUpperInvariant(),
                    CustomFieldsJson = customFieldsByRow.GetValueOrDefault(index, "{}")
                }, employeeId, parentTag));
            }
        }
        var importedAssetsByTag = importRows.ToDictionary(x => x.Asset.AssetTag, x => x.Asset, StringComparer.OrdinalIgnoreCase);
        var parentEdges = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pendingParentAssignments = new List<(Asset Asset, Guid ParentId)>();
        foreach (var item in importRows)
        {
            if (item.ParentTag is null) continue;
            if (importedAssetsByTag.TryGetValue(item.ParentTag, out var importedParent))
            {
                if (importedParent.Status == AssetStatuses.Archived)
                    AddError(errors, $"CSV içindeki arşivlenmiş {item.ParentTag} varlığı üst varlık olamaz.");
                pendingParentAssignments.Add((item.Asset, importedParent.Id));
                parentEdges[item.Asset.AssetTag] = item.ParentTag;
            }
            else if (parentLookup.TryGetValue(item.ParentTag, out var existingParent)) pendingParentAssignments.Add((item.Asset, existingParent.Id));
        }
        foreach (var startTag in parentEdges.Keys)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var cursor = startTag;
            while (parentEdges.TryGetValue(cursor, out var parentTag))
            {
                if (!visited.Add(cursor)) { AddError(errors, $"CSV içe aktarma dosyası {startTag} varlığını içeren bir üst varlık döngüsü oluşturuyor."); break; }
                cursor = parentTag;
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
                EventType = "Created", Details = $"Varlık {(isXlsx ? "Excel" : "CSV")} dosyasından içe aktarıldı.", EmployeeId = item.EmployeeId });
        db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Asset", Action = isXlsx ? "XlsxImported" : "CsvImported",
            Details = $"{(isXlsx ? "Excel" : "CSV")} dosyasından {importRows.Count} varlık içe aktarıldı." });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            foreach (var assignment in pendingParentAssignments)
            {
                assignment.Asset.ParentAssetId = assignment.ParentId;
                assignment.Asset.UpdatedAt = DateTimeOffset.UtcNow;
            }
            if (pendingParentAssignments.Count > 0) await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.Conflict(new { message = $"{(isXlsx ? "Excel" : "CSV")} kaydedilmedi. Etiket veya ilişkili kayıtlar işlem sırasında değişmiş olabilir; önizlemeyi yenileyin." });
        }
        return Results.Ok(new { imported = importRows.Count, message = $"{importRows.Count:N0} varlık başarıyla {(isXlsx ? "Excel" : "CSV")} dosyasından içe aktarıldı." });
    }

    private static string? CleanCsv(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static bool IsXlsxRequest(HttpRequest request) =>
        request.ContentType?.StartsWith("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", StringComparison.OrdinalIgnoreCase) == true;

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
        var isXlsx = IsXlsxRequest(context.Request);
        if (!isXlsx && context.Request.ContentType?.StartsWith("text/csv", StringComparison.OrdinalIgnoreCase) != true)
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

        List<List<string>> records;
        try
        {
            if (isXlsx) records = AssetXlsxImportParser.Parse(buffer, CsvImportMaxRows + 2);
            else
            {
                var text = new System.Text.UTF8Encoding(false, true).GetString(buffer.ToArray()).TrimStart('\uFEFF');
                records = ParseAssetCsv(text, CsvImportMaxRows + 2);
            }
        }
        catch (System.Text.DecoderFallbackException)
        { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "CSV dosyası UTF-8 kodlamasında olmalıdır." } }); }
        catch (FormatException)
        { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "CSV tırnak/ayraç biçimi bozuk." } }); }
        catch (Exception exception) when (isXlsx && exception is InvalidDataException or InvalidOperationException or System.Xml.XmlException or OverflowException)
        { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { exception.Message } }); }
        var customFields = await db.AssetCustomFieldDefinitions.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
        records = AssetCsvImportMapping.Normalize(records, customFields, out var unsupportedColumns);
        if (unsupportedColumns.Length > 0)
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { $"Bu dosyada henüz eşlenmeyen ve dolu veri içeren sütunlar var: {string.Join(", ", unsupportedColumns)}. Veri kaybını önlemek için aktarım durduruldu; önce bu sütunları eşleyin veya boş olduklarını doğrulayın." } });
        if (records.Count is < 2 || records.Count > CsvImportMaxRows + 1)
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { $"CSV başlık ve en fazla {CsvImportMaxRows:N0} varlık satırı içermelidir." } });

        string[] legacyHeader = ["Etiket", "Ad", "Durum", "Kategori", "Marka", "Model", "Seri No", "Konum", "Personel No", "Çalışan", "Alış Tarihi", "Alış Bedeli", "Para Birimi"];
        string[] warrantyHeader = ["Etiket", "Ad", "Durum", "Kategori", "Marka", "Model", "Seri No", "Konum", "Personel No", "Çalışan", "Alış Tarihi", "Garanti Bitiş Tarihi", "Alış Bedeli", "Para Birimi"];
        var legacyWithParent = legacyHeader.Append("Üst Varlık Etiketi").ToArray();
        var warrantyWithParent = warrantyHeader.Append("Üst Varlık Etiketi").ToArray();
        var hasParentColumn = records[0].Take(legacyWithParent.Length).SequenceEqual(legacyWithParent, StringComparer.OrdinalIgnoreCase) || records[0].Take(warrantyWithParent.Length).SequenceEqual(warrantyWithParent, StringComparer.OrdinalIgnoreCase);
        var hasWarrantyColumn = records[0].Take(warrantyHeader.Length).SequenceEqual(warrantyHeader, StringComparer.OrdinalIgnoreCase) || records[0].Take(warrantyWithParent.Length).SequenceEqual(warrantyWithParent, StringComparer.OrdinalIgnoreCase);
        var baseColumnCount = hasParentColumn ? (hasWarrantyColumn ? warrantyWithParent.Length : legacyWithParent.Length) : (hasWarrantyColumn ? warrantyHeader.Length : legacyHeader.Length);
        var columnCount = baseColumnCount + customFields.Count;
        var parentIndex = hasParentColumn ? baseColumnCount - 1 : -1;
        var costIndex = hasWarrantyColumn ? 12 : 11;
        var currencyIndex = hasWarrantyColumn ? 13 : 12;
        var expectedHeader = records[0].Take(baseColumnCount).Concat(customFields.Select(field => $"Özel alan: {field.Key}")).ToArray();
        if (!records[0].SequenceEqual(expectedHeader, StringComparer.OrdinalIgnoreCase))
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
            if (row[2] is not (AssetStatuses.Available or AssetStatuses.Deployed or AssetStatuses.Pending or AssetStatuses.Maintenance or AssetStatuses.Unusable or AssetStatuses.Archived)) AddError(errors, $"{index + 2}. satırdaki durum desteklenmiyor.");
            if ((row[2] == AssetStatuses.Deployed) != !string.IsNullOrWhiteSpace(row[8])) AddError(errors, $"{index + 2}. satırda atanmış durumuyla personel bilgisi eşleşmiyor.");
            if (row[3].Length > 120 || row[4].Length > 120 || row[5].Length > 120 || row[6].Length > 160 || row[7].Length > 160 || row[8].Length > 80 || row[9].Length > 200 || row[currencyIndex].Length != 3 || !row[currencyIndex].All(char.IsAsciiLetter))
                AddError(errors, $"{index + 2}. satırda alan uzunluğu veya para birimi biçimi geçersiz.");
            if (hasParentColumn && row[parentIndex].Length > 80) AddError(errors, $"{index + 2}. satırdaki üst varlık etiketi 80 karakteri aşamaz.");
            if (!string.IsNullOrWhiteSpace(row[10]) && !DateOnly.TryParseExact(row[10], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                AddError(errors, $"{index + 2}. satırdaki alış tarihi yyyy-AA-gg biçiminde olmalı.");
            if (hasWarrantyColumn && !string.IsNullOrWhiteSpace(row[11]) && !DateOnly.TryParseExact(row[11], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                AddError(errors, $"{index + 2}. satırdaki garanti bitiş tarihi yyyy-AA-gg biçiminde olmalı.");
            if (!string.IsNullOrWhiteSpace(row[costIndex]) && (!decimal.TryParse(row[costIndex], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var cost) || cost < 0 || cost > 9999999999999999.99m))
                AddError(errors, $"{index + 2}. satırdaki alış bedeli geçersiz veya negatif.");
            var submittedCustomFields = customFields.Select((field, fieldIndex) => (field.Key, Value: row[baseColumnCount + fieldIndex].Trim()))
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            var normalizedCustomFields = AssetCustomFieldEndpoints.NormalizeValues(customFields, submittedCustomFields);
            if (normalizedCustomFields.Error is not null) AddError(errors, $"{index + 2}. satır: {normalizedCustomFields.Error}");
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
        var uploadedAssets = validRows.GroupBy(item => item.row[0].Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().row[2], StringComparer.OrdinalIgnoreCase);
        var parentTags = hasParentColumn ? validRows.Select(item => item.row[parentIndex].Trim().ToUpperInvariant()).Where(tag => tag.Length > 0).Distinct(StringComparer.Ordinal).ToArray() : [];
        var availableParentTags = parentTags.Length == 0 ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) :
            (await db.Assets.AsNoTracking().Where(asset => asset.CompanyId == companyId && asset.Status != AssetStatuses.Archived && parentTags.Contains(asset.AssetTag.ToUpper()))
                .Select(asset => asset.AssetTag.ToUpper()).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
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
            if (hasParentColumn && !string.IsNullOrWhiteSpace(row[parentIndex]) &&
                (string.Equals(row[parentIndex].Trim(), row[0].Trim(), StringComparison.OrdinalIgnoreCase) ||
                 (!availableParentTags.Contains(row[parentIndex].Trim()) && !uploadedAssets.ContainsKey(row[parentIndex].Trim())) ||
                 (uploadedAssets.TryGetValue(row[parentIndex].Trim(), out var uploadedParentStatus) && uploadedParentStatus == AssetStatuses.Archived)))
                AddError(errors, $"{index + 2}. satırdaki üst varlık etiketi geçersiz, arşivlenmiş veya varlık kendisini üst varlık seçiyor.");
        }
        if (hasParentColumn)
        {
            var importedEdges = validRows.Where(item => !string.IsNullOrWhiteSpace(item.row[parentIndex]) && uploadedAssets.ContainsKey(item.row[parentIndex].Trim()))
                .GroupBy(item => item.row[0].Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().row[parentIndex].Trim(), StringComparer.OrdinalIgnoreCase);
            foreach (var startTag in importedEdges.Keys)
            {
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var cursor = startTag;
                while (importedEdges.TryGetValue(cursor, out var parentTag))
                {
                    if (!visited.Add(cursor)) { AddError(errors, $"CSV içe aktarma dosyası {startTag} varlığını içeren bir üst varlık döngüsü oluşturuyor."); break; }
                    cursor = parentTag;
                }
            }
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
            schemaVersion = hasParentColumn ? 3 : hasWarrantyColumn ? 2 : 1,
            counts = new { assets = records.Count - 1 },
            defaultLocationApplied,
            existingMatches = new { assets = existingMatches },
            errors = errors.Take(30).ToArray(),
            warning = $"Varsayılan konum {defaultLocationApplied:N0} boş konumlu varlığa uygulanacak. Önizleme kategori, marka, model, çalışan ve varsa üst varlık eşleşmelerini denetler. Var olan kayıtlar birleştirilmez; bu aşamada hiçbir kayıt değiştirilmez."
        });
    }

    private static List<List<string>> ParseAssetCsv(string text, int maximumRecords)
    {
        var delimiter = DetectAssetCsvDelimiter(text);
        return ParseSemicolonCsv(text, maximumRecords, delimiter);
    }

    private static char DetectAssetCsvDelimiter(string text)
    {
        var commas = 0;
        var semicolons = 0;
        var quoted = false;
        foreach (var value in text)
        {
            if (value == '"') { quoted = !quoted; continue; }
            if (quoted) continue;
            if (value is '\r' or '\n') break;
            if (value == ',') commas++;
            else if (value == ';') semicolons++;
        }
        return commas > semicolons ? ',' : ';';
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
            var assetPurchaseOrders = root.TryGetProperty("assetPurchaseOrders", out _) ? ReadRows(root, "assetPurchaseOrders", sourceCompanyId, errors, counts) : [];
            var assetPurchaseOrderLines = root.TryGetProperty("assetPurchaseOrderLines", out _) ? ReadRows(root, "assetPurchaseOrderLines", sourceCompanyId, errors, counts) : [];
            counts.TryAdd("assetPurchaseOrders", 0);
            counts.TryAdd("assetPurchaseOrderLines", 0);
            _ = ReadRows(root, "auditEntries", sourceCompanyId, errors, counts);
            _ = ReadRows(root, "inventoryRequests", sourceCompanyId, errors, counts);
            _ = ReadRows(root, "assetMaintenances", sourceCompanyId, errors, counts);
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
                if (status is not (AssetStatuses.Available or AssetStatuses.Deployed or AssetStatuses.Pending or AssetStatuses.Maintenance or AssetStatuses.Unusable or AssetStatuses.Archived))
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
            foreach (var request in EnumerateRows(root, "inventoryRequests"))
            {
                if (request.ValueKind != JsonValueKind.Object) continue;
                if (!TryGuid(request, "employeeId", out var employeeId) || !employees.Contains(employeeId))
                    AddError(errors, "Bir stok talebinin çalışanı dosyada bulunamadı.");
                var itemType = ReadString(request, "itemType");
                if (itemType is not ("Asset" or "Accessory" or "Consumable"))
                    AddError(errors, "Bir stok talebinin türü desteklenmiyor.");
                else if (itemType is "Accessory" or "Consumable")
                    AddError(errors, "Bu JSON geri yükleme sürümü yalnızca varlık taleplerini taşıyabilir; aksesuar/sarf talebi bulunan arşiv uygulanamaz.");
                if (!TryGuid(request, "itemId", out var itemId))
                    AddError(errors, "Bir stok talebinin ürün kimliği geçersiz.");
                else if (itemType == "Asset" && !assets.Contains(itemId))
                    AddError(errors, "Bir varlık talebi dosyada bulunmayan bir varlığa bağlı.");
                var status = ReadString(request, "status");
                if (status is not ("Pending" or "Approved" or "Rejected" or "Cancelled"))
                    AddError(errors, "Bir stok talebinin durumu desteklenmiyor.");
                if (!request.TryGetProperty("quantity", out var quantity) || !quantity.TryGetInt32(out var requestedQuantity) || requestedQuantity is < 1 or > 100_000)
                    AddError(errors, "Bir stok talebinin adedi 1 ile 100.000 arasında olmalı.");
                if (OptionalTextExceeds(request, "requestNote", 1000) || OptionalTextExceeds(request, "reviewNote", 1000))
                    AddError(errors, "Bir stok talebinin notu 1.000 karakteri aşamaz.");
                var hasReviewedAt = request.TryGetProperty("reviewedAt", out var reviewedAt) && reviewedAt.ValueKind != JsonValueKind.Null;
                if (status == "Pending" ? hasReviewedAt : !IsValidDateTimeOffset(reviewedAt))
                    AddError(errors, "Bir stok talebinin incelenme tarihi durumu ile uyumlu olmalı.");
            }
            foreach (var maintenance in EnumerateRows(root, "assetMaintenances"))
            {
                if (maintenance.ValueKind != JsonValueKind.Object) continue;
                if (!TryGuid(maintenance, "assetId", out var assetId) || !assets.Contains(assetId))
                    AddError(errors, "Bir bakım kaydının varlığı dosyada bulunamadı.");
                var status = ReadString(maintenance, "status");
                if (status is not ("Scheduled" or "InProgress" or "Completed" or "Cancelled"))
                    AddError(errors, "Bir bakım kaydının durumu desteklenmiyor.");
                if (string.IsNullOrWhiteSpace(ReadString(maintenance, "maintenanceType")) || ReadString(maintenance, "maintenanceType")!.Length > 120)
                    AddError(errors, "Bakım türü boş olamaz ve 120 karakteri aşamaz.");
                if (!IsValidDateOnly(maintenance, "startedOn", required: true) ||
                    !IsValidDateOnly(maintenance, "plannedOn") || !IsValidDateOnly(maintenance, "completedOn"))
                    AddError(errors, "Bir bakım kaydının tarihi yyyy-AA-gg biçiminde olmalı.");
                var startedOn = ReadDateOnly(maintenance, "startedOn");
                var completedOn = ReadDateOnly(maintenance, "completedOn");
                var plannedOn = ReadDateOnly(maintenance, "plannedOn");
                if (completedOn is not null && startedOn is not null && completedOn < startedOn)
                    AddError(errors, "Bakım bitiş tarihi başlangıç tarihinden önce olamaz.");
                if (status is "Scheduled" or "Cancelled" && plannedOn is null)
                    AddError(errors, "Planlı veya iptal edilmiş bakımda planlanan tarih bulunmalı.");
                if ((status == "Completed") != (completedOn is not null))
                    AddError(errors, "Bakım bitiş tarihi yalnızca tamamlanmış kayıtlarda bulunmalı.");
                if (ReadString(maintenance, "currency") is not { Length: 3 })
                    AddError(errors, "Bir bakım kaydının para birimi 3 harfli kod olmalı.");
                if (maintenance.TryGetProperty("cost", out var cost) && cost.ValueKind != JsonValueKind.Null &&
                    (cost.ValueKind != JsonValueKind.Number || !cost.TryGetDecimal(out var amount) || amount < 0))
                    AddError(errors, "Bir bakım kaydının maliyeti geçersiz veya negatif.");
                if (OptionalTextExceeds(maintenance, "supplier", 200) || OptionalTextExceeds(maintenance, "description", 1000) ||
                    OptionalTextExceeds(maintenance, "notes", 2000) || OptionalTextExceeds(maintenance, "completionNotes", 2000))
                    AddError(errors, "Bir bakım kaydının metin alanı izin verilen uzunluğu aşıyor.");
            }

            var assetPurchaseOrderIds = new HashSet<Guid>();
            foreach (var order in EnumerateRows(root, "assetPurchaseOrders"))
            {
                if (order.ValueKind != JsonValueKind.Object) continue;
                if (!TryGuid(order, "id", out var orderId) || !assetPurchaseOrderIds.Add(orderId))
                    AddError(errors, "Bir varlık siparişinin kimliği geçersiz veya yineleniyor.");
                if (ReadString(order, "status") is not ("Ordered" or "PartiallyReceived" or "Received" or "Cancelled"))
                    AddError(errors, "Bir varlık siparişinin durumu desteklenmiyor.");
                if (string.IsNullOrWhiteSpace(ReadString(order, "supplier")) || ReadString(order, "supplier")!.Length > 200 ||
                    string.IsNullOrWhiteSpace(ReadString(order, "orderNumber")) || ReadString(order, "orderNumber")!.Length > 80)
                    AddError(errors, "Varlık siparişinin tedarikçisi veya sipariş numarası geçersiz.");
                if (ReadString(order, "currency") is not { Length: 3 }) AddError(errors, "Bir varlık siparişinin para birimi 3 harfli kod olmalı.");
                if (!IsValidDateOnly(order, "orderDate", required: true) || !IsValidDateOnly(order, "expectedOn"))
                    AddError(errors, "Bir varlık siparişinin tarihi yyyy-AA-gg biçiminde olmalı.");
            }
            foreach (var asset in EnumerateRows(root, "assets"))
                if (asset.ValueKind == JsonValueKind.Object && TryGuid(asset, "purchaseOrderId", out var purchaseOrderId) && !assetPurchaseOrderIds.Contains(purchaseOrderId))
                    AddError(errors, "Bir varlık satın alma siparişine bağlı ancak sipariş dosyada bulunmuyor.");
            foreach (var line in EnumerateRows(root, "assetPurchaseOrderLines"))
            {
                if (line.ValueKind != JsonValueKind.Object) continue;
                if (!TryGuid(line, "purchaseOrderId", out var orderId) || !assetPurchaseOrderIds.Contains(orderId))
                    AddError(errors, "Bir varlık sipariş kaleminin siparişi dosyada bulunamadı.");
                if (!TryGuid(line, "categoryId", out var categoryId) || !categories.Contains(categoryId))
                    AddError(errors, "Bir varlık sipariş kaleminin kategorisi dosyada bulunamadı.");
                if (TryGuid(line, "brandId", out var brandId) && !brands.Contains(brandId)) AddError(errors, "Bir varlık sipariş kaleminin markası dosyada bulunamadı.");
                if (TryGuid(line, "modelId", out var modelId) && !models.Contains(modelId)) AddError(errors, "Bir varlık sipariş kaleminin modeli dosyada bulunamadı.");
                if (!line.TryGetProperty("quantity", out var quantity) || !quantity.TryGetInt32(out var qty) || qty is < 1 or > 5000 ||
                    !line.TryGetProperty("quantityReceived", out var received) || !received.TryGetInt32(out var qtyReceived) || qtyReceived < 0 || qtyReceived > qty)
                    AddError(errors, "Bir varlık sipariş kaleminin toplam/teslim alınan adedi geçersiz.");
                if (line.TryGetProperty("unitCost", out var cost) && (cost.ValueKind != JsonValueKind.Number || !cost.TryGetDecimal(out var amount) || amount < 0))
                    AddError(errors, "Bir varlık sipariş kaleminin birim maliyeti geçersiz.");
                if (OptionalTextExceeds(line, "name", 200) || string.IsNullOrWhiteSpace(ReadString(line, "name")) || OptionalTextExceeds(line, "notes", 500))
                    AddError(errors, "Bir varlık sipariş kaleminin adı veya notu geçersiz.");
                if (!IsValidDateOnly(line, "warrantyExpiresAt")) AddError(errors, "Bir varlık sipariş kaleminin garanti tarihi yyyy-AA-gg biçiminde olmalı.");
            }

            if (counts.Values.Sum() > PreviewMaxRows) AddError(errors, $"Dosya en fazla {PreviewMaxRows:N0} kayıt içerebilir.");
            var targetName = await db.Companies.AsNoTracking().Where(c => c.Id == targetCompanyId)
                .Select(c => c.Name).SingleAsync(cancellationToken);
            var existingMatches = await CountExistingMatchesAsync(db, targetCompanyId, root, cancellationToken);
            var restoreTargetEmpty = !await CompanyHasImportableDataAsync(db, targetCompanyId, cancellationToken);
            var cleanErrors = errors.Distinct(StringComparer.Ordinal).Take(30).ToArray();
            return Results.Ok(new
            {
                isValid = cleanErrors.Length == 0,
                sourceCompany = companyName.GetString(),
                targetCompany = targetName,
                schemaVersion,
                counts,
                existingMatches,
                restoreTargetEmpty,
                errors = cleanErrors,
                warning = "Eşleşme sayıları otomatik birleştirme kararı değildir. JSON geri yükleme yalnızca şirketin envanter kayıtları boşsa açılır; işlem kullanıcı hesaplarını, parolaları veya medya dosyalarını taşımaz. Bu önizleme hiçbir kayıt değiştirmedi."
            });
        }
        catch (JsonException)
        {
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "Dosya geçerli JSON değil veya iç içe veri sınırını aşıyor." } });
        }
    }

    private static async Task<IResult> ApplyCompanyImportAsync(HttpContext context, AppDbContext db,
        IAntiforgery antiforgery, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(context.User.FindFirstValue("company_id"), out var targetCompanyId) ||
            !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
        var role = context.User.FindFirstValue("company_role");
        if (!await db.Companies.AsNoTracking().AnyAsync(c => c.Id == targetCompanyId && c.IsActive &&
            db.CompanyMemberships.Any(m => m.CompanyId == targetCompanyId && m.UserId == userId && m.RoleKey == role), cancellationToken))
            return Results.Forbid();
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
        if (context.Request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) != true)
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

        // Run the exact same read-only validation before any database write.
        var validationContext = new DefaultHttpContext { User = context.User, RequestServices = context.RequestServices };
        validationContext.Request.Method = "POST";
        validationContext.Request.ContentType = context.Request.ContentType;
        validationContext.Request.ContentLength = buffer.Length;
        validationContext.Request.Body = new MemoryStream(buffer.ToArray(), writable: false);
        foreach (var header in context.Request.Headers) validationContext.Request.Headers[header.Key] = header.Value;
        var validationResult = await PreviewImportAsync(validationContext, db, antiforgery, cancellationToken);
        await using var validationBody = new MemoryStream();
        validationContext.Response.Body = validationBody;
        await validationResult.ExecuteAsync(validationContext);
        validationBody.Position = 0;
        using var validationJson = await JsonDocument.ParseAsync(validationBody, cancellationToken: cancellationToken);
        if (validationContext.Response.StatusCode != StatusCodes.Status200OK ||
            !validationJson.RootElement.TryGetProperty("isValid", out var isValid) || isValid.ValueKind != JsonValueKind.True)
            return Results.Json(validationJson.RootElement.Clone(), statusCode: validationContext.Response.StatusCode);

        try
        {
            using var document = JsonDocument.Parse(buffer.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            // Company-level lock serializes imports; the first release is intentionally limited to an empty
            // inventory workspace so no existing data can be overwritten or ambiguously merged.
            _ = await db.Database.SqlQuery<Guid>($"SELECT \"Id\" AS \"Value\" FROM \"Companies\" WHERE \"Id\" = {targetCompanyId} FOR UPDATE")
                .SingleAsync(cancellationToken);
            var occupied = await CompanyHasImportableDataAsync(db, targetCompanyId, cancellationToken);
            if (occupied)
                return Results.Conflict(new { message = "Bu ilk geri yükleme sürümü yalnızca henüz envanter kaydı olmayan bir şirkette çalışır. Mevcut veriler korunmuştur; birleştirme modu henüz sunulmuyor." });

            var departments = DeserializeRows<CompanyDepartment>(root, "departmentCatalog");
            var locations = DeserializeRows<CompanyLocation>(root, "locationCatalog");
            var employees = DeserializeRows<Employee>(root, "employees");
            var categories = DeserializeRows<AssetCategory>(root, "assetCategories");
            var brands = DeserializeRows<AssetBrand>(root, "assetBrands");
            var models = DeserializeRows<AssetModel>(root, "assetModels");
            var assets = DeserializeRows<Asset>(root, "assets");
            var requests = DeserializeRows<InventoryRequest>(root, "inventoryRequests");
            var assetPurchaseOrders = DeserializeRows<AssetPurchaseOrder>(root, "assetPurchaseOrders");
            var assetPurchaseOrderLines = DeserializeRows<AssetPurchaseOrderLine>(root, "assetPurchaseOrderLines");
            var maintenances = DeserializeRows<AssetMaintenance>(root, "assetMaintenances");
            var events = DeserializeRows<AssetEvent>(root, "assetEvents");
            var audits = DeserializeRows<AuditEntry>(root, "auditEntries");
            if (requests.Any(x => x.ItemType is "Accessory" or "Consumable"))
                return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "Dosyada bu JSON biçiminin taşımadığı aksesuar veya sarf talepleri var; hiçbir kayıt aktarılmadı." } });

            var ids = new Dictionary<Guid, Guid>();
            var sourceIds = new object[] { departments, locations, employees, categories, brands, models, assets,
                requests, maintenances, events, audits, assetPurchaseOrders, assetPurchaseOrderLines }.SelectMany(rows => ((System.Collections.IEnumerable)rows)
                .Cast<object>().Select(row => (Guid)row.GetType().GetProperty("Id")!.GetValue(row)!)).ToArray();
            if (sourceIds.Distinct().Count() != sourceIds.Length)
                return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "Dosyada tablo aileleri arasında yinelenen kayıt kimliği var; hiçbir kayıt aktarılmadı." } });
            void RemapIds<T>(IEnumerable<T> rows) where T : class
            {
                foreach (var row in rows)
                {
                    var property = typeof(T).GetProperty("Id")!;
                    ids.Add((Guid)property.GetValue(row)!, Guid.NewGuid());
                }
            }
            RemapIds(departments); RemapIds(locations); RemapIds(employees); RemapIds(categories);
            RemapIds(brands); RemapIds(models); RemapIds(assets); RemapIds(requests); RemapIds(maintenances); RemapIds(events); RemapIds(audits);
            RemapIds(assetPurchaseOrders); RemapIds(assetPurchaseOrderLines);
            Guid? Map(Guid? source) => source is Guid id && ids.TryGetValue(id, out var mapped) ? mapped : null;
            void CompanyScope<T>(IEnumerable<T> rows) where T : class
            {
                var type = typeof(T); var idProperty = type.GetProperty("Id")!; var companyProperty = type.GetProperty("CompanyId")!;
                foreach (var row in rows) { idProperty.SetValue(row, ids[(Guid)idProperty.GetValue(row)!]); companyProperty.SetValue(row, targetCompanyId); }
            }
            CompanyScope(departments); CompanyScope(locations); CompanyScope(employees); CompanyScope(categories);
            CompanyScope(brands); CompanyScope(models); CompanyScope(assets); CompanyScope(requests);
            CompanyScope(maintenances); CompanyScope(events); CompanyScope(audits);
            CompanyScope(assetPurchaseOrders); CompanyScope(assetPurchaseOrderLines);
            foreach (var item in locations) { item.ParentLocationId = Map(item.ParentLocationId); item.ManagerEmployeeId = Map(item.ManagerEmployeeId); }
            foreach (var item in employees) { item.LocationCatalogId = Map(item.LocationCatalogId); item.ManagerEmployeeId = Map(item.ManagerEmployeeId); }
            foreach (var item in models) item.BrandId = Map(item.BrandId);
            foreach (var item in assets)
            {
                item.CategoryId = Map(item.CategoryId) ?? Guid.Empty; item.BrandId = Map(item.BrandId); item.ModelId = Map(item.ModelId);
                item.ParentAssetId = Map(item.ParentAssetId); item.LocationCatalogId = Map(item.LocationCatalogId);
                item.AssignedEmployeeId = Map(item.AssignedEmployeeId); item.PurchaseOrderId = Map(item.PurchaseOrderId);
                item.DeletedAt = null; item.DeletedByUserId = null;
            }
            foreach (var item in requests)
            {
                item.EmployeeId = Map(item.EmployeeId) ?? Guid.Empty; item.ItemId = item.ItemType == "Asset" ? Map(item.ItemId) ?? Guid.Empty : item.ItemId;
                item.RequestedByUserId = null; item.ReviewedByUserId = null;
            }
            foreach (var item in maintenances) { item.AssetId = Map(item.AssetId) ?? Guid.Empty; item.ActorUserId = null; item.SupplierId = null; }
            foreach (var item in events) { item.AssetId = Map(item.AssetId) ?? Guid.Empty; item.EmployeeId = Map(item.EmployeeId); item.ActorUserId = null; }
            foreach (var item in audits) { item.ActorUserId = null; item.TargetId = Map(item.TargetId); }
            foreach (var item in assetPurchaseOrders) { item.CreatedByUserId = null; item.SupplierId = null; }
            foreach (var item in assetPurchaseOrderLines)
            {
                item.PurchaseOrderId = Map(item.PurchaseOrderId) ?? Guid.Empty;
                item.CategoryId = Map(item.CategoryId) ?? Guid.Empty;
                item.BrandId = Map(item.BrandId); item.ModelId = Map(item.ModelId);
            }

            db.CompanyDepartments.AddRange(departments); db.CompanyLocations.AddRange(locations);
            db.Employees.AddRange(employees); db.AssetCategories.AddRange(categories); db.AssetBrands.AddRange(brands);
            db.AssetModels.AddRange(models); db.Assets.AddRange(assets); db.InventoryRequests.AddRange(requests);
            db.AssetMaintenances.AddRange(maintenances); db.AssetEvents.AddRange(events); db.AuditEntries.AddRange(audits);
            db.AssetPurchaseOrders.AddRange(assetPurchaseOrders); db.AssetPurchaseOrderLines.AddRange(assetPurchaseOrderLines);
            db.AuditEntries.Add(new AuditEntry { CompanyId = targetCompanyId, ActorUserId = userId,
                TargetType = "Company", TargetId = targetCompanyId, Action = "CompanyDataImported",
                Details = "Şirket JSON arşivindeki kataloglar, çalışanlar, varlıklar ve ilişkili kayıtlar yeni kimliklerle geri yüklendi." });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            var importedCounts = validationJson.RootElement.GetProperty("counts").EnumerateObject()
                .Where(property => property.Name != "members")
                .ToDictionary(property => property.Name, property => property.Value.GetInt32(), StringComparer.Ordinal);
            return Results.Ok(new { imported = true, counts = importedCounts,
                message = "Katalog ve envanter kayıtları yeni kimliklerle geri yüklendi. Şirket profili/marka ayarları, kullanıcı hesapları, parolalar ve medya dosyaları JSON aktarımında uygulanmadı." });
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { message = "Veritabanı bütünlük kısıtı nedeniyle aktarım geri alındı. Şirket verilerinde değişiklik yapılmadı." });
        }
        catch (JsonException)
        {
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "Geri yüklenecek JSON kayıtları okunamadı." } });
        }
    }

    private static List<T> DeserializeRows<T>(JsonElement root, string property) =>
        root.TryGetProperty(property, out var rows) && rows.ValueKind == JsonValueKind.Array
            ? JsonSerializer.Deserialize<List<T>>(rows.GetRawText(), JsonOptions) ?? [] : [];

    private static async Task<bool> CompanyHasImportableDataAsync(AppDbContext db, Guid companyId, CancellationToken cancellationToken) =>
        await db.Employees.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.CompanyDepartments.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.CompanyLocations.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.AssetCategories.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.AssetBrands.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.AssetModels.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.Assets.IgnoreQueryFilters().AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.AssetEvents.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.AssetPurchaseOrders.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.AssetPurchaseOrderLines.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.AssetMaintenances.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.InventoryRequests.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.SupplierCatalogEntries.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.AssetCustomFieldDefinitions.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.Accessories.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.AccessoryAssignments.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.AccessoryMovements.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.AccessoryKits.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.AccessoryKitLines.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.AccessoryPurchaseOrders.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.AccessoryPurchaseOrderLines.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.Consumables.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.ConsumableMovements.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.InventoryComponents.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.ComponentAllocations.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.ComponentMovements.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.SoftwareLicenses.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.SoftwareLicenseAssignments.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.SoftwareLicenseEvents.AnyAsync(x => x.CompanyId == companyId, cancellationToken) ||
        await db.StockPurchaseOrders.AnyAsync(x => x.CompanyId == companyId, cancellationToken);

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

    private static bool OptionalTextExceeds(JsonElement item, string propertyName, int maxLength) =>
        item.TryGetProperty(propertyName, out var value) && value.ValueKind != JsonValueKind.Null &&
        (value.ValueKind != JsonValueKind.String || value.GetString()!.Length > maxLength);

    private static DateOnly? ReadDateOnly(JsonElement item, string propertyName) =>
        item.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String &&
        DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var date) ? date : null;

    private static bool IsValidDateOnly(JsonElement item, string propertyName, bool required = false) =>
        item.TryGetProperty(propertyName, out var value) && (value.ValueKind == JsonValueKind.Null
            ? !required
            : value.ValueKind == JsonValueKind.String && ReadDateOnly(item, propertyName) is not null);

    private static bool IsValidDateTimeOffset(JsonElement value) => value.ValueKind == JsonValueKind.String &&
        DateTimeOffset.TryParse(value.GetString(), System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind, out _);

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

        var responseWriter = PipeWriter.Create(context.Response.Body, new StreamPipeWriterOptions(leaveOpen: true));
        try
        {
            using (var writer = new Utf8JsonWriter(responseWriter, new JsonWriterOptions
            {
                Indented = false,
                SkipValidation = false
            }))
            {
                await WriteCompanyJsonAsync(writer, db, company, companyId, cancellationToken, responseWriter);
                writer.Flush();
            }
            await responseWriter.FlushAsync(cancellationToken);
        }
        finally
        {
            await responseWriter.CompleteAsync();
        }
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

        var archivePath = Path.Combine(Path.GetTempPath(), $"envantertr-company-export-{Guid.NewGuid():N}.zip");
        try
        {
            await using var archiveFile = new FileStream(archivePath, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.None, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(archivePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            var manifestFiles = new List<ArchiveManifestFile>(files.Count + 1);
            using (var archive = new ZipArchive(archiveFile, ZipArchiveMode.Create, leaveOpen: true))
            {
                var jsonEntry = archive.CreateEntry("company-data.json", CompressionLevel.Fastest);
                await using (var jsonStream = jsonEntry.Open())
                await using (var hashingStream = new HashingWriteStream(jsonStream))
                using (var writer = new Utf8JsonWriter(hashingStream, new JsonWriterOptions { Indented = false }))
                {
                    await WriteCompanyJsonAsync(writer, db, company, companyId, cancellationToken);
                    await writer.FlushAsync(cancellationToken);
                    manifestFiles.Add(new ArchiveManifestFile("company-data.json", hashingStream.BytesWritten, hashingStream.FinishHash()));
                }
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var entry = archive.CreateEntry(file.Entry, CompressionLevel.Fastest);
                    await using var output = entry.Open();
                    await using var hashingOutput = new HashingWriteStream(output);
                    await using var input = new FileStream(file.Source, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    await input.CopyToAsync(hashingOutput, cancellationToken);
                    manifestFiles.Add(new ArchiveManifestFile(file.Entry, hashingOutput.BytesWritten, hashingOutput.FinishHash()));
                }
                var manifestEntry = archive.CreateEntry("manifest.json", CompressionLevel.Fastest);
                await using (var manifestStream = manifestEntry.Open())
                    await JsonSerializer.SerializeAsync(manifestStream, new { format = "envantertr-company-archive-manifest", schemaVersion = 1, files = manifestFiles }, JsonOptions, cancellationToken);
                var readme = archive.CreateEntry("README.txt", CompressionLevel.Fastest);
                await using var readmeStream = readme.Open();
                await using var readmeWriter = new StreamWriter(readmeStream, new UTF8Encoding(false));
                await readmeWriter.WriteAsync("EnvanterTR şirket veri arşivi\r\ncompany-data.json şirket kayıtlarının bir bölümünü, departman ve konum kataloglarını içerir. media/ klasörü şirket logolarını, varlık dosyalarını, aksesuar fotoğraflarını, lisans ve bakım eklerini, çalışan fotoğraflarını/belgelerini ve bu şirkete üye hesapların profil fotoğraflarını içerir. Bu arşiv tam sistem yedeği veya geri yükleme paketi değildir. Kimlik bilgileri ve oturumlar dahil edilmez.\r\n");
            }
            await archiveFile.FlushAsync(cancellationToken);
            archiveFile.Position = 0;
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/zip";
            context.Response.Headers.CacheControl = "no-store, private";
            context.Response.Headers.ContentDisposition = $"attachment; filename=envantertr-sirket-arsivi-{DateTime.UtcNow:yyyyMMdd}.zip";
            context.Response.ContentLength = archiveFile.Length;
            await archiveFile.CopyToAsync(context.Response.Body, 64 * 1024, cancellationToken);
        }
        finally
        {
            try { if (File.Exists(archivePath)) File.Delete(archivePath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return Results.Empty;
    }

    private static async Task WriteCompanyJsonAsync(Utf8JsonWriter writer, AppDbContext db, Company company,
        Guid companyId, CancellationToken cancellationToken, PipeWriter? responseWriter = null)
    {
        writer.WriteStartObject();
        writer.WriteString("format", "envantertr-company-data");
        writer.WriteNumber("schemaVersion", 1);
        writer.WriteString("exportedAt", DateTimeOffset.UtcNow);
        writer.WritePropertyName("company");
        JsonSerializer.Serialize(writer, new { company.Id, company.Name, company.Slug, company.ContactEmail,
            company.BrandLogoPath, company.BrandPrimaryColor, company.LabelPrintSettingsJson, company.CreatedAt }, JsonOptions);
        await WritePagedAsync(writer, "departmentCatalog", db.CompanyDepartments.AsNoTracking()
            .Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken, responseWriter);
        await WritePagedAsync(writer, "locationCatalog", db.CompanyLocations.AsNoTracking()
            .Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken, responseWriter);
        await WritePagedAsync(writer, "employees", db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken, responseWriter);
        await WritePagedAsync(writer, "assetCategories", db.AssetCategories.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken, responseWriter);
        await WritePagedAsync(writer, "assetBrands", db.AssetBrands.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken, responseWriter);
        await WritePagedAsync(writer, "assetModels", db.AssetModels.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken, responseWriter);
        await WritePagedAsync(writer, "assets", db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken, responseWriter);
        await WritePagedAsync(writer, "inventoryRequests", db.InventoryRequests.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken, responseWriter);
        await WritePagedAsync(writer, "assetMaintenances", db.AssetMaintenances.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken, responseWriter);
        await WritePagedAsync(writer, "assetEvents", db.AssetEvents.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken, responseWriter);
        await WritePagedAsync(writer, "assetPurchaseOrders", db.AssetPurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken, responseWriter);
        await WritePagedAsync(writer, "assetPurchaseOrderLines", db.AssetPurchaseOrderLines.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken, responseWriter);
        await WritePagedAsync(writer, "auditEntries", db.AuditEntries.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Id), cancellationToken, responseWriter);
        var members = from membership in db.CompanyMemberships.AsNoTracking().Where(x => x.CompanyId == companyId)
                      join user in db.Users.AsNoTracking() on membership.UserId equals user.Id
                      orderby membership.UserId
                      select new { UserId = user.Id, user.DisplayName, Email = user.Email!, membership.RoleKey, membership.CreatedAt };
        await WritePagedAsync(writer, "members", members, cancellationToken, responseWriter);
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

    private sealed record ArchiveManifestFile(string Path, long Length, string Sha256);

    private sealed class HashingWriteStream : Stream
    {
        private readonly Stream _inner;
        private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        private bool _hashFinished;
        public long BytesWritten { get; private set; }

        public HashingWriteStream(Stream inner) => _inner = inner;
        public string FinishHash()
        {
            if (_hashFinished) throw new InvalidOperationException("Akış özeti zaten tamamlandı.");
            _hashFinished = true;
            return Convert.ToHexString(_hash.GetHashAndReset()).ToLowerInvariant();
        }
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => BytesWritten;
        public override long Position { get => BytesWritten; set => throw new NotSupportedException(); }
        public override void Flush() => _inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count); _hash.AppendData(buffer, offset, count); BytesWritten += count;
        }
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _inner.Write(buffer); _hash.AppendData(buffer); BytesWritten += buffer.Length;
        }
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await _inner.WriteAsync(buffer, cancellationToken); _hash.AppendData(buffer.Span); BytesWritten += buffer.Length;
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing) _hash.Dispose();
            base.Dispose(disposing);
        }
        public override ValueTask DisposeAsync()
        {
            _hash.Dispose();
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }
    }

    private static async Task WritePagedAsync<T>(Utf8JsonWriter writer, string propertyName,
        IQueryable<T> query, CancellationToken cancellationToken, PipeWriter? responseWriter = null)
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
            if (responseWriter is not null)
                await responseWriter.FlushAsync(cancellationToken);
            if (batch.Count < BatchSize) break;
            offset += batch.Count;
        }
        writer.WriteEndArray();
    }
}
