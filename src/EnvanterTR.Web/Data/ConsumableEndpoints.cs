using System.Security.Claims;
using System.Globalization;
using System.Text;
using System.Data;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class ConsumableEndpoints
{
    private const int CsvImportMaxBytes = 32 * 1024 * 1024;
    private const int CsvImportMaxRows = 5000;

    public static RouteGroupBuilder MapConsumableEndpoints(this RouteGroupBuilder api)
    {
        api.MapConsumableImportMappingProfileEndpoints();
        api.MapPost("/consumables/import/preview.csv", (HttpContext context, AppDbContext db, IAntiforgery antiforgery, CancellationToken ct) =>
            ImportCsvAsync(context, db, antiforgery, false, ct)).RequireAuthorization("asset.write")
            .WithMetadata(new RequestSizeLimitAttribute(CsvImportMaxBytes));
        api.MapPost("/consumables/import/preview.xlsx", (HttpContext context, AppDbContext db, IAntiforgery antiforgery, CancellationToken ct) =>
            ImportCsvAsync(context, db, antiforgery, false, ct)).RequireAuthorization("asset.write")
            .WithMetadata(new RequestSizeLimitAttribute(CsvImportMaxBytes));
        api.MapPost("/consumables/import.csv", (HttpContext context, AppDbContext db, IAntiforgery antiforgery, CancellationToken ct) =>
            ImportCsvAsync(context, db, antiforgery, true, ct)).RequireAuthorization("asset.write")
            .WithMetadata(new RequestSizeLimitAttribute(CsvImportMaxBytes));
        api.MapPost("/consumables/import.xlsx", (HttpContext context, AppDbContext db, IAntiforgery antiforgery, CancellationToken ct) =>
            ImportCsvAsync(context, db, antiforgery, true, ct)).RequireAuthorization("asset.write")
            .WithMetadata(new RequestSizeLimitAttribute(CsvImportMaxBytes));

        api.MapGet("/consumables", async ([FromQuery] string? q, [FromQuery] string? stock,
            [FromQuery] string? sortBy, [FromQuery] string? sortDirection, ClaimsPrincipal user, AppDbContext db, CancellationToken ct,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 200);
            var query = db.Consumables.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
                query = query.Where(x => x.Name.Contains(term) || (x.Category != null && x.Category.Contains(term)) ||
                    (x.ModelNumber != null && x.ModelNumber.Contains(term)) || (x.ItemNumber != null && x.ItemNumber.Contains(term)) ||
                    (x.Supplier != null && x.Supplier.Contains(term)) || (x.Location != null && x.Location.Contains(term)));
            }
            // Stock filtering and values use correlated aggregates so each tenant's rows stay isolated.
            if (stock == "low") query = query.Where(x => x.TotalQuantity - (db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0) <= x.MinimumQuantity);
            if (stock == "available") query = query.Where(x => x.TotalQuantity - (db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0) > 0);
            var sort = (sortBy ?? "name").ToLowerInvariant(); var desc = (sortDirection ?? "asc").ToLowerInvariant() == "desc";
            IOrderedQueryable<Consumable>? ordered = (sort, desc) switch
            {
                ("name", false) => query.OrderBy(x => x.Name), ("name", true) => query.OrderByDescending(x => x.Name),
                ("remaining", false) => query.OrderBy(x => x.TotalQuantity - (db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0)),
                ("remaining", true) => query.OrderByDescending(x => x.TotalQuantity - (db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0)),
                ("updated", false) => query.OrderBy(x => x.UpdatedAt), ("updated", true) => query.OrderByDescending(x => x.UpdatedAt), _ => null
            };
            if (ordered is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["sortBy"] = ["Sıralama alanı desteklenmiyor."] });
            var total = await query.CountAsync(ct);
            var items = await ordered.ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
            {
                x.Id, x.Name, x.Category, x.ModelNumber, x.ItemNumber, x.Supplier, x.Location,
                x.MinimumQuantity, x.TotalQuantity, x.UnitCost, x.Currency, x.PurchaseDate, x.WarrantyExpiresAt, x.Notes,
                x.IsRequestable, x.UpdatedAt,
                issued = db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0
            }).ToListAsync(ct);
            return Results.Ok(new { items, page, pageSize, total });
        }).RequireAuthorization("asset.read");

        api.MapGet("/consumables/export.csv", async ([FromQuery] string? q, [FromQuery] string? stock,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            var query = db.Consumables.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim(); if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
                query = query.Where(x => x.Name.Contains(term) || (x.Category != null && x.Category.Contains(term)) ||
                    (x.ModelNumber != null && x.ModelNumber.Contains(term)) || (x.ItemNumber != null && x.ItemNumber.Contains(term)) ||
                    (x.Supplier != null && x.Supplier.Contains(term)) || (x.Location != null && x.Location.Contains(term)));
            }
            if (stock == "low") query = query.Where(x => x.TotalQuantity - (db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0) <= x.MinimumQuantity);
            if (stock == "available") query = query.Where(x => x.TotalQuantity - (db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0) > 0);
            if (await query.CountAsync(ct) > 10000) return Results.ValidationProblem(new Dictionary<string, string[]> { ["export"] = ["Dışa aktarım tek dosyada en fazla 10.000 satır içerebilir. Arama veya stok filtresini daraltın."] });
            var rows = await query.OrderBy(x => x.Name).Select(x => new
            {
                x.Name, x.Category, x.ModelNumber, x.ItemNumber, x.Supplier, x.Location, x.TotalQuantity, x.MinimumQuantity, x.UnitCost, x.Currency, x.PurchaseDate, x.WarrantyExpiresAt,
                Issued = db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0
            }).ToListAsync(ct);
            var csv = new StringBuilder("\uFEFF\"Malzeme\",\"Kategori\",\"Model\",\"Ürün no\",\"Tedarikçi\",\"Konum\",\"Toplam\",\"Tüketilen\",\"Kalan\",\"Minimum\",\"Birim maliyeti\",\"Para birimi\",\"Alış tarihi\",\"Garanti bitiş tarihi\"\r\n");
            foreach (var row in rows)
            {
                var cells = new[] { row.Name, row.Category, row.ModelNumber, row.ItemNumber, row.Supplier, row.Location,
                    row.TotalQuantity.ToString(CultureInfo.InvariantCulture), row.Issued.ToString(CultureInfo.InvariantCulture),
                    (row.TotalQuantity - row.Issued).ToString(CultureInfo.InvariantCulture), row.MinimumQuantity.ToString(CultureInfo.InvariantCulture),
                    row.UnitCost?.ToString("0.00", CultureInfo.InvariantCulture), row.Currency, row.PurchaseDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), row.WarrantyExpiresAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) };
                csv.AppendJoin(',', cells.Select(CsvCell)).Append("\r\n");
            }
            return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"sarf-stoklari-{DateTime.UtcNow:yyyyMMdd}.csv");
        }).RequireAuthorization("asset.read");

        api.MapGet("/consumables/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            var item = await db.Consumables.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new { x.Id, x.Name, x.Category, x.ModelNumber, x.ItemNumber, x.Supplier, x.Location,
                    x.MinimumQuantity, x.TotalQuantity, x.UnitCost, x.Currency, x.PurchaseDate, x.WarrantyExpiresAt, x.Notes, x.IsRequestable, x.UpdatedAt,
                    issued = db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0 })
                .SingleOrDefaultAsync(ct);
            if (item is null) return Results.NotFound();
            var movements = await db.ConsumableMovements.AsNoTracking().Where(m => m.CompanyId == companyId && m.ConsumableId == id)
                .OrderByDescending(m => m.OccurredAt).Take(100).Select(m => new { m.Id, m.MovementType, m.Quantity, m.Notes, m.OccurredAt,
                    employee = db.Employees.Where(e => e.CompanyId == companyId && e.Id == m.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                    actor = db.Users.Where(u => u.Id == m.ActorUserId).Select(u => u.DisplayName).FirstOrDefault() }).ToListAsync(ct);
            return Results.Ok(new { item, movements });
        }).RequireAuthorization("asset.read");

        api.MapPost("/consumables", async (CreateRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            var errors = Validate(request.Name, request.MinimumQuantity, request.Quantity, request.UnitCost, request.Currency,
                request.Category, request.ModelNumber, request.ItemNumber, request.Supplier, request.Location, request.Notes);
            if (request.WarrantyExpiresAt is { } warranty && request.PurchaseDate is { } purchase && warranty < purchase)
                errors["warrantyExpiresAt"] = ["Garanti bitiş tarihi alış tarihinden önce olamaz."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var location = Clean(request.Location) ?? await AssetEndpoints.GetDefaultLocationPathAsync(db, companyId, ct);
            var item = new Consumable { CompanyId = companyId, Name = request.Name.Trim(), Category = Clean(request.Category),
                ModelNumber = Clean(request.ModelNumber), ItemNumber = Clean(request.ItemNumber), Supplier = Clean(request.Supplier),
                SupplierId = await SupplierCatalogEndpoints.ResolveSupplierIdAsync(db, companyId, request.Supplier, ct),
                Location = location, LocationCatalogId = await AssetEndpoints.ResolveLocationCatalogIdAsync(db, companyId, location, ct), MinimumQuantity = request.MinimumQuantity, TotalQuantity = request.Quantity,
                UnitCost = request.UnitCost, Currency = request.Currency.Trim().ToUpperInvariant(), PurchaseDate = request.PurchaseDate, WarrantyExpiresAt = request.WarrantyExpiresAt,
                Notes = Clean(request.Notes), IsRequestable = request.IsRequestable };
            db.Consumables.Add(item);
            if (request.Quantity > 0) db.ConsumableMovements.Add(new ConsumableMovement { CompanyId = companyId, ConsumableId = item.Id,
                ActorUserId = UserId(user), MovementType = "Received", Quantity = request.Quantity, Notes = "İlk stok kaydı" });
            AddAudit(db, companyId, user, item.Id, "consumable.created", item.Name);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/consumables/{item.Id}", new { item.Id });
        }).RequireAuthorization("asset.write");

        api.MapPut("/consumables/{id:guid}", async (Guid id, UpdateRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            var item = await db.Consumables.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (item is null) return Results.NotFound();
            var errors = Validate(request.Name, request.MinimumQuantity, item.TotalQuantity, request.UnitCost, request.Currency,
                request.Category, request.ModelNumber, request.ItemNumber, request.Supplier, request.Location, request.Notes);
            if (request.WarrantyExpiresAt is { } warranty && request.PurchaseDate is { } purchase && warranty < purchase)
                errors["warrantyExpiresAt"] = ["Garanti bitiş tarihi alış tarihinden önce olamaz."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            item.Name = request.Name.Trim(); item.Category = Clean(request.Category); item.ModelNumber = Clean(request.ModelNumber);
            item.ItemNumber = Clean(request.ItemNumber); item.Supplier = Clean(request.Supplier); item.SupplierId = await SupplierCatalogEndpoints.ResolveSupplierIdAsync(db, companyId, request.Supplier, ct); item.Location = Clean(request.Location);
            item.LocationCatalogId = await AssetEndpoints.ResolveLocationCatalogIdAsync(db, companyId, request.Location, ct);
            item.MinimumQuantity = request.MinimumQuantity; item.UnitCost = request.UnitCost; item.Currency = request.Currency.Trim().ToUpperInvariant();
            item.PurchaseDate = request.PurchaseDate; item.WarrantyExpiresAt = request.WarrantyExpiresAt; item.Notes = Clean(request.Notes); item.IsRequestable = request.IsRequestable; item.UpdatedAt = DateTimeOffset.UtcNow;
            AddAudit(db, companyId, user, item.Id, "consumable.updated", item.Name);
            await db.SaveChangesAsync(ct); return Results.NoContent();
        }).RequireAuthorization("asset.write");

        api.MapPost("/consumables/{id:guid}/stock", async (Guid id, StockRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            if (request.Quantity is 0 or < -100000 or > 100000) return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = ["Stok değişimi sıfır dışında, -100.000 ile 100.000 arasında olmalı."] });
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var item = await db.Consumables.FromSqlInterpolated(
                    $"SELECT * FROM \"Consumables\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (item is null) return Results.NotFound();
            var consumed = await db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == id && m.MovementType == "Checkout").SumAsync(m => (int?)m.Quantity, ct) ?? 0;
            var newTotal = item.TotalQuantity + request.Quantity;
            if (newTotal < consumed || newTotal > 100000) return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = [$"Yeni toplam stok tüketilmiş miktardan ({consumed}) az veya 100.000'den fazla olamaz."] });
            item.TotalQuantity = newTotal; item.UpdatedAt = DateTimeOffset.UtcNow;
            db.ConsumableMovements.Add(new ConsumableMovement { CompanyId = companyId, ConsumableId = id, ActorUserId = UserId(user),
                MovementType = request.Quantity > 0 ? "Received" : "Adjustment", Quantity = Math.Abs(request.Quantity), Notes = Clean(request.Notes) });
            AddAudit(db, companyId, user, id, "consumable.stock-adjusted", $"{request.Quantity:+#;-#;0} adet");
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return Results.Ok(new { totalQuantity = item.TotalQuantity, remaining = item.TotalQuantity - consumed });
        }).RequireAuthorization("asset.write");

        api.MapPost("/consumables/{id:guid}/checkout", async (Guid id, CheckoutRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            if (request.Quantity is < 1 or > 100000) return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = ["Tüketim adedi 1 ile 100.000 arasında olmalı."] });
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var item = await db.Consumables.FromSqlInterpolated(
                    $"SELECT * FROM \"Consumables\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (item is null) return Results.NotFound();
            if (!await db.Employees.AnyAsync(e => e.CompanyId == companyId && e.Id == request.EmployeeId && e.IsActive, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeId"] = ["Aktif çalışan seçilmelidir."] });
            var consumed = await db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == id && m.MovementType == "Checkout").SumAsync(m => (int?)m.Quantity, ct) ?? 0;
            var remaining = item.TotalQuantity - consumed;
            if (request.Quantity > remaining) return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = [$"Yeterli stok yok. Kullanılabilir miktar: {remaining}."] });
            db.ConsumableMovements.Add(new ConsumableMovement { CompanyId = companyId, ConsumableId = id, EmployeeId = request.EmployeeId,
                ActorUserId = UserId(user), MovementType = "Checkout", Quantity = request.Quantity, Notes = Clean(request.Notes) });
            AddAudit(db, companyId, user, id, "consumable.checked-out", $"{request.Quantity} adet; çalışan {request.EmployeeId}");
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return Results.Ok(new { remaining = remaining - request.Quantity });
        }).RequireAuthorization("asset.write");

        return api;
    }

    private static async Task<IResult> ImportCsvAsync(HttpContext context, AppDbContext db, IAntiforgery antiforgery,
        bool commit, CancellationToken ct)
    {
        if (!Tenant(context.User, out var companyId) || UserId(context.User) is not { } actorId) return Results.Forbid();
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
        Dictionary<string, string>? explicitMappings = null;
        var encodedMapping = context.Request.Headers["X-Consumable-Import-Mapping"].ToString();
        if (encodedMapping.Length > 0)
        {
            if (encodedMapping.Length > 24 * 1024) return Results.BadRequest(new { message = "Sütun eşlemesi sınırı aşıyor." });
            try { explicitMappings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(Convert.FromBase64String(encodedMapping)); }
            catch (Exception exception) when (exception is FormatException or System.Text.Json.JsonException)
            { return Results.ValidationProblem(new Dictionary<string, string[]> { ["mappings"] = ["Sütun eşlemesi okunamadı; dosyayı yeniden doğrulayın."] }); }
            if (!ConsumableCsvImportMapping.ValidateExplicitMappings(explicitMappings, out var mappingError))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["mappings"] = [mappingError] });
        }
        var isXlsx = DataExportEndpoints.IsXlsxRequest(context.Request);
        if (!isXlsx && context.Request.ContentType?.StartsWith("text/csv", StringComparison.OrdinalIgnoreCase) != true)
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        if (context.Request.ContentLength > CsvImportMaxBytes) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        using var buffer = new MemoryStream();
        var chunk = new byte[64 * 1024];
        while (true)
        {
            var read = await context.Request.Body.ReadAsync(chunk.AsMemory(), ct);
            if (read == 0) break;
            if (buffer.Length + read > CsvImportMaxBytes) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            await buffer.WriteAsync(chunk.AsMemory(0, read), ct);
        }
        List<List<string>> rows;
        try
        {
            if (isXlsx) rows = AssetXlsxImportParser.Parse(buffer, CsvImportMaxRows + 2);
            else { var text = new UTF8Encoding(false, true).GetString(buffer.ToArray()).TrimStart('\uFEFF'); rows = ParseCsv(text, CsvImportMaxRows + 2, DetectCsvDelimiter(text)); }
        }
        catch (DecoderFallbackException) { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "CSV UTF-8 biçiminde olmalıdır." } }); }
        catch (FormatException) { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { "CSV tırnak veya ayraç biçimi bozuk." } }); }
        catch (Exception exception) when (isXlsx && exception is InvalidDataException or InvalidOperationException or System.Xml.XmlException or OverflowException)
        { return Results.UnprocessableEntity(new { isValid = false, errors = new[] { exception.Message } }); }
        rows = ConsumableCsvImportMapping.Normalize(rows, out var unsupportedColumns, explicitMappings);
        if (unsupportedColumns.Length > 0)
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { $"Dosyadaki sütunları eşleyin veya açıkça yoksayın: {string.Join(", ", unsupportedColumns)}. Veri kaybını önlemek için aktarım durduruldu." }, unmappedColumns = unsupportedColumns, mappingTargets = ConsumableCsvImportMapping.MappingTargets() });

        string[] header = ["Malzeme", "Kategori", "Model", "Ürün no", "Tedarikçi", "Konum", "Toplam", "Minimum", "Birim maliyeti", "Para birimi", "Alış tarihi", "Garanti bitiş tarihi", "İstek yapılabilir", "Notlar"];
        if (rows.Count is < 2 || rows.Count > CsvImportMaxRows + 1 || !rows[0].SequenceEqual(header, StringComparer.OrdinalIgnoreCase))
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { $"İçe aktarma şablonunu kullanın: başlık satırı ve en fazla {CsvImportMaxRows:N0} sarf satırı." } });

        var errors = new List<string>();
        var parsed = new List<Consumable>(rows.Count - 1);
        var rowKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rowItemNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (row, index) in rows.Skip(1).Select((value, index) => (value, index)))
        {
            var line = index + 2;
            if (row.Count != header.Length) { AddCsvError(errors, $"{line}. satırda {header.Length} sütun olmalıdır."); continue; }
            var name = row[0].Trim(); var category = Clean(row[1]); var model = Clean(row[2]); var itemNumber = Clean(row[3]);
            var supplier = Clean(row[4]); var location = Clean(row[5]); var notes = Clean(row[13]);
            if (string.IsNullOrWhiteSpace(name) || name.Length > 200) AddCsvError(errors, $"{line}. satırdaki malzeme adı boş veya 200 karakterden uzun.");
            if (!int.TryParse(row[6], NumberStyles.None, CultureInfo.InvariantCulture, out var total) || total is < 0 or > 100000)
                AddCsvError(errors, $"{line}. satırdaki toplam stok 0–100.000 arasında tam sayı olmalıdır.");
            if (!int.TryParse(row[7], NumberStyles.None, CultureInfo.InvariantCulture, out var minimum) || minimum is < 0 or > 100000)
                AddCsvError(errors, $"{line}. satırdaki minimum stok 0–100.000 arasında tam sayı olmalıdır.");
            decimal? unitCost = null;
            if (!string.IsNullOrWhiteSpace(row[8]))
            {
                if (!decimal.TryParse(row[8], NumberStyles.Number, CultureInfo.InvariantCulture, out var cost) || cost is < 0 or > 9999999999999999.99m)
                    AddCsvError(errors, $"{line}. satırdaki birim maliyeti geçersiz.");
                else unitCost = cost;
            }
            var currency = row[9].Trim().ToUpperInvariant();
            if (currency.Length != 3 || !currency.All(char.IsAsciiLetter)) AddCsvError(errors, $"{line}. satırdaki para birimi üç harfli kod olmalıdır.");
            DateOnly? purchaseDate = null;
            if (!string.IsNullOrWhiteSpace(row[10]))
            {
                if (DateOnly.TryParseExact(row[10].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) purchaseDate = date;
                else AddCsvError(errors, $"{line}. satırdaki alış tarihi yyyy-AA-gg biçiminde olmalıdır.");
            }
            DateOnly? warrantyExpiresAt = null;
            if (!string.IsNullOrWhiteSpace(row[11]))
            {
                if (DateOnly.TryParseExact(row[11].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) warrantyExpiresAt = date;
                else AddCsvError(errors, $"{line}. satırdaki garanti bitiş tarihi yyyy-AA-gg biçiminde olmalıdır.");
            }
            if (warrantyExpiresAt is { } warranty && purchaseDate is { } purchase && warranty < purchase)
                AddCsvError(errors, $"{line}. satırdaki garanti bitiş tarihi alış tarihinden önce olamaz.");
            var requestableText = row[12].Trim();
            var requestable = requestableText.Equals("true", StringComparison.OrdinalIgnoreCase) || requestableText.Equals("evet", StringComparison.OrdinalIgnoreCase);
            if (!requestable && !requestableText.Equals("false", StringComparison.OrdinalIgnoreCase) && !requestableText.Equals("hayır", StringComparison.OrdinalIgnoreCase) && !requestableText.Equals("hayir", StringComparison.OrdinalIgnoreCase))
                AddCsvError(errors, $"{line}. satırdaki istek yapılabilir alanı Evet/Hayır olmalıdır.");
            if (category?.Length > 120 || model?.Length > 120 || itemNumber?.Length > 120 || supplier?.Length > 200 || location?.Length > 160 || notes?.Length > 2000)
                AddCsvError(errors, $"{line}. satırdaki metin alanlarından biri uzunluk sınırını aşıyor.");
            var key = CsvDuplicateKey(name, itemNumber);
            if (!rowKeys.Add(key)) AddCsvError(errors, $"{line}. satır aynı malzeme/ürün numarası bileşimini dosyada yineliyor.");
            if (itemNumber is not null && !rowItemNumbers.Add(itemNumber)) AddCsvError(errors, $"{line}. satırdaki ürün numarası dosyada yineleniyor.");
            parsed.Add(new Consumable { CompanyId = companyId, Name = name, Category = category, ModelNumber = model, ItemNumber = itemNumber,
                Supplier = supplier, Location = location, TotalQuantity = total, MinimumQuantity = minimum, UnitCost = unitCost,
                Currency = currency, PurchaseDate = purchaseDate, WarrantyExpiresAt = warrantyExpiresAt, IsRequestable = requestable, Notes = notes });
        }

        var cleanErrors = errors.Distinct(StringComparer.Ordinal).Take(30).ToArray();
        if (cleanErrors.Length > 0) return Results.UnprocessableEntity(new { isValid = false, rowCount = rows.Count - 1, errors = cleanErrors, message = "Dosyada düzeltilmesi gereken alanlar bulundu. Hiçbir sarf kaydı eklenmedi." });

        var names = parsed.Select(x => x.Name.ToUpper()).Distinct().ToArray();
        var itemNumbers = parsed.Where(x => x.ItemNumber != null).Select(x => x.ItemNumber!.ToUpper()).Distinct().ToArray();
        var existing = await db.Consumables.AsNoTracking().Where(x => x.CompanyId == companyId &&
            (names.Contains(x.Name.ToUpper()) || (x.ItemNumber != null && itemNumbers.Contains(x.ItemNumber.ToUpper()))))
            .Select(x => new { x.Name, x.ItemNumber }).ToListAsync(ct);
        var existingKeys = existing.Select(x => CsvDuplicateKey(x.Name, x.ItemNumber)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingItemNumbers = existing.Where(x => !string.IsNullOrWhiteSpace(x.ItemNumber)).Select(x => x.ItemNumber!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingMatches = parsed.Count(x => existingKeys.Contains(CsvDuplicateKey(x.Name, x.ItemNumber)) ||
            x.ItemNumber is not null && existingItemNumbers.Contains(x.ItemNumber));
        if (existingMatches > 0)
            return Results.Conflict(new { isValid = false, existingMatches, errors = new[] { $"Şirkette aynı ad/ürün numarası bileşiminde {existingMatches} kayıt var. Yinelenen stok açılmaması için dosya reddedildi." } });
        var locationPaths = await AssetEndpoints.GetActiveLocationPathMapAsync(db, companyId, ct);
        var defaultLocation = await AssetEndpoints.GetDefaultLocationPathAsync(db, companyId, ct);
        var defaultLocationApplied = defaultLocation is null ? 0 : parsed.Count(x => x.Location is null);
        foreach (var item in parsed)
        {
            item.Location ??= defaultLocation;
            if (item.Location is not null && locationPaths.TryGetValue(item.Location, out var locationCatalogId)) item.LocationCatalogId = locationCatalogId;
        }
        var suppliersToCreate = await SupplierImportCatalog.FindMissingNamesAsync(db, companyId, parsed.Select(x => x.Supplier), ct);
        if (!commit) return Results.Ok(new { isValid = true, rowCount = parsed.Count, defaultLocationApplied, suppliersToCreate, errors = Array.Empty<string>(),
            message = $"{parsed.Count:N0} yeni sarf kaydı doğrulandı. Varsayılan konum {defaultLocationApplied:N0} boş satıra uygulanacak; {suppliersToCreate.Length:N0} yeni tedarikçi yalnızca aktarımı onaylarsanız kataloğa eklenecek. Önizleme hiçbir kayıt eklemedi." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var latestExisting = await db.Consumables.AsNoTracking().Where(x => x.CompanyId == companyId &&
            (names.Contains(x.Name.ToUpper()) || (x.ItemNumber != null && itemNumbers.Contains(x.ItemNumber.ToUpper()))))
            .Select(x => new { x.Name, x.ItemNumber }).ToListAsync(ct);
        var latestKeys = latestExisting.Select(x => CsvDuplicateKey(x.Name, x.ItemNumber)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var latestItemNumbers = latestExisting.Where(x => !string.IsNullOrWhiteSpace(x.ItemNumber)).Select(x => x.ItemNumber!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (parsed.Any(x => latestKeys.Contains(CsvDuplicateKey(x.Name, x.ItemNumber)) || x.ItemNumber is not null && latestItemNumbers.Contains(x.ItemNumber)))
            return Results.Conflict(new { message = "Önizlemeden sonra aynı sarf kaydı eklendi. Hiçbir kayıt aktarılmadı; önizlemeyi yenileyin." });

        var suppliersCreated = await SupplierImportCatalog.LinkAndCreateAsync(db, companyId, actorId,
            parsed.Select(item => (item.Supplier, (Action<Guid>)(id => item.SupplierId = id))), ct);
        foreach (var item in parsed)
        {
            db.Consumables.Add(item);
            if (item.TotalQuantity > 0) db.ConsumableMovements.Add(new ConsumableMovement { CompanyId = companyId, ConsumableId = item.Id,
                ActorUserId = actorId, MovementType = "Received", Quantity = item.TotalQuantity, Notes = $"{(isXlsx ? "Excel" : "CSV")} içe aktarımındaki başlangıç stoku" });
            AddAudit(db, companyId, context.User, item.Id, "consumable.created", $"{item.Name}; {(isXlsx ? "Excel" : "CSV")} içe aktarımı.");
        }
        db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Consumable",
            Action = isXlsx ? "XlsxImported" : "CsvImported", Details = $"{(isXlsx ? "Excel" : "CSV")} dosyasından {parsed.Count} sarf kaydı içe aktarıldı." });
        try { await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            return Results.Conflict(new { message = "CSV aktarılmadı. Kayıt çakışması oluştu; önizlemeyi yenileyip tekrar deneyin." });
        }
        return Results.Ok(new { imported = parsed.Count, suppliersCreated, message = $"{parsed.Count:N0} sarf kaydı ve başlangıç stok hareketleri eklendi; {suppliersCreated:N0} yeni tedarikçi kataloğa bağlandı." });
    }

    private static string CsvDuplicateKey(string name, string? itemNumber) => $"{name.Trim().ToUpperInvariant()}\u001f{itemNumber?.Trim().ToUpperInvariant() ?? ""}";
    private static void AddCsvError(List<string> errors, string message) { if (errors.Count < 100) errors.Add(message); }
    private static char DetectCsvDelimiter(string text)
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

    private static List<List<string>> ParseCsv(string text, int maximumRecords, char delimiter)
    {
        var records = new List<List<string>>(); var row = new List<string>(); var cell = new StringBuilder(); var quoted = false; var afterQuote = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (quoted) { if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; } else if (ch == '"') { quoted = false; afterQuote = true; } else cell.Append(ch); continue; }
            if (afterQuote && ch != delimiter && ch is not ('\r' or '\n')) throw new FormatException();
            if (ch == '"') { if (cell.Length != 0) throw new FormatException(); quoted = true; }
            else if (ch == delimiter) { row.Add(cell.ToString()); cell.Clear(); afterQuote = false; }
            else if (ch is '\r' or '\n')
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(cell.ToString()); cell.Clear(); afterQuote = false;
                if (row.Any(value => value.Length > 0)) { records.Add(row); if (records.Count > maximumRecords) throw new FormatException(); }
                row = [];
            }
            else { cell.Append(ch); afterQuote = false; }
        }
        if (quoted) throw new FormatException();
        if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); if (row.Any(value => value.Length > 0)) records.Add(row); }
        return records;
    }

    private static bool Tenant(ClaimsPrincipal user, out Guid id) => Guid.TryParse(user.FindFirstValue("company_id"), out id);
    private static Guid? UserId(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string CsvCell(string? value)
    {
        var safe = value?.Trim() ?? "";
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@' or '\t' or '\r') safe = "'" + safe;
        return "\"" + safe.Replace("\"", "\"\"") + "\"";
    }
    private static void AddAudit(AppDbContext db, Guid companyId, ClaimsPrincipal user, Guid id, string action, string details) =>
        db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = UserId(user), TargetType = "Consumable", TargetId = id, Action = action, Details = details });

    private static Dictionary<string, string[]> Validate(string name, int minimum, int total, decimal? cost, string currency,
        string? category, string? model, string? item, string? supplier, string? location, string? notes)
    {
        var errors = new Dictionary<string, string[]>();
        void Add(string key, string message) => errors[key] = [message];
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) Add("name", "Ad zorunludur ve 200 karakteri aşamaz.");
        if (minimum is < 0 or > 100000) Add("minimumQuantity", "Minimum miktar 0 ile 100.000 arasında olmalı.");
        if (total is < 0 or > 100000) Add("quantity", "Toplam stok 0 ile 100.000 arasında olmalı.");
        if (cost < 0) Add("unitCost", "Birim maliyeti negatif olamaz.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3 || currency.Any(c => !char.IsAsciiLetter(c))) Add("currency", "Para birimi üç harfli kod olmalı.");
        if (category?.Length > 120) Add("category", "Kategori 120 karakteri aşamaz."); if (model?.Length > 120) Add("modelNumber", "Model 120 karakteri aşamaz.");
        if (item?.Length > 120) Add("itemNumber", "Ürün numarası 120 karakteri aşamaz."); if (supplier?.Length > 200) Add("supplier", "Tedarikçi 200 karakteri aşamaz.");
        if (location?.Length > 160) Add("location", "Konum 160 karakteri aşamaz."); if (notes?.Length > 2000) Add("notes", "Notlar 2.000 karakteri aşamaz.");
        return errors;
    }
    public sealed record CreateRequest(string Name, string? Category, string? ModelNumber, string? ItemNumber, string? Supplier,
        string? Location, int MinimumQuantity, int Quantity, decimal? UnitCost, string Currency, DateOnly? PurchaseDate, string? Notes, bool IsRequestable, DateOnly? WarrantyExpiresAt = null);
    public sealed record UpdateRequest(string Name, string? Category, string? ModelNumber, string? ItemNumber, string? Supplier,
        string? Location, int MinimumQuantity, decimal? UnitCost, string Currency, DateOnly? PurchaseDate, string? Notes, bool IsRequestable, DateOnly? WarrantyExpiresAt = null);
    public sealed record StockRequest(int Quantity, string? Notes);
    public sealed record CheckoutRequest(Guid EmployeeId, int Quantity, string? Notes);
}
