using System.Security.Claims;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class SoftwareLicenseEndpoints
{
    private const long LicenseAttachmentMaxBytes = 10 * 1024 * 1024;
    private const long LicenseAttachmentRequestBytes = LicenseAttachmentMaxBytes + 65_536;

    public static RouteGroupBuilder MapSoftwareLicenseEndpoints(this RouteGroupBuilder api)
    {
        api.MapSoftwareLicenseImportEndpoints();
        api.MapGet("/software-licenses", async ([FromQuery] string? q, ClaimsPrincipal user, AppDbContext db, CancellationToken ct,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? status = "active") =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            status = string.IsNullOrWhiteSpace(status) ? "active" : status.Trim().ToLowerInvariant();
            if (status is not ("active" or "archived" or "all"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Durum active, archived veya all olmalıdır."] });
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 200);
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            var scopedAssignments = ApplyAssignmentLocationScope(db, companyId, allowedLocations);
            var query = db.SoftwareLicenses.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (allowedLocations.IsRestricted) query = query.Where(x => scopedAssignments.Any(a => a.LicenseId == x.Id));
            if (status == "active") query = query.Where(x => !x.IsArchived);
            else if (status == "archived") query = query.Where(x => x.IsArchived);
            if (!string.IsNullOrWhiteSpace(q)) { var term = q.Trim(); if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string,string[]> { ["q"]=["Arama metni 120 karakteri aşamaz."] }); query = FilterLicenseReportSearch(query, term); }
            var total = await query.CountAsync(ct);
            var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
            {
                x.Id, x.Name, x.Publisher, x.ProductVersion, x.LicenseType, x.LicenseReference, x.Supplier, x.PurchaseOrderReference, x.Seats,
                x.UnitCost, x.Currency, x.PurchaseDate, x.ExpirationDate, x.AutoRenew, x.RequiresEulaAcceptance, x.IsArchived, x.UpdatedAt,
                assigned = scopedAssignments.Count(a => a.LicenseId == x.Id && a.RevokedAt == null)
            }).ToListAsync(ct);
            return Results.Ok(new { items, page, pageSize, total });
        }).RequireAuthorization("asset.read");

        var exportLicenses = async Task<IResult> ([FromQuery] string? q, HttpContext context, ClaimsPrincipal user, AppDbContext db, CancellationToken ct,
            [FromQuery] string? status = "active") =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            status = string.IsNullOrWhiteSpace(status) ? "active" : status.Trim().ToLowerInvariant();
            if (status is not ("active" or "archived" or "all"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Durum active, archived veya all olmalıdır."] });
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            var scopedAssignments = ApplyAssignmentLocationScope(db, companyId, allowedLocations);
            var query = db.SoftwareLicenses.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (allowedLocations.IsRestricted) query = query.Where(x => scopedAssignments.Any(a => a.LicenseId == x.Id));
            if (status == "active") query = query.Where(x => !x.IsArchived);
            else if (status == "archived") query = query.Where(x => x.IsArchived);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
                query = FilterLicenseReportSearch(query, term);
            }
            if (await query.CountAsync(ct) > 10000) return Results.ValidationProblem(new Dictionary<string, string[]> { ["export"] = ["Dışa aktarım en fazla 10.000 lisans içerir; aramayı daraltın."] });
            var rows = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Select(x => new
            {
                x.Name, x.Publisher, x.ProductVersion, x.LicenseType, x.Supplier, x.PurchaseOrderReference, x.Seats, x.UnitCost, x.Currency,
                x.PurchaseDate, x.ExpirationDate, x.AutoRenew,
                Assigned = scopedAssignments.Count(a => a.LicenseId == x.Id && a.RevokedAt == null)
            }).ToListAsync(ct);
            if (context.Request.Path.Value?.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) == true)
            {
                using var workbook = new XlsxExportWriter(["Lisans", "Yayıncı", "Ürün sürümü", "Tür", "Tedarikçi", "Satın alma sipariş no.", "Koltuk", "Atanmış", "Birim maliyeti", "Para birimi", "Alış tarihi", "Bitiş tarihi", "Otomatik yenileme"]);
                foreach (var row in rows)
                    workbook.WriteRow([row.Name, row.Publisher, row.ProductVersion, row.LicenseType, row.Supplier, row.PurchaseOrderReference,
                        row.Seats, row.Assigned, row.UnitCost, row.Currency, row.PurchaseDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        row.ExpirationDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), row.AutoRenew ? "true" : "false"]);
                context.Response.Headers["X-Total-Count"] = rows.Count.ToString(CultureInfo.InvariantCulture);
                await workbook.SendAsync(context, $"envantertr-lisanslar-{DateTime.UtcNow:yyyyMMdd}.xlsx", ct);
                return Results.Empty;
            }
            var csv = new StringBuilder("\uFEFF\"Lisans\",\"Yayıncı\",\"Ürün sürümü\",\"Tür\",\"Tedarikçi\",\"Satın alma sipariş no.\",\"Koltuk\",\"Atanmış\",\"Birim maliyeti\",\"Para birimi\",\"Alış tarihi\",\"Bitiş tarihi\",\"Otomatik yenileme\"\r\n");
            foreach (var x in rows)
            {
                var cells = new[] { x.Name, x.Publisher, x.ProductVersion, x.LicenseType, x.Supplier, x.PurchaseOrderReference, x.Seats.ToString(CultureInfo.InvariantCulture),
                    x.Assigned.ToString(CultureInfo.InvariantCulture), x.UnitCost?.ToString("0.00", CultureInfo.InvariantCulture), x.Currency,
                    x.PurchaseDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), x.ExpirationDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), x.AutoRenew ? "true" : "false" };
                csv.AppendJoin(',', cells.Select(CsvCell)).Append("\r\n");
            }
            return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"envantertr-lisanslar-{DateTime.UtcNow:yyyyMMdd}.csv");
        };
        api.MapGet("/software-licenses/export.csv", exportLicenses).RequireAuthorization("asset.read");
        api.MapGet("/software-licenses/export.xlsx", exportLicenses).RequireAuthorization("asset.read");

        api.MapGet("/reports/software-licenses", async (DateOnly? through, string? state, string? q, bool? includeArchived,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            if (!TryGetLicenseReportFilters(through, state, q, out var today, out var end, out var normalizedState, out var term, out var error)) return error!;
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            var scopedAssignments = ApplyAssignmentLocationScope(db, companyId, allowedLocations);
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            var companyLicenses = db.SoftwareLicenses.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (allowedLocations.IsRestricted) companyLicenses = companyLicenses.Where(x => scopedAssignments.Any(a => a.LicenseId == x.Id));
            if (includeArchived != true) companyLicenses = companyLicenses.Where(x => !x.IsArchived);
            var searchQuery = FilterLicenseReportSearch(companyLicenses, term);
            var expired = await searchQuery.CountAsync(x => x.ExpirationDate != null && x.ExpirationDate < today, ct);
            var upcoming = await searchQuery.CountAsync(x => x.ExpirationDate != null && x.ExpirationDate >= today && x.ExpirationDate <= end, ct);
            var withoutDate = await searchQuery.CountAsync(x => x.ExpirationDate == null, ct);
            var query = FilterLicenseReportState(searchQuery, normalizedState, today, end);
            var total = await query.CountAsync(ct);
            var rows = await query.OrderBy(x => x.ExpirationDate == null).ThenBy(x => x.ExpirationDate).ThenBy(x => x.Name).ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.Name, x.Publisher, x.ProductVersion, x.LicenseType, x.Supplier,
                    x.Seats, x.ExpirationDate, x.AutoRenew,
                    assigned = scopedAssignments.Count(a => a.LicenseId == x.Id && a.RevokedAt == null)
                }).ToListAsync(ct);
            var items = rows.Select(x => new
            {
                x.Id, x.Name, x.Publisher, x.ProductVersion, x.LicenseType, x.Supplier,
                x.Seats, x.ExpirationDate, x.AutoRenew, x.assigned,
                available = allowedLocations.IsUnrestricted ? Math.Max(0, x.Seats - x.assigned) : (int?)null,
                daysUntilExpiration = x.ExpirationDate is DateOnly expiration ? expiration.DayNumber - today.DayNumber : (int?)null
            });
            return Results.Ok(new { today, through = end, state = normalizedState, expired, upcoming, withoutDate,
                total, page, pageSize, pageCount = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)), items });
        }).RequireAuthorization("asset.read");

        api.MapGet("/reports/software-licenses/export.csv", async (DateOnly? through, string? state, string? q, bool? includeArchived,
            HttpContext context, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId)) { context.Response.StatusCode = StatusCodes.Status403Forbidden; return; }
            if (!TryGetLicenseReportFilters(through, state, q, out var today, out var end, out var normalizedState, out var term, out var error))
            {
                await error!.ExecuteAsync(context);
                return;
            }
            var companyLicenses = db.SoftwareLicenses.AsNoTracking().Where(x => x.CompanyId == companyId);
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            var scopedAssignments = ApplyAssignmentLocationScope(db, companyId, allowedLocations);
            if (allowedLocations.IsRestricted) companyLicenses = companyLicenses.Where(x => scopedAssignments.Any(a => a.LicenseId == x.Id));
            if (includeArchived != true) companyLicenses = companyLicenses.Where(x => !x.IsArchived);
            var query = FilterLicenseReportState(
                FilterLicenseReportSearch(companyLicenses, term),
                normalizedState, today, end);
            var total = await query.CountAsync(ct);
            if (total > 10000)
            {
                await Results.ValidationProblem(new Dictionary<string, string[]> { ["export"] = ["Dışa aktarım en fazla 10.000 lisans içerir; aramayı daraltın."] }).ExecuteAsync(context);
                return;
            }
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/csv; charset=utf-8";
            context.Response.Headers["Content-Disposition"] = $"attachment; filename=envantertr-lisans-bitis-raporu-{DateTime.UtcNow:yyyyMMdd}.csv";
            context.Response.Headers["Cache-Control"] = "no-store";
            context.Response.Headers["X-Total-Count"] = total.ToString(CultureInfo.InvariantCulture);
            await using var writer = new StreamWriter(context.Response.Body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), 16 * 1024, leaveOpen: true);
            await writer.WriteLineAsync("Lisans;Yayıncı;Ürün sürümü;Tür;Tedarikçi;Bitiş tarihi;Kalan gün;Atanmış koltuk;Toplam koltuk;Boş koltuk;Otomatik yenileme".AsMemory(), ct);
            var rows = query.OrderBy(x => x.ExpirationDate == null).ThenBy(x => x.ExpirationDate).ThenBy(x => x.Name).ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.Name, x.Publisher, x.ProductVersion, x.LicenseType, x.Supplier, x.ExpirationDate, x.Seats, x.AutoRenew,
                    Assigned = scopedAssignments.Count(a => a.LicenseId == x.Id && a.RevokedAt == null)
                });
            await foreach (var row in rows.AsAsyncEnumerable().WithCancellation(ct))
            {
                var days = row.ExpirationDate is DateOnly expiration ? (expiration.DayNumber - today.DayNumber).ToString(CultureInfo.InvariantCulture) : "";
                var line = string.Join(';', new[]
                {
                    CsvCell(row.Name), CsvCell(row.Publisher), CsvCell(row.ProductVersion), CsvCell(row.LicenseType), CsvCell(row.Supplier),
                    CsvCell(row.ExpirationDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), CsvCell(days),
                    CsvCell(row.Assigned.ToString(CultureInfo.InvariantCulture)), CsvCell(row.Seats.ToString(CultureInfo.InvariantCulture)),
                    CsvCell(allowedLocations.IsUnrestricted ? Math.Max(0, row.Seats - row.Assigned).ToString(CultureInfo.InvariantCulture) : ""),
                    CsvCell(row.AutoRenew ? "Evet" : "Hayır")
                });
                await writer.WriteLineAsync(line.AsMemory(), ct);
            }
            await writer.FlushAsync(ct);
        }).RequireAuthorization("asset.read");

        api.MapGet("/software-licenses/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            var scopedAssignments = ApplyAssignmentLocationScope(db, companyId, allowedLocations);
            var license = await db.SoftwareLicenses.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id &&
                (!allowedLocations.IsRestricted || scopedAssignments.Any(a => a.LicenseId == x.Id))).Select(x => new
            {
                x.Id, x.Name, x.Publisher, x.ProductVersion, x.LicenseType, x.LicenseReference, x.Supplier, x.SupplierId, x.PurchaseOrderReference, x.Seats, x.UnitCost, x.Currency,
                x.PurchaseDate, x.ExpirationDate, x.AutoRenew, x.RequiresEulaAcceptance, x.IsArchived, x.Notes, x.UpdatedAt,
                assigned = scopedAssignments.Count(a => a.LicenseId == x.Id && a.RevokedAt == null)
            }).SingleOrDefaultAsync(ct);
            if (license is null) return Results.NotFound();
            var assignments = await scopedAssignments.Where(a => a.LicenseId == id)
                .OrderBy(a => a.RevokedAt != null).ThenByDescending(a => a.AssignedAt).Take(200).Select(a => new
                { a.Id, a.EmployeeId, a.AssetId, employee = db.Employees.Where(e => e.CompanyId == companyId && e.Id == a.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                    employeeNumber = db.Employees.Where(e => e.CompanyId == companyId && e.Id == a.EmployeeId).Select(e => e.EmployeeNumber).FirstOrDefault(),
                    assetTag = db.Assets.Where(asset => asset.CompanyId == companyId && asset.Id == a.AssetId).Select(asset => asset.AssetTag).FirstOrDefault(),
                    assetName = db.Assets.Where(asset => asset.CompanyId == companyId && asset.Id == a.AssetId).Select(asset => asset.Name).FirstOrDefault(),
                    a.AssignedAt, a.EulaAcceptedAt, a.EulaVersion, a.EulaAttachmentId,
                    eulaAcceptedBy = db.Users.Where(actor => actor.Id == a.EulaAcceptedByUserId).Select(actor => actor.DisplayName).FirstOrDefault(),
                    a.RevokedAt, a.Notes }).ToListAsync(ct);
            var scopedEvents = ApplyEventLocationScope(db, companyId, allowedLocations);
            var events = await scopedEvents.Where(e => e.LicenseId == id)
                .OrderByDescending(e => e.OccurredAt).Take(100).Select(e => new { e.EventType, e.Details, e.OccurredAt,
                    employee = db.Employees.Where(p => p.CompanyId == companyId && p.Id == e.EmployeeId).Select(p => p.DisplayName).FirstOrDefault(),
                    assetTag = db.Assets.Where(asset => asset.CompanyId == companyId && asset.Id == e.AssetId).Select(asset => asset.AssetTag).FirstOrDefault() }).ToListAsync(ct);
            var attachments = await db.SoftwareLicenseAttachments.AsNoTracking().Where(x => x.CompanyId == companyId && x.LicenseId == id)
                .OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.OriginalFileName, x.ContentType, x.Length, x.CreatedAt,
                    x.IsEulaDocument, x.IsCurrentEula, x.EulaVersion,
                    uploadedBy = db.Users.Where(actor => actor.Id == x.UploadedByUserId).Select(actor => actor.DisplayName).FirstOrDefault() }).ToListAsync(ct);
            var purchaseOrders = await db.SoftwareLicensePurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId && x.LicenseId == id)
                .OrderBy(x => x.IsArchived).ThenByDescending(x => x.OrderDate).ThenByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.SupplierId,
                    supplier = db.SupplierCatalogEntries.Where(s => s.CompanyId == companyId && s.Id == x.SupplierId).Select(s => s.Name).FirstOrDefault(),
                    x.OrderNumber, x.OrderDate, x.SeatsPurchased, x.UnitCost, x.Currency, x.Notes, x.IsArchived, x.CreatedAt, x.UpdatedAt })
                .ToListAsync(ct);
            var currentEula = attachments.Where(x => x.IsCurrentEula).Select(x => new { x.Id, x.OriginalFileName, x.ContentType, x.EulaVersion }).SingleOrDefault();
            return Results.Ok(new { license, assignments, events, attachments, purchaseOrders, currentEula, maxAttachmentCount = 30, maxAttachmentSizeBytes = LicenseAttachmentMaxBytes });
        }).RequireAuthorization("asset.read");

        api.MapPost("/software-licenses/{id:guid}/purchase-orders", async (Guid id, SoftwareLicensePurchaseOrderRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            if (allowedLocations.IsRestricted) return Results.Forbid();
            if (!await CanAccessLicenseAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            var license = await db.SoftwareLicenses.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (license is null) return Results.NotFound();
            if (license.IsArchived) return Results.Conflict(new { message = "Arşivlenmiş lisansa sipariş bağlamadan önce lisansı geri yükleyin." });
            var errors = Validate(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var supplier = await db.SupplierCatalogEntries.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == request.SupplierId && x.IsActive, ct);
            if (supplier is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["supplierId"] = ["Etkin bir şirket tedarikçisi seçin."] });
            var item = new SoftwareLicensePurchaseOrder { CompanyId = companyId, LicenseId = id, SupplierId = supplier.Id,
                OrderNumber = request.OrderNumber.Trim(), OrderDate = request.OrderDate, SeatsPurchased = request.SeatsPurchased,
                UnitCost = request.UnitCost, Currency = request.Currency.Trim().ToUpperInvariant(), Notes = Clean(request.Notes) };
            db.SoftwareLicensePurchaseOrders.Add(item);
            AddEvent(db, license, user, "PurchaseOrderLinked", $"Satın alma siparişi {item.OrderNumber} lisansla ilişkilendirildi.");
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "SoftwareLicense", TargetId = id,
                Action = "PurchaseOrderLinked", Details = $"Satın alma siparişi {item.OrderNumber} ilişkilendirildi." });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/software-licenses/{id}/purchase-orders/{item.Id}", new { item.Id });
        }).RequireAuthorization("asset.write");

        api.MapPut("/software-licenses/{id:guid}/purchase-orders/{purchaseOrderId:guid}", async (Guid id, Guid purchaseOrderId,
            SoftwareLicensePurchaseOrderRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            if (allowedLocations.IsRestricted) return Results.Forbid();
            if (!await CanAccessLicenseAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            var license = await db.SoftwareLicenses.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            var item = await db.SoftwareLicensePurchaseOrders.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.LicenseId == id && x.Id == purchaseOrderId, ct);
            if (license is null || item is null) return Results.NotFound();
            if (license.IsArchived || item.IsArchived) return Results.Conflict(new { message = "Arşivlenmiş lisans veya sipariş kaydı düzenlenemez." });
            var errors = Validate(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var supplier = await db.SupplierCatalogEntries.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == request.SupplierId && (x.IsActive || x.Id == item.SupplierId), ct);
            if (supplier is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["supplierId"] = ["Etkin bir şirket tedarikçisi seçin."] });
            item.SupplierId = supplier.Id; item.OrderNumber = request.OrderNumber.Trim(); item.OrderDate = request.OrderDate;
            item.SeatsPurchased = request.SeatsPurchased; item.UnitCost = request.UnitCost; item.Currency = request.Currency.Trim().ToUpperInvariant();
            item.Notes = Clean(request.Notes); item.UpdatedAt = DateTimeOffset.UtcNow;
            AddEvent(db, license, user, "PurchaseOrderUpdated", $"Satın alma siparişi {item.OrderNumber} güncellendi.");
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "SoftwareLicense", TargetId = id,
                Action = "PurchaseOrderUpdated", Details = $"Satın alma siparişi {item.OrderNumber} güncellendi." });
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization("asset.write");

        foreach (var action in new[] { "archive", "restore" })
        {
            var isArchive = action == "archive";
            api.MapPost($"/software-licenses/{{id:guid}}/purchase-orders/{{purchaseOrderId:guid}}/{action}", async (Guid id, Guid purchaseOrderId,
                ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
            {
                if (!Company(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
                var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
                if (allowedLocations.IsRestricted) return Results.Forbid();
                if (!await CanAccessLicenseAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
                var license = await db.SoftwareLicenses.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
                var item = await db.SoftwareLicensePurchaseOrders.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.LicenseId == id && x.Id == purchaseOrderId, ct);
                if (license is null || item is null) return Results.NotFound();
                if (license.IsArchived) return Results.Conflict(new { message = "Önce lisansı geri yükleyin." });
                if (item.IsArchived == isArchive) return Results.NoContent();
                item.IsArchived = isArchive; item.UpdatedAt = DateTimeOffset.UtcNow;
                var eventType = isArchive ? "PurchaseOrderArchived" : "PurchaseOrderRestored";
                AddEvent(db, license, user, eventType, $"Satın alma siparişi {item.OrderNumber} {(isArchive ? "arşivlendi" : "geri yüklendi")}.");
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "SoftwareLicense", TargetId = id,
                    Action = eventType, Details = item.OrderNumber });
                await db.SaveChangesAsync(ct);
                return Results.NoContent();
            }).RequireAuthorization("asset.write");
        }

        api.MapPost("/software-licenses/{id:guid}/attachments", async (Guid id, HttpRequest request, ClaimsPrincipal user,
            AppDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            if (allowedLocations.IsRestricted) return Results.Forbid();
            if (!await CanAccessLicenseAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            if (!request.HasFormContentType) return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
            var form = await request.ReadFormAsync(ct);
            var upload = form.Files.GetFile("file");
            var isEulaDocument = string.Equals(form["isEulaDocument"], "true", StringComparison.OrdinalIgnoreCase);
            var eulaVersion = isEulaDocument ? form["eulaVersion"].ToString().Trim() : null;
            if (upload is null || upload.Length is < 1 or > LicenseAttachmentMaxBytes)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Lisans dosyası 1 bayt ile 10 MB arasında olmalıdır."] });
            if (isEulaDocument && (string.IsNullOrWhiteSpace(eulaVersion) || eulaVersion.Length > 80))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["eulaVersion"] = ["EULA belgesi için 1 ile 80 karakter arasında sürüm girin."] });
            await using var buffer = new MemoryStream((int)upload.Length);
            await upload.CopyToAsync(buffer, ct);
            var bytes = buffer.ToArray();
            var detected = DetectLicenseAttachment(bytes);
            if (detected is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Yalnızca gerçek PDF, PNG, JPEG veya WebP dosyaları yüklenebilir."] });

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var license = await db.SoftwareLicenses.FromSqlInterpolated($"SELECT * FROM \"SoftwareLicenses\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (license is null) return Results.NotFound();
            if (license.IsArchived) return Results.Conflict(new { message = "Arşivlenmiş lisansa dosya eklemeden önce lisansı geri yükleyin." });
            if (await db.SoftwareLicenseAttachments.CountAsync(x => x.CompanyId == companyId && x.LicenseId == id, ct) >= 30)
                return Results.Conflict(new { message = "Bir lisansa en fazla 30 dosya eklenebilir." });
            if (isEulaDocument)
            {
                var previousCurrent = await db.SoftwareLicenseAttachments.Where(x => x.CompanyId == companyId && x.LicenseId == id && x.IsCurrentEula).ToListAsync(ct);
                foreach (var previous in previousCurrent) previous.IsCurrentEula = false;
                if (previousCurrent.Count > 0) await db.SaveChangesAsync(ct);
            }

            var attachmentId = Guid.NewGuid();
            var storageName = $"{attachmentId:N}.{detected.Value.Extension}";
            var originalName = CleanLicenseAttachmentName(upload.FileName, detected.Value.Extension);
            var directory = LicenseAttachmentDirectory(configuration, companyId, id);
            var destination = Path.Combine(directory, storageName);
            if (!CompanyMediaPathGuard.IsSafePath(configuration, companyId, destination))
                return Results.Conflict(new { message = "Dosya alanı güvenli biçimde kullanılamıyor. Şirket yöneticinize başvurun." });
            Directory.CreateDirectory(directory);
            var attachment = new SoftwareLicenseAttachment { Id = attachmentId, CompanyId = companyId, LicenseId = id, UploadedByUserId = actorId,
                OriginalFileName = originalName, StorageFileName = storageName, ContentType = detected.Value.ContentType, Length = bytes.LongLength,
                IsEulaDocument = isEulaDocument, IsCurrentEula = isEulaDocument, EulaVersion = eulaVersion,
                Sha256 = isEulaDocument ? Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() : null };
            try
            {
                await System.IO.File.WriteAllBytesAsync(destination, bytes, ct);
                db.SoftwareLicenseAttachments.Add(attachment);
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "SoftwareLicense", TargetId = id,
                    Action = "AttachmentAdded", Details = $"Lisans dosyası eklendi ({detected.Value.ContentType}, {bytes.LongLength} bayt)." });
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                try { if (System.IO.File.Exists(destination)) System.IO.File.Delete(destination); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                throw;
            }
            return Results.Created($"/api/software-licenses/{id}/attachments/{attachmentId}/file", new { attachment.Id, attachment.OriginalFileName,
                attachment.ContentType, attachment.Length, attachment.CreatedAt, attachment.IsEulaDocument, attachment.IsCurrentEula, attachment.EulaVersion });
        }).RequireAuthorization("asset.write").WithMetadata(new RequestSizeLimitAttribute(LicenseAttachmentRequestBytes));

        api.MapGet("/software-licenses/{id:guid}/attachments/{attachmentId:guid}/file", async (Guid id, Guid attachmentId,
            ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            var scopedAssignments = ApplyAssignmentLocationScope(db, companyId, allowedLocations);
            var attachment = await db.SoftwareLicenseAttachments.AsNoTracking().Where(x => !allowedLocations.IsRestricted ||
                    scopedAssignments.Any(a => a.LicenseId == x.LicenseId)).SingleOrDefaultAsync(x =>
                x.CompanyId == companyId && x.LicenseId == id && x.Id == attachmentId, ct);
            if (attachment is null || !IsSafeLicenseAttachmentName(attachment.StorageFileName)) return Results.NotFound();
            var path = Path.Combine(LicenseAttachmentDirectory(configuration, companyId, id), attachment.StorageFileName);
            if (!CompanyMediaPathGuard.IsSafePath(configuration, companyId, path) || !System.IO.File.Exists(path))
                return Results.NotFound();
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            return Results.File(stream, attachment.ContentType, attachment.OriginalFileName, enableRangeProcessing: false);
        }).RequireAuthorization("asset.read");

        api.MapDelete("/software-licenses/{id:guid}/attachments/{attachmentId:guid}", async (Guid id, Guid attachmentId,
            ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            if (allowedLocations.IsRestricted) return Results.Forbid();
            if (!await CanAccessLicenseAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            var attachment = await db.SoftwareLicenseAttachments.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.LicenseId == id && x.Id == attachmentId, ct);
            if (attachment is null) return Results.NotFound();
            if (await db.SoftwareLicenses.AnyAsync(x => x.CompanyId == companyId && x.Id == id && x.IsArchived, ct))
                return Results.Conflict(new { message = "Arşivlenmiş lisansın geçmiş dosyaları korunur; silmek için önce lisansı geri yükleyin." });
            if (attachment.IsCurrentEula && await db.SoftwareLicenses.AnyAsync(x => x.CompanyId == companyId && x.Id == id && x.RequiresEulaAcceptance, ct))
                return Results.Conflict(new { message = "Atamada zorunlu tutulan geçerli EULA silinemez. Önce yeni bir EULA sürümü yükleyin veya lisans ayarını kapatın." });
            if (await db.SoftwareLicenseAssignments.AnyAsync(x => x.CompanyId == companyId && x.LicenseId == id && x.EulaAttachmentId == attachmentId, ct))
                return Results.Conflict(new { message = "Kabul kaydında kullanılan EULA sürümü silinemez; denetim kaydı için saklanmalıdır." });
            if (!IsSafeLicenseAttachmentName(attachment.StorageFileName)) return Results.NotFound();
            var path = Path.Combine(LicenseAttachmentDirectory(configuration, companyId, id), attachment.StorageFileName);
            if (!CompanyMediaPathGuard.IsSafePath(configuration, companyId, path)) return Results.NotFound();
            db.SoftwareLicenseAttachments.Remove(attachment);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "SoftwareLicense", TargetId = id,
                Action = "AttachmentRemoved", Details = $"Lisans dosyası kaldırıldı ({attachment.ContentType}, {attachment.Length} bayt)." });
            await db.SaveChangesAsync(ct);
            try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            return Results.NoContent();
        }).RequireAuthorization("asset.write");

        api.MapPost("/software-licenses", async (LicenseRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            if (allowedLocations.IsRestricted) return Results.Forbid();
            var errors = Validate(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var item = new SoftwareLicense { CompanyId = companyId, Name = request.Name.Trim(), Publisher = Clean(request.Publisher), ProductVersion = Clean(request.ProductVersion),
                LicenseType = request.LicenseType, LicenseReference = Clean(request.LicenseReference), Supplier = Clean(request.Supplier),
                PurchaseOrderReference = Clean(request.PurchaseOrderReference),
                SupplierId = await SupplierCatalogEndpoints.ResolveSupplierIdAsync(db, companyId, request.Supplier, ct), Seats = request.Seats,
                UnitCost = request.UnitCost, Currency = request.Currency.Trim().ToUpperInvariant(), PurchaseDate = request.PurchaseDate, ExpirationDate = request.ExpirationDate,
                AutoRenew = request.AutoRenew, RequiresEulaAcceptance = request.RequiresEulaAcceptance, Notes = Clean(request.Notes) };
            db.SoftwareLicenses.Add(item); AddEvent(db, item, user, "Created", "Lisans kaydı oluşturuldu.");
            await db.SaveChangesAsync(ct); return Results.Created($"/api/software-licenses/{item.Id}", new { item.Id });
        }).RequireAuthorization("asset.write");

        api.MapPut("/software-licenses/{id:guid}", async (Guid id, LicenseRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            if (allowedLocations.IsRestricted) return Results.Forbid();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var item = await db.SoftwareLicenses.FromSqlInterpolated($"SELECT * FROM \"SoftwareLicenses\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct); if (item is null) return Results.NotFound();
            if (!await CanAccessLicenseAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            if (item.IsArchived) return Results.Conflict(new { message = "Arşivlenmiş lisansı düzenlemeden önce geri yükleyin." });
            var errors = Validate(request); var active = await db.SoftwareLicenseAssignments.CountAsync(a => a.CompanyId == companyId && a.LicenseId == id && a.RevokedAt == null, ct);
            if (request.Seats < active) errors["seats"] = [$"Koltuk sayısı mevcut {active} atamadan düşük olamaz."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            item.Name = request.Name.Trim(); item.Publisher = Clean(request.Publisher); item.ProductVersion = Clean(request.ProductVersion); item.LicenseType = request.LicenseType;
            item.LicenseReference = Clean(request.LicenseReference); item.Supplier = Clean(request.Supplier); item.SupplierId = await SupplierCatalogEndpoints.ResolveSupplierIdAsync(db, companyId, request.Supplier, ct); item.PurchaseOrderReference = Clean(request.PurchaseOrderReference); item.Seats = request.Seats; item.UnitCost = request.UnitCost;
            item.Currency = request.Currency.Trim().ToUpperInvariant(); item.PurchaseDate = request.PurchaseDate; item.ExpirationDate = request.ExpirationDate;
            item.AutoRenew = request.AutoRenew; item.RequiresEulaAcceptance = request.RequiresEulaAcceptance; item.Notes = Clean(request.Notes); item.UpdatedAt = DateTimeOffset.UtcNow;
            AddEvent(db, item, user, "Updated", "Lisans bilgileri güncellendi."); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return Results.NoContent();
        }).RequireAuthorization("asset.write");

        api.MapPost("/software-licenses/{id:guid}/archive", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            if (allowedLocations.IsRestricted) return Results.Forbid();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var item = await db.SoftwareLicenses.FromSqlInterpolated($"SELECT * FROM \"SoftwareLicenses\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (item is null) return Results.NotFound();
            if (!await CanAccessLicenseAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            if (item.IsArchived) return Results.NoContent();
            var activeAssignments = await db.SoftwareLicenseAssignments.CountAsync(a => a.CompanyId == companyId && a.LicenseId == id && a.RevokedAt == null, ct);
            if (activeAssignments > 0)
                return Results.Conflict(new { message = $"Lisansın {activeAssignments} etkin ataması var. Önce etkin atamaları kaldırın; hiçbir kayıt değiştirilmedi.", activeAssignments });
            item.IsArchived = true;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            AddEvent(db, item, user, "Archived", "Lisans kaydı arşivlendi; geçmiş ve dosyalar korundu.");
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "SoftwareLicense", TargetId = id,
                Action = "license.archived", Details = item.Name });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization("asset.write");

        api.MapPost("/software-licenses/{id:guid}/restore", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            if (allowedLocations.IsRestricted) return Results.Forbid();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var item = await db.SoftwareLicenses.FromSqlInterpolated($"SELECT * FROM \"SoftwareLicenses\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (item is null) return Results.NotFound();
            if (!await CanAccessLicenseAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            if (!item.IsArchived) return Results.NoContent();
            item.IsArchived = false;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            AddEvent(db, item, user, "Restored", "Lisans kaydı arşivden geri yüklendi.");
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "SoftwareLicense", TargetId = id,
                Action = "license.restored", Details = item.Name });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization("asset.write");

        api.MapPost("/software-licenses/{id:guid}/assign", async (Guid id, AssignmentRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var item = await db.SoftwareLicenses.FromSqlInterpolated($"SELECT * FROM \"SoftwareLicenses\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct); if (item is null) return Results.NotFound();
            if (!await CanAccessLicenseAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            if (item.IsArchived) return Results.Conflict(new { message = "Arşivlenmiş lisansa atama yapılamaz. Önce lisansı geri yükleyin." });
            if ((request.EmployeeId is null) == (request.AssetId is null))
                return Results.ValidationProblem(new Dictionary<string,string[]> { ["target"]=["Tek bir çalışan veya varlık seçilmelidir."] });
            if (!await IsAssignmentTargetInScopeAsync(db, companyId, request.EmployeeId, request.AssetId, allowedLocations, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["target"] = ["Seçilen çalışan veya varlık erişim kapsamınızdaki bir konumda olmalıdır."] });
            if (item.RequiresEulaAcceptance && !request.EulaAccepted)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["eulaAccepted"] = ["Bu lisans için kullanıcı EULA kabulünün alındığını teyit etmelisiniz."] });
            var currentEula = item.RequiresEulaAcceptance
                ? await db.SoftwareLicenseAttachments.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.LicenseId == id && x.IsCurrentEula, ct)
                : null;
            if (item.RequiresEulaAcceptance && currentEula is null)
                return Results.Conflict(new { message = "EULA kabulü zorunlu. Atama yapmadan önce lisansın güncel EULA belgesini ve sürümünü ekleyin." });
            if (request.EmployeeId is Guid employeeId && !await db.Employees.AnyAsync(e => e.CompanyId == companyId && e.Id == employeeId && e.IsActive, ct))
                return Results.ValidationProblem(new Dictionary<string,string[]> { ["employeeId"]=["Aktif çalışan seçilmelidir."] });
            if (request.AssetId is Guid assetId && !await db.Assets.AnyAsync(a => a.CompanyId == companyId && a.Id == assetId && a.Status != AssetStatuses.Archived, ct))
                return Results.ValidationProblem(new Dictionary<string,string[]> { ["assetId"]=["Arşivlenmemiş şirket varlığı seçilmelidir."] });
            if (await db.SoftwareLicenseAssignments.AnyAsync(a => a.CompanyId == companyId && a.LicenseId == id && a.RevokedAt == null &&
                    ((request.EmployeeId != null && a.EmployeeId == request.EmployeeId) || (request.AssetId != null && a.AssetId == request.AssetId)), ct))
                return Results.Conflict(new { message = "Bu lisans seçilen hedefe zaten atanmış." });
            var count = await db.SoftwareLicenseAssignments.CountAsync(a => a.CompanyId == companyId && a.LicenseId == id && a.RevokedAt == null, ct);
            if (count >= item.Seats) return Results.Conflict(new { message = "Kullanılabilir lisans koltuğu kalmadı." });
            var visibleAssigned = allowedLocations.IsUnrestricted ? count : await ApplyAssignmentLocationScope(db, companyId, allowedLocations)
                .CountAsync(a => a.LicenseId == id && a.RevokedAt == null, ct);
            var actorId = UserId(user);
            var assignment = new SoftwareLicenseAssignment { CompanyId = companyId, LicenseId = id, EmployeeId = request.EmployeeId, AssetId = request.AssetId,
                ActorUserId = actorId, EulaAcceptedAt = currentEula is not null && request.EulaAccepted ? DateTimeOffset.UtcNow : null,
                EulaAcceptedByUserId = currentEula is not null && request.EulaAccepted ? actorId : null,
                EulaAttachmentId = currentEula?.Id, EulaVersion = currentEula?.EulaVersion, Notes = Clean(request.Notes) };
            db.SoftwareLicenseAssignments.Add(assignment);
            var targetDetails = request.EmployeeId is Guid employeeTarget
                ? $"Çalışana atandı: {employeeTarget}"
                : $"Varlığa atandı: {request.AssetId}";
            if (currentEula is not null && request.EulaAccepted) targetDetails += $"; EULA {currentEula.EulaVersion} kabulü yetkili tarafından teyit edildi";
            AddEvent(db, item, user, "Assigned", targetDetails, request.EmployeeId, request.AssetId);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Results.Created($"/api/software-licenses/{id}", new
            { assignment.Id, assigned = visibleAssigned + 1, available = allowedLocations.IsUnrestricted ? item.Seats - count - 1 : (int?)null });
        }).RequireAuthorization("asset.write");

        api.MapPost("/software-licenses/{id:guid}/assign/bulk", async (Guid id, BulkAssignmentRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            var employeeIds = request.EmployeeIds ?? [];
            if (employeeIds.Length is < 1 or > 100 || employeeIds.Distinct().Count() != employeeIds.Length || request.Notes?.Length > 2000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeIds"] = ["1 ile 100 arasında, yinelenmeyen çalışan seçin; notlar en fazla 2.000 karakter olabilir."] });

            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var item = await db.SoftwareLicenses.FromSqlInterpolated($"SELECT * FROM \"SoftwareLicenses\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (item is null) return Results.NotFound();
            if (!await CanAccessLicenseAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            if (item.IsArchived) return Results.Conflict(new { message = "Arşivlenmiş lisansa toplu atama yapılamaz. Önce lisansı geri yükleyin." });
            if (item.RequiresEulaAcceptance && !request.EulaAccepted)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["eulaAccepted"] = ["Bu lisans için her seçili kullanıcıdan EULA kabulü alındığını teyit etmelisiniz."] });
            var currentEula = item.RequiresEulaAcceptance
                ? await db.SoftwareLicenseAttachments.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.LicenseId == id && x.IsCurrentEula, ct)
                : null;
            if (item.RequiresEulaAcceptance && currentEula is null)
                return Results.Conflict(new { message = "EULA kabulü zorunlu. Toplu atama öncesinde güncel EULA belgesini ve sürümünü ekleyin." });

            var activeEmployees = await db.Employees.AsNoTracking().Where(e => e.CompanyId == companyId && e.IsActive && employeeIds.Contains(e.Id))
                .Select(e => e.Id).ToListAsync(ct);
            if (activeEmployees.Count != employeeIds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeIds"] = ["Seçilen çalışanların tümü etkin ve bu şirkete ait olmalıdır."] });
            var visibleEmployeeCount = await ApplyEmployeeScope(
                db.Employees.AsNoTracking().Where(e => e.CompanyId == companyId && employeeIds.Contains(e.Id)), allowedLocations)
                .CountAsync(ct);
            if (allowedLocations.IsRestricted && visibleEmployeeCount != employeeIds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeIds"] = ["Seçilen çalışanların tümü erişim kapsamınızdaki konumlarda olmalıdır."] });

            var alreadyAssigned = await db.SoftwareLicenseAssignments.AnyAsync(a => a.CompanyId == companyId && a.LicenseId == id &&
                a.RevokedAt == null && a.EmployeeId != null && employeeIds.Contains(a.EmployeeId.Value), ct);
            if (alreadyAssigned) return Results.Conflict(new { message = "Seçilen çalışanlardan en az birine bu lisans zaten atanmış; hiçbir atama yapılmadı." });

            var assigned = await db.SoftwareLicenseAssignments.CountAsync(a => a.CompanyId == companyId && a.LicenseId == id && a.RevokedAt == null, ct);
            if (assigned + employeeIds.Length > item.Seats)
                return Results.Conflict(new { message = allowedLocations.IsUnrestricted
                    ? $"Yeterli lisans koltuğu yok. Kullanılabilir: {Math.Max(0, item.Seats - assigned)}, istenen: {employeeIds.Length}; hiçbir atama yapılmadı."
                    : "Yeterli lisans koltuğu yok; hiçbir atama yapılmadı." });
            var visibleAssigned = allowedLocations.IsUnrestricted ? assigned : await ApplyAssignmentLocationScope(db, companyId, allowedLocations)
                .CountAsync(a => a.LicenseId == id && a.RevokedAt == null, ct);

            var now = DateTimeOffset.UtcNow;
            var actorId = UserId(user);
            var eulaAcceptedAt = currentEula is not null && request.EulaAccepted ? now : (DateTimeOffset?)null;
            var assignments = employeeIds.Select(employeeId => new SoftwareLicenseAssignment
            {
                CompanyId = companyId, LicenseId = id, EmployeeId = employeeId, ActorUserId = actorId,
                AssignedAt = now, EulaAcceptedAt = eulaAcceptedAt, EulaAcceptedByUserId = eulaAcceptedAt is not null ? actorId : null,
                EulaAttachmentId = currentEula?.Id, EulaVersion = currentEula?.EulaVersion, Notes = Clean(request.Notes)
            }).ToArray();
            db.SoftwareLicenseAssignments.AddRange(assignments);
            foreach (var employeeId in employeeIds)
                AddEvent(db, item, user, "Assigned", $"Toplu atama: çalışan {employeeId}" +
                    (currentEula is not null && request.EulaAccepted ? $"; EULA {currentEula.EulaVersion} kabulü yetkili tarafından teyit edildi" : ""), employeeId);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Results.Created($"/api/software-licenses/{id}", new
            {
                assignments = assignments.Select(a => new { a.Id, a.EmployeeId, a.AssignedAt }),
                assigned = visibleAssigned + assignments.Length,
                available = allowedLocations.IsUnrestricted ? item.Seats - assigned - assignments.Length : (int?)null
            });
        }).RequireAuthorization("asset.write");

        api.MapPost("/software-licenses/{id:guid}/revoke", async (Guid id, AssignmentRevokeRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            var item = await db.SoftwareLicenses.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct); if (item is null) return Results.NotFound();
            if (!await CanAccessLicenseAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            var scopedAssignments = ApplyAssignmentLocationScope(db, companyId, allowedLocations);
            if (!await scopedAssignments.AnyAsync(a => a.LicenseId == id && a.Id == request.AssignmentId && a.RevokedAt == null, ct))
                return Results.NotFound();
            var assignment = await db.SoftwareLicenseAssignments.SingleOrDefaultAsync(a => a.CompanyId == companyId &&
                a.LicenseId == id && a.Id == request.AssignmentId && a.RevokedAt == null, ct);
            if (assignment is null) return Results.NotFound();
            assignment.RevokedAt = DateTimeOffset.UtcNow; AddEvent(db, item, user, "Revoked", "Lisans ataması kaldırıldı.", assignment.EmployeeId, assignment.AssetId);
            await db.SaveChangesAsync(ct); return Results.NoContent();
        }).RequireAuthorization("asset.write");

        api.MapGet("/software-license-assignments", async ([FromQuery] string? q, [FromQuery] bool? active,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            var allowedLocations = await GetAllowedLocationsAsync(db, companyId, user, ct);
            var scopedAssignments = ApplyAssignmentLocationScope(db, companyId, allowedLocations);
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 200);
            var query = scopedAssignments;
            if (active == true) query = query.Where(a => a.RevokedAt == null);
            if (active == false) query = query.Where(a => a.RevokedAt != null);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim(); if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string,string[]> { ["q"]=["Arama metni 120 karakteri aşamaz."] });
                query = query.Where(a => db.Employees.Any(e => e.CompanyId == companyId && e.Id == a.EmployeeId && (e.DisplayName.Contains(term) || e.EmployeeNumber.Contains(term))) ||
                    db.Assets.Any(asset => asset.CompanyId == companyId && asset.Id == a.AssetId && (asset.AssetTag.Contains(term) || asset.Name.Contains(term))) ||
                    db.SoftwareLicenses.Any(l => l.CompanyId == companyId && l.Id == a.LicenseId && (l.Name.Contains(term) || (l.Publisher != null && l.Publisher.Contains(term)))));
            }
            var total = await query.CountAsync(ct);
            var items = await query.OrderBy(a => a.RevokedAt != null).ThenByDescending(a => a.AssignedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).Select(a => new
                {
                    a.Id, a.LicenseId, license = db.SoftwareLicenses.Where(l => l.CompanyId == companyId && l.Id == a.LicenseId).Select(l => l.Name).FirstOrDefault(),
                    licenseType = db.SoftwareLicenses.Where(l => l.CompanyId == companyId && l.Id == a.LicenseId).Select(l => l.LicenseType).FirstOrDefault(),
                    a.EmployeeId, a.AssetId, employee = db.Employees.Where(e => e.CompanyId == companyId && e.Id == a.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                    employeeNumber = db.Employees.Where(e => e.CompanyId == companyId && e.Id == a.EmployeeId).Select(e => e.EmployeeNumber).FirstOrDefault(),
                    assetTag = db.Assets.Where(asset => asset.CompanyId == companyId && asset.Id == a.AssetId).Select(asset => asset.AssetTag).FirstOrDefault(),
                    assetName = db.Assets.Where(asset => asset.CompanyId == companyId && asset.Id == a.AssetId).Select(asset => asset.Name).FirstOrDefault(),
                    a.AssignedAt, a.RevokedAt, a.Notes,
                    expirationDate = db.SoftwareLicenses.Where(l => l.CompanyId == companyId && l.Id == a.LicenseId).Select(l => l.ExpirationDate).FirstOrDefault()
                }).ToListAsync(ct);
            return Results.Ok(new { items, page, pageSize, total });
        }).RequireAuthorization("asset.read");
        return api;
    }

    private static bool TryGetLicenseReportFilters(DateOnly? throughInput, string? stateInput, string? queryInput,
        out DateOnly today, out DateOnly through, out string state, out string? term, out IResult? error)
    {
        today = DateOnly.FromDateTime(DateTime.UtcNow);
        through = throughInput ?? today.AddDays(90);
        state = string.IsNullOrWhiteSpace(stateInput) ? "upcoming" : stateInput.Trim().ToLowerInvariant();
        term = string.IsNullOrWhiteSpace(queryInput) ? null : queryInput.Trim();
        error = null;
        if (state is not ("upcoming" or "expired" or "later" or "missing" or "all"))
        {
            error = Results.ValidationProblem(new Dictionary<string, string[]> { ["state"] = ["Durum upcoming, expired, later, missing veya all olmalıdır."] });
            return false;
        }
        if (through < today || through > today.AddDays(366))
        {
            error = Results.ValidationProblem(new Dictionary<string, string[]> { ["through"] = ["Bitiş tarihi bugünden başlayıp en fazla 366 gün sonrasını kapsamalıdır."] });
            return false;
        }
        if (term?.Length > 120)
        {
            error = Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
            return false;
        }
        return true;
    }

    private static IQueryable<SoftwareLicense> FilterLicenseReportSearch(IQueryable<SoftwareLicense> query, string? term)
    {
        if (term is null) return query;
        return query.Where(x => x.Name.Contains(term) || (x.Publisher != null && x.Publisher.Contains(term)) ||
            (x.ProductVersion != null && x.ProductVersion.Contains(term)) || (x.Supplier != null && x.Supplier.Contains(term)) ||
            (x.LicenseReference != null && x.LicenseReference.Contains(term)) ||
            (x.PurchaseOrderReference != null && x.PurchaseOrderReference.Contains(term)));
    }

    private static IQueryable<SoftwareLicense> FilterLicenseReportState(IQueryable<SoftwareLicense> query, string state,
        DateOnly today, DateOnly through) => state switch
    {
        "upcoming" => query.Where(x => x.ExpirationDate != null && x.ExpirationDate >= today && x.ExpirationDate <= through),
        "expired" => query.Where(x => x.ExpirationDate != null && x.ExpirationDate < today),
        "later" => query.Where(x => x.ExpirationDate != null && x.ExpirationDate > through),
        "missing" => query.Where(x => x.ExpirationDate == null),
        _ => query
    };

    private static bool Company(ClaimsPrincipal user, out Guid id) => Guid.TryParse(user.FindFirstValue("company_id"), out id);
    private static async Task<LicenseAccessScope> GetAllowedLocationsAsync(AppDbContext db, Guid companyId,
        ClaimsPrincipal user, CancellationToken ct)
    {
        var userId = UserId(user);
        if (userId is not Guid id) return new LicenseAccessScope(new EmployeeAccessScope([], [], true, true));
        return new LicenseAccessScope(await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, id, ct));
    }

    private static IQueryable<SoftwareLicenseAssignment> ApplyAssignmentLocationScope(AppDbContext db, Guid companyId,
        LicenseAccessScope scope)
    {
        var query = db.SoftwareLicenseAssignments.AsNoTracking().Where(a => a.CompanyId == companyId);
        if (!scope.IsRestricted) return query;
        var visibleEmployees = ApplyEmployeeScope(db.Employees.AsNoTracking().Where(e => e.CompanyId == companyId), scope);
        return query.Where(a => (a.EmployeeId != null || a.AssetId != null) &&
            (a.EmployeeId == null || visibleEmployees.Any(e => e.Id == a.EmployeeId)) &&
            (a.AssetId == null || db.Assets.Any(asset => asset.CompanyId == companyId && asset.Id == a.AssetId &&
                asset.DeletedAt == null && asset.LocationCatalogId.HasValue &&
                scope.EffectiveLocations!.Contains(asset.LocationCatalogId.Value))));
    }

    private static IQueryable<SoftwareLicenseEvent> ApplyEventLocationScope(AppDbContext db, Guid companyId,
        LicenseAccessScope scope)
    {
        var query = db.SoftwareLicenseEvents.AsNoTracking().Where(e => e.CompanyId == companyId);
        if (!scope.IsRestricted) return query;
        var visibleEmployees = ApplyEmployeeScope(db.Employees.AsNoTracking().Where(employee => employee.CompanyId == companyId), scope);
        return query.Where(e => (e.EmployeeId != null || e.AssetId != null) &&
            (e.EmployeeId == null || visibleEmployees.Any(employee => employee.Id == e.EmployeeId)) &&
            (e.AssetId == null || db.Assets.Any(asset => asset.CompanyId == companyId && asset.Id == e.AssetId &&
                asset.DeletedAt == null && asset.LocationCatalogId.HasValue &&
                scope.EffectiveLocations!.Contains(asset.LocationCatalogId.Value))));
    }

    private static async Task<bool> CanAccessLicenseAsync(AppDbContext db, Guid companyId, Guid licenseId,
        LicenseAccessScope scope, CancellationToken ct)
    {
        if (!scope.IsRestricted) return true;
        return await ApplyAssignmentLocationScope(db, companyId, scope).AnyAsync(a => a.LicenseId == licenseId, ct);
    }

    private static async Task<bool> IsAssignmentTargetInScopeAsync(AppDbContext db, Guid companyId, Guid? employeeId,
        Guid? assetId, LicenseAccessScope scope, CancellationToken ct)
    {
        if (!scope.IsRestricted) return true;
        if (employeeId is Guid employee)
            return await ApplyEmployeeScope(db.Employees.AsNoTracking().Where(e => e.CompanyId == companyId && e.Id == employee), scope)
                .AnyAsync(ct);
        if (assetId is Guid asset)
            return await db.Assets.AsNoTracking().AnyAsync(a => a.CompanyId == companyId && a.Id == asset &&
                a.DeletedAt == null && a.LocationCatalogId.HasValue &&
                scope.EffectiveLocations!.Contains(a.LocationCatalogId.Value), ct);
        return false;
    }

    private static IQueryable<Employee> ApplyEmployeeScope(IQueryable<Employee> query, LicenseAccessScope scope)
        => scope.EmployeeAccess.Apply(query);

    private sealed record LicenseAccessScope(EmployeeAccessScope EmployeeAccess)
    {
        public HashSet<Guid>? EffectiveLocations => EmployeeAccess.AllowedLocations;
        public HashSet<Guid>? AllowedCompanyUnits => EmployeeAccess.AllowedCompanyUnits;
        public bool RestrictLocations => EmployeeAccess.RestrictLocations;
        public bool RestrictCompanyUnits => EmployeeAccess.RestrictCompanyUnits;
        public bool IsRestricted => EmployeeAccess.IsRestricted;
        public bool IsUnrestricted => !IsRestricted;
    }
    internal static string LicenseAttachmentDirectory(IConfiguration configuration, Guid companyId, Guid licenseId) =>
        Path.Combine(configuration["Storage:CompanyMediaPath"] ?? "/var/lib/envantertr/company-media", companyId.ToString("N"), "licenses", licenseId.ToString("N"), "attachments");
    private static string CleanLicenseAttachmentName(string? fileName, string extension)
    {
        var safe = Path.GetFileNameWithoutExtension(Path.GetFileName((fileName ?? "dosya").Replace('\\', '/')));
        var filtered = new string(safe.Where(c => !char.IsControl(c) && c is not '/' and not '\\').ToArray()).Trim();
        if (filtered.Length > 175) filtered = filtered[..175];
        return $"{(string.IsNullOrWhiteSpace(filtered) ? "dosya" : filtered)}.{extension}";
    }
    internal static bool IsSafeLicenseAttachmentName(string name) => Regex.IsMatch(name, "^[a-fA-F0-9]{32}\\.(pdf|png|jpg|webp)$", RegexOptions.CultureInvariant);
    private static (string ContentType, string Extension)? DetectLicenseAttachment(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 5 && bytes[..5].SequenceEqual("%PDF-"u8)) return ("application/pdf", "pdf");
        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ("image/png", "png");
        if (bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff) return ("image/jpeg", "jpg");
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8)) return ("image/webp", "webp");
        return null;
    }
    private static Guid? UserId(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static string CsvCell(string? value)
    {
        var safe = value?.Trim() ?? "";
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@' or '\t' or '\r') safe = "'" + safe;
        return "\"" + safe.Replace("\"", "\"\"") + "\"";
    }
    private static void AddEvent(AppDbContext db, SoftwareLicense license, ClaimsPrincipal user, string type, string details, Guid? employeeId = null, Guid? assetId = null) => db.SoftwareLicenseEvents.Add(new SoftwareLicenseEvent { CompanyId = license.CompanyId, LicenseId = license.Id, ActorUserId = UserId(user), EmployeeId = employeeId, AssetId = assetId, EventType = type, Details = details });
    private static Dictionary<string,string[]> Validate(LicenseRequest r)
    {
        var e = new Dictionary<string,string[]>(); void Add(string k,string v)=>e[k]=[v];
        if (string.IsNullOrWhiteSpace(r.Name) || r.Name.Trim().Length > 200) Add("name","Lisans adı zorunludur ve 200 karakteri aşamaz.");
        if (r.Seats is < 1 or > 100000) Add("seats","Koltuk sayısı 1 ile 100.000 arasında olmalı.");
        if (r.UnitCost < 0) Add("unitCost","Birim maliyeti negatif olamaz.");
        if (!new[] { "Subscription", "Perpetual", "Device", "Site", "Trial", "Other" }.Contains(r.LicenseType)) Add("licenseType","Lisans türü desteklenmiyor.");
        if (string.IsNullOrWhiteSpace(r.Currency) || r.Currency.Trim().Length != 3 || r.Currency.Any(c => !char.IsAsciiLetter(c))) Add("currency","Para birimi üç harfli kod olmalı.");
        if (r.ExpirationDate is DateOnly expires && r.PurchaseDate is DateOnly purchased && expires < purchased) Add("expirationDate","Bitiş tarihi alış tarihinden önce olamaz.");
        if (r.Publisher?.Length > 160) Add("publisher","Yayıncı 160 karakteri aşamaz."); if (r.ProductVersion?.Length > 120) Add("productVersion","Sürüm 120 karakteri aşamaz.");
        if (r.LicenseReference?.Length > 160) Add("licenseReference","Lisans referansı 160 karakteri aşamaz."); if (r.Supplier?.Length > 200) Add("supplier","Tedarikçi 200 karakteri aşamaz."); if (r.Notes?.Length > 2000) Add("notes","Notlar 2.000 karakteri aşamaz.");
        if (r.PurchaseOrderReference?.Length > 120) Add("purchaseOrderReference","Satın alma sipariş numarası 120 karakteri aşamaz.");
        return e;
    }
    private static Dictionary<string,string[]> Validate(SoftwareLicensePurchaseOrderRequest r)
    {
        var e = new Dictionary<string,string[]>(); void Add(string k,string v)=>e[k]=[v];
        if (r.SupplierId == Guid.Empty) Add("supplierId","Tedarikçi seçilmelidir.");
        if (string.IsNullOrWhiteSpace(r.OrderNumber) || r.OrderNumber.Trim().Length > 120) Add("orderNumber","Sipariş numarası zorunludur ve 120 karakteri aşamaz.");
        if (r.SeatsPurchased is < 1 or > 100000) Add("seatsPurchased","Satın alınan koltuk sayısı 1 ile 100.000 arasında olmalıdır.");
        if (r.UnitCost < 0) Add("unitCost","Birim maliyeti negatif olamaz.");
        if (string.IsNullOrWhiteSpace(r.Currency) || r.Currency.Trim().Length != 3 || r.Currency.Any(c => !char.IsAsciiLetter(c))) Add("currency","Para birimi üç harfli kod olmalıdır.");
        if (r.Notes?.Length > 2000) Add("notes","Notlar 2.000 karakteri aşamaz.");
        return e;
    }
    public sealed record LicenseRequest(string Name, string? Publisher, string? ProductVersion, string LicenseType, string? LicenseReference,
        string? Supplier, int Seats, decimal? UnitCost, string Currency, DateOnly? PurchaseDate, DateOnly? ExpirationDate, bool AutoRenew, string? Notes,
        bool RequiresEulaAcceptance = false, string? PurchaseOrderReference = null);
    public sealed record SoftwareLicensePurchaseOrderRequest(Guid SupplierId, string OrderNumber, DateOnly? OrderDate,
        int SeatsPurchased, decimal? UnitCost, string Currency, string? Notes);
    public sealed record AssignmentRequest(Guid? EmployeeId, Guid? AssetId, string? Notes, bool EulaAccepted = false);
    public sealed record BulkAssignmentRequest(Guid[]? EmployeeIds, string? Notes, bool EulaAccepted = false);
    public sealed record AssignmentRevokeRequest(Guid AssignmentId);
}
