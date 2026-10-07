using System.Security.Claims;
using System.Runtime.CompilerServices;
using System.Text;
using System.Globalization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AssetEndpoints
{
    private const long AssetAttachmentMaxSizeBytes = 10_485_760;
    private const long AssetAttachmentRequestLimitBytes = 10_616_832;

    public static IEndpointRouteBuilder MapAssetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api").RequireAuthorization();
        api.AddEndpointFilter(async (invocation, next) =>
        {
            var user = invocation.HttpContext.User;
            if (!TryCompanyId(user, out var companyId) ||
                !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
                string.IsNullOrWhiteSpace(user.FindFirstValue("company_role"))) return Results.Forbid();
            var db = invocation.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var role = user.FindFirstValue("company_role");
            if (!await db.Companies.AsNoTracking().AnyAsync(x => x.Id == companyId && x.IsActive &&
                    db.CompanyMemberships.Any(m => m.CompanyId == companyId && m.UserId == userId && m.RoleKey == role),
                    invocation.HttpContext.RequestAborted)) return Results.Forbid();
            if (HttpMethods.IsGet(invocation.HttpContext.Request.Method) || HttpMethods.IsHead(invocation.HttpContext.Request.Method))
                return await next(invocation);
            var antiforgery = invocation.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            try { await antiforgery.ValidateRequestAsync(invocation.HttpContext); }
            catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
            return await next(invocation);
        });
        api.MapConsumableEndpoints();
        api.MapSoftwareLicenseEndpoints();
        api.MapComponentEndpoints();
        api.MapAccessoryImportEndpoints();
        api.MapInventoryMovementEndpoints();
        api.MapAccessoryPurchaseOrderEndpoints();
        api.MapSupplierCatalogEndpoints();
        api.MapAccessoryKitEndpoints();

        api.MapGet("/assets", async ([FromQuery] string? q, [FromQuery] string? status,
            [FromQuery] Guid? categoryId, [FromQuery] Guid? brandId, [FromQuery] Guid? modelId,
            [FromQuery] Guid? employeeId, [FromQuery] string? location, [FromQuery] string? warranty,
            [FromQuery] string? sortBy, [FromQuery] string? sortDirection,
            [FromQuery] int page, [FromQuery] int pageSize,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            var query = db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
            if (categoryId is Guid selectedCategory) query = query.Where(x => x.CategoryId == selectedCategory);
            if (brandId is Guid selectedBrand) query = query.Where(x => x.BrandId == selectedBrand);
            if (modelId is Guid selectedModel) query = query.Where(x => x.ModelId == selectedModel);
            if (employeeId is Guid selectedEmployee) query = query.Where(x => x.AssignedEmployeeId == selectedEmployee);
            if (!string.IsNullOrWhiteSpace(location))
            {
                var selectedLocation = location.Trim();
                if (selectedLocation.Length > 160) return Results.ValidationProblem(new Dictionary<string, string[]> { ["location"] = ["Konum filtresi 160 karakteri aşamaz."] });
                query = query.Where(x => x.Location == selectedLocation);
            }
            if (!TryApplyWarrantyFilter(query, warranty, out var warrantyFiltered))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["warranty"] = ["Garanti filtresi expired, expiring, active veya unspecified olmalıdır."] });
            query = warrantyFiltered;
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
                query = query.Where(x => x.AssetTag.Contains(term) || x.Name.Contains(term) ||
                    (x.SerialNumber != null && x.SerialNumber.Contains(term)) ||
                    (x.Location != null && x.Location.Contains(term)) ||
                    db.Employees.Any(e => e.CompanyId == companyId && e.Id == x.AssignedEmployeeId &&
                        (e.DisplayName.Contains(term) || e.EmployeeNumber.Contains(term))) ||
                    db.AssetCategories.Any(c => c.CompanyId == companyId && c.Id == x.CategoryId && c.Name.Contains(term)) ||
                    db.AssetBrands.Any(b => b.CompanyId == companyId && b.Id == x.BrandId && b.Name.Contains(term)) ||
                    db.AssetModels.Any(m => m.CompanyId == companyId && m.Id == x.ModelId && m.Name.Contains(term)));
            }

            var ordered = OrderAssets(query, db, companyId, sortBy, sortDirection);
            if (ordered is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["sortBy"] = ["Sıralama alanı veya yönü desteklenmiyor."] });
            var total = await query.CountAsync(cancellationToken);
            var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new AssetListItem(x.Id, x.AssetTag, x.Name, x.Status, x.SerialNumber,
                    x.Location, x.CategoryId,
                    db.AssetCategories.Where(c => c.CompanyId == companyId && c.Id == x.CategoryId).Select(c => c.Name).FirstOrDefault() ?? "",
                    x.BrandId,
                    db.AssetBrands.Where(b => b.CompanyId == companyId && b.Id == x.BrandId).Select(b => b.Name).FirstOrDefault(),
                    x.ModelId,
                    db.AssetModels.Where(m => m.CompanyId == companyId && m.Id == x.ModelId).Select(m => m.Name).FirstOrDefault(),
                    db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.AssignedEmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                    x.AssignedEmployeeId, x.WarrantyExpiresAt, x.UpdatedAt,
                    db.AssetAttachments.Where(photo => photo.CompanyId == companyId && photo.AssetId == x.Id && photo.ContentType.StartsWith("image/"))
                        .OrderByDescending(photo => photo.CreatedAt).Select(photo => (Guid?)photo.Id).FirstOrDefault(),
                    x.LocationCatalogId)).ToListAsync(cancellationToken);
            return Results.Ok(new { items, page, pageSize, total });
        }).RequireAuthorization("asset.read");

        api.MapGet("/accessories", async ([FromQuery] string? q, [FromQuery] string? category,
            [FromQuery] string? location, [FromQuery] string? stock, [FromQuery] string? sortBy,
            [FromQuery] string? sortDirection, [FromQuery] int page, [FromQuery] int pageSize,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 200);
            var query = db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
                query = query.Where(x => x.Name.Contains(term) || (x.Category != null && x.Category.Contains(term)) ||
                    (x.ModelNumber != null && x.ModelNumber.Contains(term)) || (x.ItemNumber != null && x.ItemNumber.Contains(term)) ||
                    (x.Supplier != null && x.Supplier.Contains(term)) || (x.Location != null && x.Location.Contains(term)) ||
                    db.AccessoryAssignments.Any(a => a.CompanyId == companyId && a.AccessoryId == x.Id &&
                        a.TargetType == "Location" && a.Quantity > a.ReturnedQuantity && a.Location != null && a.Location.Contains(term)));
            }
            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category == category.Trim());
            if (!string.IsNullOrWhiteSpace(location)) query = query.Where(x => x.Location == location.Trim());
            if (stock == "low") query = query.Where(x => x.TotalQuantity - (db.AccessoryAssignments
                .Where(a => a.CompanyId == companyId && a.AccessoryId == x.Id)
                .Sum(a => (int?)(a.Quantity - a.ReturnedQuantity)) ?? 0) <= x.MinimumQuantity);
            if (stock == "available") query = query.Where(x => x.TotalQuantity > (db.AccessoryAssignments
                .Where(a => a.CompanyId == companyId && a.AccessoryId == x.Id)
                .Sum(a => (int?)(a.Quantity - a.ReturnedQuantity)) ?? 0));
            var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            var sort = (sortBy ?? "name").Trim().ToLowerInvariant();
            if (sort is not ("name" or "category" or "location" or "total" or "remaining" or "updated"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["sortBy"] = ["Sıralama alanı desteklenmiyor."] });
            if (sortDirection is not null && !new[] { "asc", "desc" }.Contains(sortDirection.Trim(), StringComparer.OrdinalIgnoreCase))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["sortDirection"] = ["Sıralama yönü asc veya desc olmalı."] });
            IOrderedQueryable<Accessory> ordered = (sort, descending) switch
            {
                ("category", false) => query.OrderBy(x => x.Category), ("category", true) => query.OrderByDescending(x => x.Category),
                ("location", false) => query.OrderBy(x => x.Location), ("location", true) => query.OrderByDescending(x => x.Location),
                ("total", false) => query.OrderBy(x => x.TotalQuantity), ("total", true) => query.OrderByDescending(x => x.TotalQuantity),
                ("remaining", false) => query.OrderBy(x => x.TotalQuantity - (db.AccessoryAssignments.Where(a => a.CompanyId == companyId && a.AccessoryId == x.Id).Sum(a => (int?)(a.Quantity - a.ReturnedQuantity)) ?? 0)),
                ("remaining", true) => query.OrderByDescending(x => x.TotalQuantity - (db.AccessoryAssignments.Where(a => a.CompanyId == companyId && a.AccessoryId == x.Id).Sum(a => (int?)(a.Quantity - a.ReturnedQuantity)) ?? 0)),
                ("updated", false) => query.OrderBy(x => x.UpdatedAt), ("updated", true) => query.OrderByDescending(x => x.UpdatedAt),
                (_, false) => query.OrderBy(x => x.Name), _ => query.OrderByDescending(x => x.Name)
            };
            var total = await query.CountAsync(cancellationToken);
            var items = await ordered.ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.Name, x.Category, x.ModelNumber, x.ItemNumber, x.Supplier, x.Location,
                    x.MinimumQuantity, x.TotalQuantity,
                    checkedOut = db.AccessoryAssignments.Where(a => a.CompanyId == companyId && a.AccessoryId == x.Id).Sum(a => (int?)(a.Quantity - a.ReturnedQuantity)) ?? 0,
                    x.UnitCost, x.Currency, x.PurchaseDate, x.IsRequestable, x.UpdatedAt
                }).ToListAsync(cancellationToken);
            return Results.Ok(new { items, page, pageSize, total });
        }).RequireAuthorization("asset.read");

        api.MapGet("/accessories/export.csv", async ([FromQuery] string? q, [FromQuery] string? category,
            [FromQuery] string? location, [FromQuery] string? stock, [FromQuery] string? sortBy,
            [FromQuery] string? sortDirection, HttpContext context, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            if (q is not null && q.Trim().Length > 120)
            {
                await Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] }).ExecuteAsync(context);
                return;
            }
            if (location is not null && location.Trim().Length > 160)
            {
                await Results.ValidationProblem(new Dictionary<string, string[]> { ["location"] = ["Konum filtresi 160 karakteri aşamaz."] }).ExecuteAsync(context);
                return;
            }

            var query = db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.Name.Contains(term) || (x.Category != null && x.Category.Contains(term)) ||
                    (x.ModelNumber != null && x.ModelNumber.Contains(term)) || (x.ItemNumber != null && x.ItemNumber.Contains(term)) ||
                    (x.Supplier != null && x.Supplier.Contains(term)) || (x.Location != null && x.Location.Contains(term)) ||
                    db.AccessoryAssignments.Any(a => a.CompanyId == companyId && a.AccessoryId == x.Id &&
                        a.TargetType == "Location" && a.Quantity > a.ReturnedQuantity && a.Location != null && a.Location.Contains(term)));
            }
            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category == category.Trim());
            if (!string.IsNullOrWhiteSpace(location)) query = query.Where(x => x.Location == location.Trim());
            if (stock == "low") query = query.Where(x => x.TotalQuantity - (db.AccessoryAssignments
                .Where(a => a.CompanyId == companyId && a.AccessoryId == x.Id)
                .Sum(a => (int?)(a.Quantity - a.ReturnedQuantity)) ?? 0) <= x.MinimumQuantity);
            if (stock == "available") query = query.Where(x => x.TotalQuantity > (db.AccessoryAssignments
                .Where(a => a.CompanyId == companyId && a.AccessoryId == x.Id)
                .Sum(a => (int?)(a.Quantity - a.ReturnedQuantity)) ?? 0));

            var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            var sort = (sortBy ?? "name").Trim().ToLowerInvariant();
            if (sort is not ("name" or "category" or "location" or "total" or "remaining" or "updated") ||
                (sortDirection is not null && !new[] { "asc", "desc" }.Contains(sortDirection.Trim(), StringComparer.OrdinalIgnoreCase)))
            {
                await Results.ValidationProblem(new Dictionary<string, string[]> { ["sortBy"] = ["Sıralama alanı veya yönü desteklenmiyor."] }).ExecuteAsync(context);
                return;
            }
            IOrderedQueryable<Accessory> ordered = (sort, descending) switch
            {
                ("category", false) => query.OrderBy(x => x.Category), ("category", true) => query.OrderByDescending(x => x.Category),
                ("location", false) => query.OrderBy(x => x.Location), ("location", true) => query.OrderByDescending(x => x.Location),
                ("total", false) => query.OrderBy(x => x.TotalQuantity), ("total", true) => query.OrderByDescending(x => x.TotalQuantity),
                ("remaining", false) => query.OrderBy(x => x.TotalQuantity - (db.AccessoryAssignments.Where(a => a.CompanyId == companyId && a.AccessoryId == x.Id).Sum(a => (int?)(a.Quantity - a.ReturnedQuantity)) ?? 0)),
                ("remaining", true) => query.OrderByDescending(x => x.TotalQuantity - (db.AccessoryAssignments.Where(a => a.CompanyId == companyId && a.AccessoryId == x.Id).Sum(a => (int?)(a.Quantity - a.ReturnedQuantity)) ?? 0)),
                ("updated", false) => query.OrderBy(x => x.UpdatedAt), ("updated", true) => query.OrderByDescending(x => x.UpdatedAt),
                (_, false) => query.OrderBy(x => x.Name), _ => query.OrderByDescending(x => x.Name)
            };

            var total = await query.CountAsync(cancellationToken);
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/csv; charset=utf-8";
            context.Response.Headers["Content-Disposition"] = $"attachment; filename=envantertr-aksesuarlar-{DateTime.UtcNow:yyyyMMdd}.csv";
            context.Response.Headers["Cache-Control"] = "no-store";
            context.Response.Headers["X-Total-Count"] = total.ToString(CultureInfo.InvariantCulture);
            await using var writer = new StreamWriter(context.Response.Body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), 16 * 1024, leaveOpen: true);
            await writer.WriteLineAsync("Ad;Kategori;Model No;Ürün No;Tedarikçi;Konum;Toplam;Zimmetli;Kalan;Minimum;Birim Maliyeti;Para Birimi;Alış Tarihi;İstek Açık".AsMemory(), cancellationToken);
            var rows = ordered.ThenBy(x => x.Id).Select(x => new
            {
                x.Name, x.Category, x.ModelNumber, x.ItemNumber, x.Supplier, x.Location,
                x.TotalQuantity,
                CheckedOut = db.AccessoryAssignments.Where(a => a.CompanyId == companyId && a.AccessoryId == x.Id)
                    .Sum(a => (int?)(a.Quantity - a.ReturnedQuantity)) ?? 0,
                x.MinimumQuantity, x.UnitCost, x.Currency, x.PurchaseDate, x.IsRequestable
            });
            var written = 0;
            await foreach (var row in rows.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                var remaining = row.TotalQuantity - row.CheckedOut;
                var line = string.Join(';', new[] { CsvCell(row.Name), CsvCell(row.Category), CsvCell(row.ModelNumber),
                    CsvCell(row.ItemNumber), CsvCell(row.Supplier), CsvCell(row.Location),
                    CsvCell(row.TotalQuantity.ToString(CultureInfo.InvariantCulture)),
                    CsvCell(row.CheckedOut.ToString(CultureInfo.InvariantCulture)),
                    CsvCell(remaining.ToString(CultureInfo.InvariantCulture)),
                    CsvCell(row.MinimumQuantity.ToString(CultureInfo.InvariantCulture)),
                    CsvCell(row.UnitCost?.ToString("0.00", CultureInfo.InvariantCulture)), CsvCell(row.Currency),
                    CsvCell(row.PurchaseDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    CsvCell(row.IsRequestable ? "Evet" : "Hayır") });
                await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
                if (++written % 500 == 0) await writer.FlushAsync(cancellationToken);
            }
            await writer.FlushAsync(cancellationToken);
        }).RequireAuthorization("asset.read");

        api.MapGet("/accessories/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var item = await db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new
                {
                    x.Id, x.Name, x.Category, x.ModelNumber, x.ItemNumber, x.Supplier, x.Location,
                    x.MinimumQuantity, x.TotalQuantity,
                    checkedOut = db.AccessoryAssignments.Where(a => a.CompanyId == companyId && a.AccessoryId == x.Id).Sum(a => (int?)(a.Quantity - a.ReturnedQuantity)) ?? 0,
                    x.UnitCost, x.Currency, x.PurchaseDate, x.Notes, x.IsRequestable, x.CreatedAt, x.UpdatedAt
                }).FirstOrDefaultAsync(cancellationToken);
            if (item is null) return Results.NotFound();
            var assignments = await db.AccessoryAssignments.AsNoTracking().Where(x => x.CompanyId == companyId && x.AccessoryId == id && x.Quantity > x.ReturnedQuantity)
                .OrderByDescending(x => x.CheckedOutAt).Select(x => new
                {
                    x.Id, x.TargetType, x.EmployeeId, x.AssetId, x.Location,
                    targetName = x.TargetType == "Employee"
                        ? db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.DisplayName).FirstOrDefault()
                        : x.TargetType == "Asset"
                            ? db.Assets.Where(a => a.CompanyId == companyId && a.Id == x.AssetId).Select(a => a.AssetTag + " · " + a.Name).FirstOrDefault()
                            : x.Location,
                    x.Quantity, x.ReturnedQuantity, remainingQuantity = x.Quantity - x.ReturnedQuantity, x.Notes, x.CheckedOutAt
                }).ToListAsync(cancellationToken);
            var movements = await db.AccessoryMovements.AsNoTracking().Where(x => x.CompanyId == companyId && x.AccessoryId == id)
                .OrderByDescending(x => x.OccurredAt).Take(50).Select(x => new
                {
                    x.Id, x.MovementType, x.Quantity, x.Notes, x.OccurredAt,
                    targetName = db.AccessoryAssignments.Where(a => a.CompanyId == companyId && a.Id == x.AssignmentId)
                        .Select(a => a.TargetType == "Employee"
                            ? db.Employees.Where(e => e.CompanyId == companyId && e.Id == a.EmployeeId).Select(e => e.DisplayName).FirstOrDefault()
                            : a.TargetType == "Asset"
                                ? db.Assets.Where(asset => asset.CompanyId == companyId && asset.Id == a.AssetId).Select(asset => asset.AssetTag + " · " + asset.Name).FirstOrDefault()
                                : a.Location).FirstOrDefault()
                }).ToListAsync(cancellationToken);
            var orders = await (from line in db.AccessoryPurchaseOrderLines.AsNoTracking()
                                join order in db.AccessoryPurchaseOrders.AsNoTracking()
                                    on new { line.CompanyId, Id = line.PurchaseOrderId } equals new { order.CompanyId, order.Id }
                                where line.CompanyId == companyId && line.AccessoryId == id
                                orderby order.OrderDate descending, order.CreatedAt descending
                                select new
                                {
                                    line.PurchaseOrderId, order.OrderNumber, order.Supplier, order.Status, order.OrderDate, order.Currency,
                                    line.Quantity, line.QuantityReceived, remaining = line.Quantity - line.QuantityReceived, line.UnitCost
                                }).Take(25).ToListAsync(cancellationToken);
            return Results.Ok(new { item, assignments, movements, orders });
        }).RequireAuthorization("asset.read");

        api.MapPost("/accessories", async (CreateAccessoryRequest request, ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var errors = ValidateAccessory(request.Name, request.MinimumQuantity, request.Quantity,
                request.UnitCost, request.Currency, request.Category, request.ModelNumber, request.ItemNumber,
                request.Supplier, request.Location, request.Notes);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var location = Clean(request.Location) ?? await GetDefaultLocationPathAsync(db, companyId, cancellationToken);
            var item = new Accessory
            {
                CompanyId = companyId, Name = request.Name.Trim(), Category = Clean(request.Category),
                ModelNumber = Clean(request.ModelNumber), ItemNumber = Clean(request.ItemNumber),
                Supplier = Clean(request.Supplier), SupplierId = await SupplierCatalogEndpoints.ResolveSupplierIdAsync(db, companyId, request.Supplier, cancellationToken), Location = location,
                LocationCatalogId = await ResolveLocationCatalogIdAsync(db, companyId, location, cancellationToken),
                MinimumQuantity = request.MinimumQuantity, TotalQuantity = request.Quantity,
                UnitCost = request.UnitCost, Currency = request.Currency.Trim().ToUpperInvariant(),
                PurchaseDate = request.PurchaseDate, Notes = Clean(request.Notes), IsRequestable = request.IsRequestable
            };
            db.Accessories.Add(item);
            if (item.TotalQuantity > 0) db.AccessoryMovements.Add(new AccessoryMovement
            {
                CompanyId = companyId, AccessoryId = item.Id, ActorUserId = CurrentUserId(user),
                MovementType = "Received", Quantity = item.TotalQuantity, Notes = "İlk stok girişi"
            });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = CurrentUserId(user), TargetType = "Accessory", TargetId = item.Id, Action = "created", Details = $"Aksesuar kaydı oluşturuldu; ilk stok {item.TotalQuantity} adet." });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/accessories/{item.Id}", new { item.Id, item.Name });
        }).RequireAuthorization("asset.write");

        api.MapPut("/accessories/{id:guid}", async (Guid id, UpdateAccessoryRequest request, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var item = await db.Accessories.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (item is null) return Results.NotFound();
            var errors = ValidateAccessory(request.Name, request.MinimumQuantity, item.TotalQuantity,
                request.UnitCost, request.Currency, request.Category, request.ModelNumber, request.ItemNumber,
                request.Supplier, request.Location, request.Notes);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            item.Name = request.Name.Trim(); item.Category = Clean(request.Category); item.ModelNumber = Clean(request.ModelNumber);
            item.ItemNumber = Clean(request.ItemNumber); item.Supplier = Clean(request.Supplier); item.SupplierId = await SupplierCatalogEndpoints.ResolveSupplierIdAsync(db, companyId, request.Supplier, cancellationToken); item.Location = Clean(request.Location);
            item.LocationCatalogId = await ResolveLocationCatalogIdAsync(db, companyId, request.Location, cancellationToken);
            item.MinimumQuantity = request.MinimumQuantity; item.UnitCost = request.UnitCost;
            item.Currency = request.Currency.Trim().ToUpperInvariant(); item.PurchaseDate = request.PurchaseDate;
            item.Notes = Clean(request.Notes); item.IsRequestable = request.IsRequestable; item.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = CurrentUserId(user), TargetType = "Accessory", TargetId = item.Id, Action = "updated", Details = "Aksesuar temel bilgileri güncellendi." });
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("asset.write");

        api.MapPost("/accessories/{id:guid}/stock", async (Guid id, AccessoryStockRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            if (request.Quantity == 0 || Math.Abs((long)request.Quantity) > 100000 || request.Notes?.Length > 2000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = ["Stok hareketi 0 olamaz, 100.000 adedi aşamaz ve not 2.000 karakterden uzun olamaz."] });
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            var item = await db.Accessories.FromSqlInterpolated(
                    $"SELECT * FROM \"Accessories\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (item is null) return Results.NotFound();
            var outstanding = await db.AccessoryAssignments.Where(x => x.CompanyId == companyId && x.AccessoryId == id)
                .SumAsync(x => (int?)(x.Quantity - x.ReturnedQuantity), cancellationToken) ?? 0;
            if (item.TotalQuantity + request.Quantity > 100000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = ["Toplam stok 100.000 adedi aşamaz."] });
            if (item.TotalQuantity + request.Quantity < outstanding || item.TotalQuantity + request.Quantity < 0)
                return Results.Conflict(new { message = $"Stok {outstanding} zimmetli adet altına indirilemez." });
            item.TotalQuantity += request.Quantity; item.UpdatedAt = DateTimeOffset.UtcNow;
            db.AccessoryMovements.Add(new AccessoryMovement
            {
                CompanyId = companyId, AccessoryId = item.Id, ActorUserId = CurrentUserId(user),
                MovementType = request.Quantity > 0 ? "Received" : "Adjustment", Quantity = Math.Abs(request.Quantity), Notes = Clean(request.Notes)
            });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = CurrentUserId(user), TargetType = "Accessory", TargetId = id, Action = request.Quantity > 0 ? "stock_received" : "stock_adjusted", Details = $"Stok {request.Quantity:+#;-#;0} adet değiştirildi." });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { item.TotalQuantity, checkedOut = outstanding, remaining = item.TotalQuantity - outstanding });
        }).RequireAuthorization("asset.write");

        api.MapPost("/accessories/{id:guid}/checkout", async (Guid id, AccessoryCheckoutRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            if (request.Quantity is < 1 or > 100000 || request.Notes?.Length > 2000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = ["Adet 1 ile 100.000 arasında olmalı; not en fazla 2.000 karakter olabilir."] });
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
            var targetType = request.TargetType?.Trim().ToLowerInvariant();
            var targetErrors = new Dictionary<string, string[]>();
            var normalizedLocation = Clean(request.Location);
            var employeeId = request.EmployeeId;
            var assetId = request.AssetId;
            if (targetType == "employee")
            {
                if (employeeId is not Guid selectedEmployee || !await db.Employees.AsNoTracking().AnyAsync(x => x.CompanyId == companyId && x.Id == selectedEmployee && x.IsActive, cancellationToken))
                    targetErrors["employeeId"] = ["Etkin çalışan bu şirkette bulunamadı."];
                assetId = null; normalizedLocation = null;
            }
            else if (targetType == "asset")
            {
                if (assetId is not Guid selectedAsset || !await db.Assets.AsNoTracking().AnyAsync(x => x.CompanyId == companyId && x.Id == selectedAsset, cancellationToken))
                    targetErrors["assetId"] = ["Varlık bu şirkette bulunamadı."];
                employeeId = null; normalizedLocation = null;
            }
            else if (targetType == "location")
            {
                if (string.IsNullOrWhiteSpace(normalizedLocation) || normalizedLocation.Length > 160)
                    targetErrors["location"] = ["Konum zorunludur ve 160 karakteri aşamaz."];
                employeeId = null; assetId = null;
            }
            else targetErrors["targetType"] = ["Teslim hedefi çalışan, varlık veya konum olmalı."];
            if (targetErrors.Count > 0) return Results.ValidationProblem(targetErrors);
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            var item = await db.Accessories.FromSqlInterpolated(
                    $"SELECT * FROM \"Accessories\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (item is null) return Results.NotFound();
            var outstanding = await db.AccessoryAssignments.Where(x => x.CompanyId == companyId && x.AccessoryId == id)
                .SumAsync(x => (int?)(x.Quantity - x.ReturnedQuantity), cancellationToken) ?? 0;
            var available = item.TotalQuantity - outstanding;
            if (request.Quantity > available) return Results.Conflict(new { message = $"Yalnızca {available} adet stokta kullanılabilir." });
            var assignment = new AccessoryAssignment
            {
                CompanyId = companyId, AccessoryId = id, TargetType = targetType![..1].ToUpperInvariant() + targetType[1..],
                EmployeeId = employeeId, AssetId = assetId, Location = normalizedLocation,
                Quantity = request.Quantity, Notes = Clean(request.Notes)
            };
            db.AccessoryAssignments.Add(assignment);
            db.AccessoryMovements.Add(new AccessoryMovement
            {
                CompanyId = companyId, AccessoryId = id, AssignmentId = assignment.Id, EmployeeId = employeeId,
                ActorUserId = userId, MovementType = "Checkout", Quantity = request.Quantity, Notes = Clean(request.Notes)
            });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId, TargetType = "Accessory", TargetId = id, Action = "checked_out", Details = $"{request.Quantity} adet aksesuar çalışana zimmetlendi." });
            item.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Created($"/api/accessories/{id}", new { assignment.Id, assignment.Quantity, remaining = available - request.Quantity });
        }).RequireAuthorization("asset.write");

        api.MapPost("/accessories/bulk-checkout", async (BulkAccessoryCheckoutRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
            var items = request.Items ?? [];
            if (items.Length is < 1 or > 100 || items.Any(x => x.AccessoryId == Guid.Empty || x.Quantity is < 1 or > 100000) || items.Select(x => x.AccessoryId).Distinct().Count() != items.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["1 ile 100 arasında, tekrarsız aksesuar ve her biri için 1–100.000 arası adet girin."] });
            if (request.Notes?.Length > 2000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["notes"] = ["Not en fazla 2.000 karakter olabilir."] });

            var targetType = request.TargetType?.Trim().ToLowerInvariant();
            var employeeId = request.EmployeeId;
            var assetId = request.AssetId;
            var location = Clean(request.Location);
            var targetErrors = new Dictionary<string, string[]>();
            if (targetType == "employee")
            {
                if (employeeId is not Guid selectedEmployee || !await db.Employees.AsNoTracking().AnyAsync(x => x.CompanyId == companyId && x.Id == selectedEmployee && x.IsActive, cancellationToken))
                    targetErrors["employeeId"] = ["Etkin çalışan bu şirkette bulunamadı."];
                assetId = null; location = null;
            }
            else if (targetType == "asset")
            {
                if (assetId is not Guid selectedAsset || !await db.Assets.AsNoTracking().AnyAsync(x => x.CompanyId == companyId && x.Id == selectedAsset, cancellationToken))
                    targetErrors["assetId"] = ["Varlık bu şirkette bulunamadı."];
                employeeId = null; location = null;
            }
            else if (targetType == "location")
            {
                if (string.IsNullOrWhiteSpace(location) || location.Length > 160) targetErrors["location"] = ["Konum zorunludur ve 160 karakteri aşamaz."];
                employeeId = null; assetId = null;
            }
            else targetErrors["targetType"] = ["Teslim hedefi çalışan, varlık veya konum olmalı."];
            if (targetErrors.Count > 0) return Results.ValidationProblem(targetErrors);

            var ids = items.Select(x => x.AccessoryId).ToArray();
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            var accessories = await db.Accessories.FromSqlInterpolated(
                    $"SELECT * FROM \"Accessories\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = ANY ({ids}) ORDER BY \"Id\" FOR UPDATE")
                .ToListAsync(cancellationToken);
            if (accessories.Count != items.Length) return Results.NotFound(new { message = "Aksesuarlardan biri bu şirkette bulunamadı. Hiçbir teslim kaydedilmedi." });
            var checkedOutById = await db.AccessoryAssignments.Where(x => x.CompanyId == companyId && ids.Contains(x.AccessoryId))
                .GroupBy(x => x.AccessoryId).Select(group => new { AccessoryId = group.Key, Quantity = group.Sum(x => x.Quantity - x.ReturnedQuantity) })
                .ToDictionaryAsync(x => x.AccessoryId, x => x.Quantity, cancellationToken);
            var shortages = new List<string>();
            foreach (var accessory in accessories)
            {
                var item = items.Single(x => x.AccessoryId == accessory.Id);
                var available = accessory.TotalQuantity - checkedOutById.GetValueOrDefault(accessory.Id);
                if (item.Quantity > available) shortages.Add($"{accessory.Name}: {available} adet kullanılabilir.");
            }
            if (shortages.Count > 0) return Results.Conflict(new { message = "Bazı ürünlerde yeterli stok yok; hiçbir teslim kaydedilmedi.", items = shortages });

            var notes = Clean(request.Notes);
            var now = DateTimeOffset.UtcNow;
            foreach (var accessory in accessories)
            {
                var item = items.Single(x => x.AccessoryId == accessory.Id);
                var assignment = new AccessoryAssignment
                {
                    CompanyId = companyId, AccessoryId = accessory.Id,
                    TargetType = targetType![..1].ToUpperInvariant() + targetType[1..],
                    EmployeeId = employeeId, AssetId = assetId, Location = location,
                    Quantity = item.Quantity, Notes = notes
                };
                db.AccessoryAssignments.Add(assignment);
                db.AccessoryMovements.Add(new AccessoryMovement
                {
                    CompanyId = companyId, AccessoryId = accessory.Id, AssignmentId = assignment.Id,
                    EmployeeId = employeeId, ActorUserId = userId, MovementType = "Checkout", Quantity = item.Quantity, Notes = notes
                });
                db.AuditEntries.Add(new AuditEntry
                {
                    CompanyId = companyId, ActorUserId = userId, TargetType = "Accessory", TargetId = accessory.Id,
                    Action = "bulk_checked_out", Details = $"{item.Quantity} adet aksesuar toplu teslim edildi."
                });
                accessory.UpdatedAt = now;
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { checkedOut = accessories.Count, targetType, employeeId, assetId, location });
        }).RequireAuthorization("asset.write");

        api.MapPost("/accessories/bulk-stock-adjustment", async (BulkAccessoryStockAdjustmentRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
            var items = request.Items ?? [];
            if (items.Length is < 1 or > 100 || items.Any(x => x.AccessoryId == Guid.Empty || x.Quantity == 0 || Math.Abs((long)x.Quantity) > 100000) || items.Select(x => x.AccessoryId).Distinct().Count() != items.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["1 ile 100 arasında tekrarsız aksesuar seçin; değişiklik adedi sıfır olamaz ve ±100.000 aralığında olmalı."] });
            if (request.Notes?.Length > 1900)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["notes"] = ["Toplu düzeltme notu 1.900 karakteri aşamaz."] });

            var ids = items.Select(x => x.AccessoryId).ToArray();
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            var accessories = await db.Accessories.FromSqlInterpolated(
                    $"SELECT * FROM \"Accessories\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = ANY ({ids}) ORDER BY \"Id\" FOR UPDATE")
                .ToListAsync(cancellationToken);
            if (accessories.Count != items.Length) return Results.NotFound(new { message = "Aksesuarlardan biri bu şirkette bulunamadı. Hiçbir stok değiştirilmedi." });
            var outstandingById = await db.AccessoryAssignments.Where(x => x.CompanyId == companyId && ids.Contains(x.AccessoryId))
                .GroupBy(x => x.AccessoryId).Select(group => new { AccessoryId = group.Key, Quantity = group.Sum(x => x.Quantity - x.ReturnedQuantity) })
                .ToDictionaryAsync(x => x.AccessoryId, x => x.Quantity, cancellationToken);
            var invalid = new List<string>();
            foreach (var accessory in accessories)
            {
                var delta = items.Single(x => x.AccessoryId == accessory.Id).Quantity;
                var adjusted = accessory.TotalQuantity + delta;
                var outstanding = outstandingById.GetValueOrDefault(accessory.Id);
                if (adjusted is < 0 or > 100000 || adjusted < outstanding)
                    invalid.Add($"{accessory.Name}: yeni toplam {adjusted}; en az {outstanding} zimmetli adedi ve en fazla 100.000 toplam stoku karşılamalı.");
            }
            if (invalid.Count > 0) return Results.Conflict(new { message = "Stok sayımı seçilen ürünlerden en az birinde geçersiz; hiçbir stok değiştirilmedi.", items = invalid });

            var now = DateTimeOffset.UtcNow;
            var note = Clean(request.Notes);
            foreach (var accessory in accessories)
            {
                var delta = items.Single(x => x.AccessoryId == accessory.Id).Quantity;
                accessory.TotalQuantity += delta;
                accessory.UpdatedAt = now;
                var movementNotes = $"Stok sayım farkı {delta:+#;-#;0} adet";
                if (!string.IsNullOrWhiteSpace(note)) movementNotes += $" · {note}";
                db.AccessoryMovements.Add(new AccessoryMovement
                {
                    CompanyId = companyId, AccessoryId = accessory.Id, ActorUserId = userId,
                    MovementType = delta > 0 ? "Received" : "Adjustment", Quantity = Math.Abs(delta), Notes = movementNotes
                });
                db.AuditEntries.Add(new AuditEntry
                {
                    CompanyId = companyId, ActorUserId = userId, TargetType = "Accessory", TargetId = accessory.Id,
                    Action = delta > 0 ? "bulk_stock_received" : "bulk_stock_adjusted", Details = movementNotes
                });
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { adjusted = accessories.Count });
        }).RequireAuthorization("asset.write");

        api.MapPost("/accessories/{id:guid}/bulk-checkin", async (Guid id, BulkAccessoryCheckinRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
            var items = request.Items ?? [];
            if (id == Guid.Empty || items.Length is < 1 or > 100 || items.Any(x => x.AssignmentId == Guid.Empty || x.Quantity is < 1 or > 100000) || items.Select(x => x.AssignmentId).Distinct().Count() != items.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["1 ile 100 arasında, tekrarsız açık zimmet ve her biri için 1–100.000 arası iade adedi girin."] });
            if (request.Notes?.Length > 2000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["notes"] = ["Not en fazla 2.000 karakter olabilir."] });

            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            var accessory = await db.Accessories.FromSqlInterpolated(
                    $"SELECT * FROM \"Accessories\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (accessory is null) return Results.NotFound();
            var assignmentIds = items.Select(x => x.AssignmentId).ToArray();
            var assignments = await db.AccessoryAssignments.FromSqlInterpolated(
                    $"SELECT * FROM \"AccessoryAssignments\" WHERE \"CompanyId\" = {companyId} AND \"AccessoryId\" = {id} AND \"Id\" = ANY ({assignmentIds}) ORDER BY \"Id\" FOR UPDATE")
                .ToListAsync(cancellationToken);
            if (assignments.Count != items.Length) return Results.NotFound(new { message = "Açık zimmetlerden biri bulunamadı. Hiçbir iade kaydedilmedi." });
            var quantitiesById = items.ToDictionary(x => x.AssignmentId, x => x.Quantity);
            var excessive = assignments.Where(x => quantitiesById[x.Id] > x.Quantity - x.ReturnedQuantity).ToArray();
            if (excessive.Length > 0)
                return Results.Conflict(new { message = "Seçilen zimmetlerden en az birinde istenen miktar kalan adedi aşıyor. Hiçbir iade kaydedilmedi.", assignments = excessive.Select(x => new { x.Id, remaining = x.Quantity - x.ReturnedQuantity }) });

            var now = DateTimeOffset.UtcNow;
            var notes = Clean(request.Notes);
            foreach (var assignment in assignments)
            {
                var quantity = quantitiesById[assignment.Id];
                assignment.ReturnedQuantity += quantity;
                assignment.LastReturnedAt = now;
                db.AccessoryMovements.Add(new AccessoryMovement
                {
                    CompanyId = companyId, AccessoryId = id, AssignmentId = assignment.Id, EmployeeId = assignment.EmployeeId,
                    ActorUserId = userId, MovementType = "Checkin", Quantity = quantity, Notes = notes
                });
                db.AuditEntries.Add(new AuditEntry
                {
                    CompanyId = companyId, ActorUserId = userId, TargetType = "Accessory", TargetId = id,
                    Action = "bulk_checked_in", Details = $"{quantity} adet aksesuar toplu iade alındı."
                });
            }
            accessory.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { returnedAssignments = assignments.Count, returnedQuantity = items.Sum(x => x.Quantity) });
        }).RequireAuthorization("asset.write");

        api.MapPost("/accessories/{id:guid}/checkin", async (Guid id, AccessoryCheckinRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            if (request.Quantity is < 1 or > 100000 || request.Notes?.Length > 2000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = ["Adet 1 ile 100.000 arasında olmalı; not en fazla 2.000 karakter olabilir."] });
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            var item = await db.Accessories.FromSqlInterpolated(
                    $"SELECT * FROM \"Accessories\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (item is null) return Results.NotFound();
            var assignment = await db.AccessoryAssignments.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.AccessoryId == id && x.Id == request.AssignmentId, cancellationToken);
            if (assignment is null) return Results.NotFound();
            var outstanding = assignment.Quantity - assignment.ReturnedQuantity;
            if (request.Quantity > outstanding) return Results.Conflict(new { message = $"Bu zimmette yalnızca {outstanding} adet iade alınabilir." });
            assignment.ReturnedQuantity += request.Quantity; assignment.LastReturnedAt = DateTimeOffset.UtcNow;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            db.AccessoryMovements.Add(new AccessoryMovement
            {
                CompanyId = companyId, AccessoryId = id, AssignmentId = assignment.Id, EmployeeId = assignment.EmployeeId,
                ActorUserId = userId, MovementType = "Checkin", Quantity = request.Quantity, Notes = Clean(request.Notes)
            });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId, TargetType = "Accessory", TargetId = id, Action = "checked_in", Details = $"{request.Quantity} adet aksesuar iade alındı." });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { assignment.Id, assignment.Quantity, assignment.ReturnedQuantity, remainingOnAssignment = assignment.Quantity - assignment.ReturnedQuantity });
        }).RequireAuthorization("asset.write");

        // Free-text location values stay backward compatible while a company-scoped
        // building/floor/room catalog supplies managed choices to forms.
        api.MapGet("/locations", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var locations = new Dictionary<string, (string Name, int Assets, int Assigned, int Available, int Maintenance, int Accessories, int AccessoryAssignments, int Consumables, int Components)>(StringComparer.OrdinalIgnoreCase);
            void AddLocation(string name, int assets = 0, int assigned = 0, int available = 0, int maintenance = 0,
                int accessories = 0, int accessoryAssignments = 0, int consumables = 0, int components = 0)
            {
                if (string.IsNullOrWhiteSpace(name)) return;
                var key = name.Trim();
                locations.TryGetValue(key, out var current);
                locations[key] = (current.Name ?? key, current.Assets + assets, current.Assigned + assigned,
                    current.Available + available, current.Maintenance + maintenance,
                    current.Accessories + accessories, current.AccessoryAssignments + accessoryAssignments,
                    current.Consumables + consumables,
                    current.Components + components);
            }

            var assetLocations = await db.Assets.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.Location != null && x.Location.Trim() != "")
                .GroupBy(x => x.Location!.Trim())
                .Select(group => new
                {
                    name = group.Key,
                    assets = group.Count(),
                    assigned = group.Count(asset => asset.AssignedEmployeeId != null),
                    available = group.Count(asset => asset.Status == AssetStatuses.Available),
                    maintenance = group.Count(asset => asset.Status == AssetStatuses.Maintenance)
                })
                .ToListAsync(cancellationToken);
            foreach (var row in assetLocations) AddLocation(row.name, assets: row.assets, assigned: row.assigned,
                available: row.available, maintenance: row.maintenance);
            var accessoryLocations = await db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId && x.Location != null && x.Location.Trim() != "")
                .GroupBy(x => x.Location!.Trim()).Select(group => new { name = group.Key, count = group.Count() }).ToListAsync(cancellationToken);
            foreach (var row in accessoryLocations) AddLocation(row.name, accessories: row.count);
            var accessoryAssignmentLocations = await db.AccessoryAssignments.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.TargetType == "Location" && x.Location != null &&
                    x.Location.Trim() != "" && x.Quantity > x.ReturnedQuantity)
                .GroupBy(x => x.Location!.Trim())
                .Select(group => new { name = group.Key, count = group.Count() }).ToListAsync(cancellationToken);
            foreach (var row in accessoryAssignmentLocations) AddLocation(row.name, accessoryAssignments: row.count);
            var consumableLocations = await db.Consumables.AsNoTracking().Where(x => x.CompanyId == companyId && x.Location != null && x.Location.Trim() != "")
                .GroupBy(x => x.Location!.Trim()).Select(group => new { name = group.Key, count = group.Count() }).ToListAsync(cancellationToken);
            foreach (var row in consumableLocations) AddLocation(row.name, consumables: row.count);
            var componentLocations = await db.InventoryComponents.AsNoTracking().Where(x => x.CompanyId == companyId && x.Location != null && x.Location.Trim() != "")
                .GroupBy(x => x.Location!.Trim()).Select(group => new { name = group.Key, count = group.Count() }).ToListAsync(cancellationToken);
            foreach (var row in componentLocations) AddLocation(row.name, components: row.count);
            var items = locations.Values.Select(x => new LocationDirectoryEntry(x.Name, x.Assets, x.Assigned,
                x.Available, x.Maintenance, x.Accessories, x.AccessoryAssignments, x.Consumables, x.Components,
                null, null, 0, false, true, false, null, null)).ToList();
            var catalog = await db.CompanyLocations.AsNoTracking().Where(x => x.CompanyId == companyId)
                .OrderBy(x => x.Name).ToListAsync(cancellationToken);
            var byId = catalog.ToDictionary(x => x.Id);
            var managerIds = catalog.Where(x => x.ManagerEmployeeId != null).Select(x => x.ManagerEmployeeId!.Value).Distinct().ToArray();
            var managers = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && managerIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName, cancellationToken);
            (string Path, int Depth) GetPath(CompanyLocation location)
            {
                if (location.ParentLocationId is not Guid parentId || !byId.TryGetValue(parentId, out var parent))
                    return (location.Name, 0);
                var parentPath = GetPath(parent);
                return ($"{parentPath.Path} / {location.Name}", parentPath.Depth + 1);
            }
            foreach (var location in catalog)
            {
                var (path, depth) = GetPath(location);
                var index = items.FindIndex(x => string.Equals(x.Name, path, StringComparison.OrdinalIgnoreCase));
                var entry = index >= 0 ? items[index] : new LocationDirectoryEntry(path, 0, 0, 0, 0, 0, 0, 0, 0,
                    null, null, 0, false, true, false, null, null);
                var selectedIndex = index;
                var catalogEntry = entry with { LocationId = location.Id, ParentLocationId = location.ParentLocationId,
                    Depth = depth, IsCatalogEntry = true, IsActive = location.IsActive, IsDefault = location.IsDefault,
                    ManagerEmployeeId = location.ManagerEmployeeId,
                    ManagerName = location.ManagerEmployeeId is Guid managerId && managers.TryGetValue(managerId, out var managerName) ? managerName : null };
                if (selectedIndex >= 0) items[selectedIndex] = catalogEntry; else items.Add(catalogEntry);
            }
            items = items.OrderBy(x => x.Name, StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), true)).ToList();
            return Results.Ok(new { items, total = items.Count });
        }).RequireAuthorization("asset.read");

        api.MapPost("/locations/catalog", async ([FromBody] LocationCatalogRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 120 || name.Any(char.IsControl) || name.Contains('/') || name.Contains('\\'))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Konum adı 1–120 karakter olmalı; eğik çizgi ve kontrol karakteri içeremez."] });
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            if (await db.Companies.FromSqlInterpolated($"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken) is null) return Results.Forbid();
            CompanyLocation? parent = null;
            var depth = 0;
            var path = name;
            if (request.ParentLocationId is Guid parentId)
            {
                parent = await db.CompanyLocations.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == parentId, cancellationToken);
                if (parent is null || !parent.IsActive) return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentLocationId"] = ["Etkin üst konum bu şirkette bulunamadı."] });
                var chain = await GetLocationChainAsync(db, companyId, parent, cancellationToken);
                depth = chain.Count;
                if (depth >= 3) return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentLocationId"] = ["Hiyerarşi en fazla bina, kat ve oda olmak üzere üç seviyedir."] });
                path = string.Join(" / ", chain.Select(x => x.Name).Append(name));
            }
            if (path.Length > 160) return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Tam konum yolu 160 karakteri aşamaz; adları kısaltın."] });
            var normalized = NormalizeLocationName(name);
            var siblingExists = await db.CompanyLocations.AnyAsync(x => x.CompanyId == companyId && x.ParentLocationId == request.ParentLocationId &&
                x.NormalizedName == normalized, cancellationToken);
            if (siblingExists) return Results.Conflict(new { message = "Aynı üst konum altında bu ad zaten kayıtlı." });
            var location = new CompanyLocation { CompanyId = companyId, ParentLocationId = parent?.Id,
                Name = name, NormalizedName = normalized };
            db.CompanyLocations.Add(location);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Location", TargetId = location.Id, Action = "CatalogCreated",
                Details = $"Konum kataloğa eklendi: {path}." });
            try { await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "Bu hiyerarşide aynı adlı konum zaten kaydedilmiş." });
            }
            return Results.Created("/api/locations/catalog", new { location.Id, location.ParentLocationId,
                location.Name, displayPath = path, depth, location.IsActive });
        }).RequireAuthorization("asset.write");

        api.MapPost("/locations/catalog/{id:guid}/default", async (Guid id, [FromBody] SetLocationDefaultRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            if (await db.Companies.FromSqlInterpolated($"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken) is null) return Results.Forbid();
            var location = await db.CompanyLocations.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (location is null) return Results.NotFound();
            if (request.IsDefault && !location.IsActive)
                return Results.Conflict(new { message = "Arşivlenmiş konum varsayılan yapılamaz; önce konumu geri alın." });
            if (request.IsDefault)
            {
                var paths = await GetActiveLocationPathMapAsync(db, companyId, cancellationToken);
                var path = paths.FirstOrDefault(x => x.Value == id).Key;
                if (string.IsNullOrWhiteSpace(path) || path.Length > 160)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["location"] = ["Varsayılan konumun tam yolu 160 karakteri aşamaz."] });
            }
            if (request.IsDefault)
                await db.CompanyLocations.Where(x => x.CompanyId == companyId && x.IsDefault && x.Id != id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsDefault, false)
                        .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
            if (location.IsDefault != request.IsDefault)
            {
                location.IsDefault = request.IsDefault; location.UpdatedAt = DateTimeOffset.UtcNow;
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                    TargetType = "Location", TargetId = id, Action = request.IsDefault ? "CatalogDefaultSet" : "CatalogDefaultCleared",
                    Details = request.IsDefault ? $"Varsayılan konum: {location.Name}." : $"Varsayılan konum kaldırıldı: {location.Name}." });
                await db.SaveChangesAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { location.Id, location.IsDefault });
        }).RequireAuthorization("asset.write");

        api.MapPost("/locations/catalog/{id:guid}/manager", async (Guid id, [FromBody] SetLocationManagerRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            if (await db.Companies.FromSqlInterpolated($"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken) is null) return Results.Forbid();
            var location = await db.CompanyLocations.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (location is null) return Results.NotFound();
            if (!location.IsActive) return Results.Conflict(new { message = "Arşivlenmiş konuma sorumlu atanamaz." });
            Employee? manager = null;
            if (request.EmployeeId is Guid employeeId)
            {
                manager = await db.Employees.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == employeeId && x.IsActive, cancellationToken);
                if (manager is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeId"] = ["Sorumlu, bu şirkette kayıtlı etkin bir çalışan olmalıdır."] });
            }
            if (location.ManagerEmployeeId == request.EmployeeId)
            {
                await transaction.CommitAsync(cancellationToken);
                return Results.Ok(new { location.Id, location.ManagerEmployeeId, managerName = manager?.DisplayName });
            }
            var previousManager = location.ManagerEmployeeId is Guid previousId
                ? await db.Employees.Where(x => x.CompanyId == companyId && x.Id == previousId).Select(x => x.DisplayName).SingleOrDefaultAsync(cancellationToken)
                : null;
            location.ManagerEmployeeId = request.EmployeeId; location.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Location", TargetId = id, Action = "CatalogManagerChanged",
                Details = $"Konum sorumlusu: {previousManager ?? "atanmamış"} → {manager?.DisplayName ?? "atanmamış"}." });
            await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { location.Id, location.ManagerEmployeeId, managerName = manager?.DisplayName });
        }).RequireAuthorization("asset.write");

        api.MapPatch("/locations/catalog/{id:guid}", async (Guid id, [FromBody] LocationCatalogRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 120 || name.Any(char.IsControl) || name.Contains('/') || name.Contains('\\'))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Konum adı 1–120 karakter olmalı; eğik çizgi ve kontrol karakteri içeremez."] });
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            if (await db.Companies.FromSqlInterpolated($"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken) is null) return Results.Forbid();
            var location = await db.CompanyLocations.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (location is null) return Results.NotFound();
            var normalized = NormalizeLocationName(name);
            if (await db.CompanyLocations.AnyAsync(x => x.CompanyId == companyId && x.ParentLocationId == location.ParentLocationId &&
                x.NormalizedName == normalized && x.Id != id, cancellationToken))
                return Results.Conflict(new { message = "Aynı üst konum altında bu ad zaten kayıtlı." });
            var allLocations = await db.CompanyLocations.AsNoTracking().Where(x => x.CompanyId == companyId).ToListAsync(cancellationToken);
            var parentChain = await GetLocationChainAsync(db, companyId, location, cancellationToken);
            var pathPrefix = string.Join(" / ", parentChain.Take(parentChain.Count - 1).Select(x => x.Name));
            bool DescendantPathsFit(Guid currentId, string prefix)
            {
                var currentName = currentId == id ? name : allLocations.Single(x => x.Id == currentId).Name;
                var path = string.IsNullOrEmpty(prefix) ? currentName : $"{prefix} / {currentName}";
                if (path.Length > 160) return false;
                return allLocations.Where(x => x.ParentLocationId == currentId)
                    .All(child => DescendantPathsFit(child.Id, path));
            }
            if (!DescendantPathsFit(id, pathPrefix))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Bu ad, alt konumlarla birlikte 160 karakterlik konum yolu sınırını aşar."] });
            var oldName = location.Name;
            location.Name = name; location.NormalizedName = normalized; location.UpdatedAt = DateTimeOffset.UtcNow;
            var pathsById = allLocations.ToDictionary(x => x.Id);
            pathsById[id] = location;
            (string Path, int Depth) ResolvePath(Guid currentId)
            {
                var current = pathsById[currentId];
                if (current.ParentLocationId is not Guid parentId || !pathsById.TryGetValue(parentId, out var parent))
                    return (current.Name, 0);
                var parentPath = ResolvePath(parentId);
                return ($"{parentPath.Path} / {current.Name}", parentPath.Depth + 1);
            }
            foreach (var catalogLocation in allLocations)
            {
                var path = ResolvePath(catalogLocation.Id).Path;
                await db.Assets.Where(x => x.CompanyId == companyId && x.LocationCatalogId == catalogLocation.Id && x.Location != path)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Location, path)
                        .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
                await db.Employees.Where(x => x.CompanyId == companyId && x.LocationCatalogId == catalogLocation.Id && x.Location != path)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Location, path), cancellationToken);
                await db.Accessories.Where(x => x.CompanyId == companyId && x.LocationCatalogId == catalogLocation.Id && x.Location != path)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Location, path)
                        .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
                await db.Consumables.Where(x => x.CompanyId == companyId && x.LocationCatalogId == catalogLocation.Id && x.Location != path)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Location, path)
                        .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
                await db.InventoryComponents.Where(x => x.CompanyId == companyId && x.LocationCatalogId == catalogLocation.Id && x.Location != path)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Location, path)
                        .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
            }
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Location", TargetId = id, Action = "CatalogRenamed", Details = $"Konum adı “{oldName}” → “{name}”." });
            await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { location.Id, location.Name, location.ParentLocationId, location.IsActive });
        }).RequireAuthorization("asset.write");

        api.MapPatch("/locations/rename", async ([FromBody] RenameDirectoryEntryRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var currentName = request.CurrentName?.Trim();
            var newName = request.NewName?.Trim();
            if (string.IsNullOrWhiteSpace(currentName) || currentName.Length > 160 || currentName.Any(char.IsControl) ||
                string.IsNullOrWhiteSpace(newName) || newName.Length > 160 || newName.Any(char.IsControl))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Mevcut ve yeni konum adları 1–160 karakter olmalı ve kontrol karakteri içermemelidir."] });
            var currentPattern = currentName.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
            var newPattern = newName.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            if (await db.Companies.FromSqlInterpolated($"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken) is null) return Results.Forbid();

            var assets = db.Assets.Where(x => x.CompanyId == companyId && x.Location != null && EF.Functions.ILike(x.Location.Trim(), currentPattern, "\\"));
            if (await assets.AnyAsync(x => x.LocationCatalogId != null, cancellationToken))
                return Results.Conflict(new { message = "Bu ad katalog konumuna bağlı varlıklarda kullanılıyor. Katalog düğümünü düzenleyerek kimlik bağlantısını koruyun." });
            var employees = db.Employees.Where(x => x.CompanyId == companyId && x.Location != null && EF.Functions.ILike(x.Location.Trim(), currentPattern, "\\"));
            if (await employees.AnyAsync(x => x.LocationCatalogId != null, cancellationToken))
                return Results.Conflict(new { message = "Bu ad katalog konumuna bağlı çalışanlarda kullanılıyor. Katalog düğümünü düzenleyerek kimlik bağlantısını koruyun." });
            var accessories = db.Accessories.Where(x => x.CompanyId == companyId && x.Location != null && EF.Functions.ILike(x.Location.Trim(), currentPattern, "\\"));
            if (await accessories.AnyAsync(x => x.LocationCatalogId != null, cancellationToken))
                return Results.Conflict(new { message = "Bu ad katalog konumuna bağlı stok kayıtlarında kullanılıyor. Katalog düğümünü düzenleyerek kimlik bağlantısını koruyun." });
            var consumables = db.Consumables.Where(x => x.CompanyId == companyId && x.Location != null && EF.Functions.ILike(x.Location.Trim(), currentPattern, "\\"));
            if (await consumables.AnyAsync(x => x.LocationCatalogId != null, cancellationToken))
                return Results.Conflict(new { message = "Bu ad katalog konumuna bağlı stok kayıtlarında kullanılıyor. Katalog düğümünü düzenleyerek kimlik bağlantısını koruyun." });
            var components = db.InventoryComponents.Where(x => x.CompanyId == companyId && x.Location != null && EF.Functions.ILike(x.Location.Trim(), currentPattern, "\\"));
            if (await components.AnyAsync(x => x.LocationCatalogId != null, cancellationToken))
                return Results.Conflict(new { message = "Bu ad katalog konumuna bağlı stok kayıtlarında kullanılıyor. Katalog düğümünü düzenleyerek kimlik bağlantısını koruyun." });
            var accessoryAssignments = db.AccessoryAssignments.Where(x => x.CompanyId == companyId && x.TargetType == "Location" &&
                x.Location != null && EF.Functions.ILike(x.Location.Trim(), currentPattern, "\\") && x.Quantity > x.ReturnedQuantity);
            var assetCount = await assets.CountAsync(cancellationToken);
            var accessoryCount = await accessories.CountAsync(cancellationToken);
            var consumableCount = await consumables.CountAsync(cancellationToken);
            var componentCount = await components.CountAsync(cancellationToken);
            var accessoryAssignmentCount = await accessoryAssignments.CountAsync(cancellationToken);
            if (assetCount + accessoryCount + consumableCount + componentCount + accessoryAssignmentCount == 0) return Results.NotFound();
            if (string.Equals(currentName, newName, StringComparison.Ordinal))
            {
                await transaction.CommitAsync(cancellationToken);
                return Results.Ok(new { name = currentName, updatedCount = assetCount + accessoryCount + consumableCount + componentCount + accessoryAssignmentCount,
                    assetCount, accessoryCount, accessoryAssignmentCount, consumableCount, componentCount });
            }

            var targetExists = await db.Assets.Where(x => x.CompanyId == companyId && x.Location != null).AnyAsync(x => EF.Functions.ILike(x.Location!.Trim(), newPattern, "\\") && !EF.Functions.ILike(x.Location.Trim(), currentPattern, "\\"), cancellationToken) ||
                await db.Accessories.Where(x => x.CompanyId == companyId && x.Location != null).AnyAsync(x => EF.Functions.ILike(x.Location!.Trim(), newPattern, "\\") && !EF.Functions.ILike(x.Location.Trim(), currentPattern, "\\"), cancellationToken) ||
                await db.Consumables.Where(x => x.CompanyId == companyId && x.Location != null).AnyAsync(x => EF.Functions.ILike(x.Location!.Trim(), newPattern, "\\") && !EF.Functions.ILike(x.Location.Trim(), currentPattern, "\\"), cancellationToken) ||
                await db.InventoryComponents.Where(x => x.CompanyId == companyId && x.Location != null).AnyAsync(x => EF.Functions.ILike(x.Location!.Trim(), newPattern, "\\") && !EF.Functions.ILike(x.Location.Trim(), currentPattern, "\\"), cancellationToken) ||
                await db.AccessoryAssignments.Where(x => x.CompanyId == companyId && x.TargetType == "Location" && x.Location != null && x.Quantity > x.ReturnedQuantity)
                    .AnyAsync(x => EF.Functions.ILike(x.Location!.Trim(), newPattern, "\\") && !EF.Functions.ILike(x.Location.Trim(), currentPattern, "\\"), cancellationToken);
            if (targetExists && !request.Merge)
                return Results.Conflict(new { message = "Bu adla başka bir konum var. Birleştirme onayı gereklidir." });

            var updatedAt = DateTimeOffset.UtcNow;
            var updatedAssets = await assets.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Location, newName).SetProperty(x => x.UpdatedAt, updatedAt), cancellationToken);
            var updatedAccessories = await accessories.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Location, newName).SetProperty(x => x.UpdatedAt, updatedAt), cancellationToken);
            var updatedConsumables = await consumables.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Location, newName).SetProperty(x => x.UpdatedAt, updatedAt), cancellationToken);
            var updatedComponents = await components.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Location, newName).SetProperty(x => x.UpdatedAt, updatedAt), cancellationToken);
            var updatedAccessoryAssignments = await accessoryAssignments.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Location, newName), cancellationToken);
            var total = updatedAssets + updatedAccessories + updatedConsumables + updatedComponents + updatedAccessoryAssignments;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Location", TargetId = companyId, Action = "Renamed",
                Details = $"Konum “{currentName}” → “{newName}”{(targetExists ? " (mevcut konumla birleştirildi)" : "")}; {updatedAssets} varlık, {updatedAccessories} aksesuar, {updatedAccessoryAssignments} aktif aksesuar teslimi, {updatedConsumables} sarf malzemesi ve {updatedComponents} bileşen güncellendi. Kapanmış teslim geçmişi korundu." });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { name = newName, updatedCount = total, assetCount = updatedAssets,
                accessoryCount = updatedAccessories, accessoryAssignmentCount = updatedAccessoryAssignments,
                consumableCount = updatedConsumables, componentCount = updatedComponents });
        }).RequireAuthorization("asset.write");

        api.MapPost("/locations/catalog/{id:guid}/archive", async (Guid id, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            if (await db.Companies.FromSqlInterpolated($"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken) is null) return Results.Forbid();
            var location = await db.CompanyLocations.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (location is null) return Results.NotFound();
            if (await db.CompanyLocations.AnyAsync(x => x.CompanyId == companyId && x.ParentLocationId == id && x.IsActive, cancellationToken))
                return Results.Conflict(new { message = "Önce bu konumun altındaki etkin konumları arşivleyin." });
            if (!location.IsActive) return Results.Ok(new { location.Id, location.IsActive });
            location.IsActive = false; location.IsDefault = false; location.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Location", TargetId = id, Action = "CatalogArchived", Details = $"Konum arşivlendi: {location.Name}." });
            await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { location.Id, location.IsActive });
        }).RequireAuthorization("asset.write");

        api.MapPost("/locations/catalog/{id:guid}/restore", async (Guid id, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            if (await db.Companies.FromSqlInterpolated($"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken) is null) return Results.Forbid();
            var location = await db.CompanyLocations.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (location is null) return Results.NotFound();
            if (location.ParentLocationId is Guid parentId && !await db.CompanyLocations.AnyAsync(x =>
                x.CompanyId == companyId && x.Id == parentId && x.IsActive, cancellationToken))
                return Results.Conflict(new { message = "Önce üst konumu geri alın." });
            if (location.IsActive) return Results.Ok(new { location.Id, location.IsActive });
            location.IsActive = true; location.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Location", TargetId = id, Action = "CatalogRestored", Details = $"Konum geri alındı: {location.Name}." });
            await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { location.Id, location.IsActive });
        }).RequireAuthorization("asset.write");

        api.MapGet("/reports/assets", async ([FromQuery] DateOnly? purchaseDateFrom, [FromQuery] DateOnly? purchaseDateTo,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            if (purchaseDateFrom is DateOnly from && purchaseDateTo is DateOnly to && from > to)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["purchaseDate"] = ["Başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            var companyAssets = db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (purchaseDateFrom is DateOnly startDate) companyAssets = companyAssets.Where(x => x.PurchaseDate >= startDate);
            if (purchaseDateTo is DateOnly endDate) companyAssets = companyAssets.Where(x => x.PurchaseDate <= endDate);
            var totalAssets = await companyAssets.CountAsync(cancellationToken);
            var statusCounts = await companyAssets.GroupBy(x => x.Status)
                .Select(group => new { status = group.Key, count = group.Count() })
                .OrderBy(row => row.status).ToListAsync(cancellationToken);
            var categoryRows = await (
                from asset in companyAssets
                join category in db.AssetCategories.AsNoTracking().Where(x => x.CompanyId == companyId)
                    on new { asset.CompanyId, asset.CategoryId } equals new { category.CompanyId, CategoryId = category.Id }
                group asset by new { category.Id, category.Name } into groupedAssets
                orderby groupedAssets.Key.Name
                select new
                {
                    categoryId = groupedAssets.Key.Id,
                    name = groupedAssets.Key.Name,
                    assetCount = groupedAssets.Count(),
                    assignedCount = groupedAssets.Count(asset => asset.AssignedEmployeeId != null),
                    availableCount = groupedAssets.Count(asset => asset.Status == AssetStatuses.Available),
                    maintenanceCount = groupedAssets.Count(asset => asset.Status == AssetStatuses.Maintenance)
                })
                .ToListAsync(cancellationToken);
            var costRows = await companyAssets.Where(x => x.PurchaseCost != null)
                .GroupBy(x => x.Currency)
                .Select(group => new
                {
                    currency = group.Key,
                    recordedAssetCount = group.Count(),
                    recordedPurchaseCost = group.Sum(asset => asset.PurchaseCost!.Value)
                })
                .OrderBy(row => row.currency).ToListAsync(cancellationToken);
            var pricedAssetCount = costRows.Sum(row => row.recordedAssetCount);
            var costRowsForResponse = costRows.Select(row => new
            {
                row.currency,
                row.recordedAssetCount,
                row.recordedPurchaseCost,
                recordedPurchaseCostExact = row.recordedPurchaseCost.ToString(CultureInfo.InvariantCulture)
            }).ToList();
            return Results.Ok(new
            {
                generatedAt = DateTimeOffset.UtcNow,
                purchaseDateFrom,
                purchaseDateTo,
                totalAssets,
                pricedAssetCount,
                withoutPurchaseCostCount = totalAssets - pricedAssetCount,
                statusCounts,
                categories = categoryRows,
                purchaseCostByCurrency = costRowsForResponse
            });
        }).RequireAuthorization("asset.read");

        api.MapGet("/reports/maintenances", async ([FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            if (from is DateOnly start && to is DateOnly end && start > end)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["dateRange"] = ["Başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            var query = db.AssetMaintenances.AsNoTracking().Where(x => x.CompanyId == companyId && (x.Status == "InProgress" || x.Status == "Completed"));
            if (from is DateOnly fromDate) query = query.Where(x => x.StartedOn >= fromDate);
            if (to is DateOnly toDate) query = query.Where(x => x.StartedOn <= toDate);

            var total = await query.CountAsync(cancellationToken);
            var active = await query.CountAsync(x => x.Status == "InProgress", cancellationToken);
            var completed = await query.CountAsync(x => x.Status == "Completed", cancellationToken);
            var byType = await query.GroupBy(x => x.MaintenanceType)
                .Select(group => new { name = group.Key, count = group.Count() })
                .OrderByDescending(row => row.count).ThenBy(row => row.name)
                .Take(25).ToListAsync(cancellationToken);
            var costRows = await query.Where(x => x.Cost != null).GroupBy(x => x.Currency)
                .Select(group => new { currency = group.Key, recordCount = group.Count(), totalCost = group.Sum(x => x.Cost!.Value) })
                .OrderBy(row => row.currency).ToListAsync(cancellationToken);
            return Results.Ok(new
            {
                generatedAt = DateTimeOffset.UtcNow,
                dateFrom = from,
                dateTo = to,
                total,
                inProgress = active,
                completed,
                byType,
                costByCurrency = costRows.Select(row => new
                {
                    row.currency,
                    row.recordCount,
                    row.totalCost,
                    totalCostExact = row.totalCost.ToString(CultureInfo.InvariantCulture)
                })
            });
        }).RequireAuthorization("asset.read");

        api.MapGet("/assets/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var asset = await db.Assets.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new
                {
                    x.Id, x.AssetTag, x.Name, x.Status, x.SerialNumber, x.Location,
                    x.LocationCatalogId,
                    x.CategoryId, x.BrandId, x.ModelId, x.AssignedEmployeeId,
                    x.PurchaseDate, x.WarrantyExpiresAt, x.NextAuditOn, x.PurchaseCost, x.Currency, x.CreatedAt, x.UpdatedAt
                }).SingleOrDefaultAsync(cancellationToken);
            if (asset is null) return Results.NotFound();
            var references = new
            {
                category = await db.AssetCategories.Where(x => x.CompanyId == companyId && x.Id == asset.CategoryId).Select(x => x.Name).SingleOrDefaultAsync(cancellationToken),
                brand = await db.AssetBrands.Where(x => x.CompanyId == companyId && x.Id == asset.BrandId).Select(x => x.Name).SingleOrDefaultAsync(cancellationToken),
                model = await db.AssetModels.Where(x => x.CompanyId == companyId && x.Id == asset.ModelId).Select(x => x.Name).SingleOrDefaultAsync(cancellationToken),
                employee = await db.Employees.Where(x => x.CompanyId == companyId && x.Id == asset.AssignedEmployeeId).Select(x => x.DisplayName).SingleOrDefaultAsync(cancellationToken)
            };
            var history = await db.AssetEvents.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.AssetId == id)
                .OrderByDescending(x => x.OccurredAt)
                .Select(x => new
                {
                    x.Id,
                    x.EventType,
                    x.Details,
                    x.EmployeeId,
                    actorName = db.Users
                        .Where(actor => actor.Id == x.ActorUserId)
                        .Select(actor => actor.DisplayName)
                        .FirstOrDefault(),
                    employeeName = db.Employees
                        .Where(employee => employee.CompanyId == companyId && employee.Id == x.EmployeeId)
                        .Select(employee => employee.DisplayName)
                        .FirstOrDefault(),
                    x.OccurredAt
                })
                .ToListAsync(cancellationToken);
            var attachments = await db.AssetAttachments.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.AssetId == id)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.OriginalFileName, x.ContentType, x.Length, x.CreatedAt,
                    uploadedBy = db.Users.Where(actor => actor.Id == x.UploadedByUserId).Select(actor => actor.DisplayName).FirstOrDefault() })
                .ToListAsync(cancellationToken);
            return Results.Ok(new { asset, references, history, attachments });
        }).RequireAuthorization("asset.read");

        api.MapGet("/assets/audit-schedule", async (DateOnly? through, string? q, string? state, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var end = through ?? today.AddDays(30);
            if (end < today || end > today.AddDays(366))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["through"] = ["Bitiş tarihi bugünden başlayıp en fazla 366 gün sonrasını kapsamalıdır."] });
            if (q?.Trim().Length > 120)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
            state = string.IsNullOrWhiteSpace(state) ? "all" : state.Trim().ToLowerInvariant();
            if (state is not ("all" or "overdue" or "upcoming"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["state"] = ["Durum all, overdue veya upcoming olmalıdır."] });
            var query = db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId && x.NextAuditOn != null && x.Status != AssetStatuses.Archived);
            var overdue = await query.CountAsync(x => x.NextAuditOn < today, cancellationToken);
            var dueSoon = await query.CountAsync(x => x.NextAuditOn >= today && x.NextAuditOn <= end, cancellationToken);
            var filtered = state == "overdue" ? query.Where(x => x.NextAuditOn < today) :
                state == "upcoming" ? query.Where(x => x.NextAuditOn >= today && x.NextAuditOn <= end) : query.Where(x => x.NextAuditOn <= end);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                filtered = filtered.Where(x => x.AssetTag.Contains(term) || x.Name.Contains(term) ||
                    (x.SerialNumber != null && x.SerialNumber.Contains(term)) || (x.Location != null && x.Location.Contains(term)) ||
                    db.Employees.Any(e => e.CompanyId == companyId && e.Id == x.AssignedEmployeeId && e.DisplayName.Contains(term)));
            }
            var total = await filtered.CountAsync(cancellationToken);
            var items = await filtered
                .OrderBy(x => x.NextAuditOn).ThenBy(x => x.AssetTag).Take(250)
                .Select(x => new { x.Id, x.AssetTag, x.Name, x.NextAuditOn, x.Status, x.Location,
                    employeeName = db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.AssignedEmployeeId).Select(e => e.DisplayName).FirstOrDefault() })
                .ToListAsync(cancellationToken);
            return Results.Ok(new { today, through = end, overdue, dueSoon, total, items });
        }).RequireAuthorization("asset.read");

        api.MapPut("/assets/{id:guid}/audit-schedule", async (Guid id, [FromBody] SetAssetAuditScheduleRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (request.NextAuditOn is { } date && (date < today || date > today.AddYears(10)))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["nextAuditOn"] = ["Denetim tarihi bugün ile 10 yıl sonrası arasında olmalıdır."] });
            var asset = await db.Assets.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (asset is null) return Results.NotFound();
            asset.NextAuditOn = request.NextAuditOn;
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            var actorId = CurrentUserId(user);
            var scheduleDetails = request.NextAuditOn is { } scheduled
                ? $"Sonraki fiziksel denetim planlandı · {scheduled:yyyy-MM-dd}"
                : "Planlı fiziksel denetim kaldırıldı.";
            db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = id, ActorUserId = actorId,
                EventType = request.NextAuditOn is null ? "AuditScheduleCleared" : "AuditScheduled", Details = scheduleDetails });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Asset", TargetId = id, Action = request.NextAuditOn is null ? "AuditScheduleCleared" : "AuditScheduled",
                Details = scheduleDetails });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { id, nextAuditOn = asset.NextAuditOn });
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/{id:guid}/attachments", async (Guid id, HttpRequest request, ClaimsPrincipal user,
            AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            if (!request.HasFormContentType) return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
            var form = await request.ReadFormAsync(cancellationToken);
            var upload = form.Files.GetFile("file");
            if (upload is null || upload.Length is < 1 or > AssetAttachmentMaxSizeBytes)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Dosya 1 bayt ile 10 MB arasında olmalıdır."] });
            await using var buffer = new MemoryStream((int)upload.Length);
            await upload.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            var detected = DetectAssetAttachment(bytes);
            if (detected is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Yalnızca gerçek PNG, JPEG, WebP görselleri ve PDF dosyaları yüklenebilir."] });
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var asset = await db.Assets.FromSqlInterpolated($"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (asset is null) return Results.NotFound();
            if (await db.AssetAttachments.CountAsync(x => x.CompanyId == companyId && x.AssetId == id, cancellationToken) >= 30)
                return Results.Conflict(new { message = "Bir varlığa en fazla 30 dosya eklenebilir." });

            var originalName = CleanAttachmentFileName(upload.FileName);
            var attachmentId = Guid.NewGuid();
            var storageFileName = $"{attachmentId:N}.{detected.Value.Extension}";
            var directory = AssetAttachmentDirectory(configuration, companyId, id);
            Directory.CreateDirectory(directory);
            var destination = Path.Combine(directory, storageFileName);
            var attachment = new AssetAttachment { Id = attachmentId, CompanyId = companyId, AssetId = id,
                UploadedByUserId = actorId, OriginalFileName = originalName, StorageFileName = storageFileName,
                ContentType = detected.Value.ContentType, Length = bytes.LongLength };
            try
            {
                await System.IO.File.WriteAllBytesAsync(destination, bytes, cancellationToken);
                db.AssetAttachments.Add(attachment);
                db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = id, ActorUserId = actorId,
                    EventType = "AttachmentAdded", Details = $"Dosya eklendi: {originalName}" });
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Asset",
                    TargetId = id, Action = "AttachmentAdded", Details = $"{asset.AssetTag}: {originalName} ({bytes.LongLength} bayt)." });
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                try { if (System.IO.File.Exists(destination)) System.IO.File.Delete(destination); }
                catch (IOException) { /* The rolled-back metadata keeps an incomplete upload inaccessible. */ }
                catch (UnauthorizedAccessException) { /* The rolled-back metadata keeps an incomplete upload inaccessible. */ }
                throw;
            }
            return Results.Created($"/api/assets/{id}/attachments/{attachmentId}/file", new { attachment.Id,
                attachment.OriginalFileName, attachment.ContentType, attachment.Length, attachment.CreatedAt });
        }).RequireAuthorization("asset.write")
            .WithMetadata(new RequestSizeLimitAttribute(AssetAttachmentRequestLimitBytes));

        api.MapGet("/assets/{id:guid}/attachments/{attachmentId:guid}/file", async (Guid id, Guid attachmentId,
            [FromQuery] bool download, ClaimsPrincipal user, AppDbContext db, IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var attachment = await db.AssetAttachments.AsNoTracking().SingleOrDefaultAsync(x =>
                x.CompanyId == companyId && x.AssetId == id && x.Id == attachmentId, cancellationToken);
            if (attachment is null || !IsSafeAssetAttachmentFileName(attachment.StorageFileName)) return Results.NotFound();
            var path = Path.Combine(AssetAttachmentDirectory(configuration, companyId, id), attachment.StorageFileName);
            if (!System.IO.File.Exists(path)) return Results.NotFound();
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var fileName = download || !attachment.ContentType.StartsWith("image/", StringComparison.Ordinal)
                ? attachment.OriginalFileName : null;
            return Results.File(stream, attachment.ContentType, fileName, enableRangeProcessing: false);
        }).RequireAuthorization("asset.read");

        api.MapDelete("/assets/{id:guid}/attachments/{attachmentId:guid}", async (Guid id, Guid attachmentId,
            ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var attachment = await db.AssetAttachments.SingleOrDefaultAsync(x =>
                x.CompanyId == companyId && x.AssetId == id && x.Id == attachmentId, cancellationToken);
            if (attachment is null) return Results.NotFound();
            db.AssetAttachments.Remove(attachment);
            db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = id, ActorUserId = actorId,
                EventType = "AttachmentRemoved", Details = $"Dosya kaldırıldı: {attachment.OriginalFileName}" });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Asset",
                TargetId = id, Action = "AttachmentRemoved", Details = attachment.OriginalFileName });
            await db.SaveChangesAsync(cancellationToken);
            var path = Path.Combine(AssetAttachmentDirectory(configuration, companyId, id), attachment.StorageFileName);
            try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
            catch (IOException) { /* The row is removed, so an orphaned file cannot be accessed through the app. */ }
            catch (UnauthorizedAccessException) { /* The row is removed, so an orphaned file cannot be accessed through the app. */ }
            return Results.NoContent();
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/labels/selected", async ([FromBody] SelectedAssetLabelsRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var ids = request.AssetIds?.Distinct().ToArray() ?? [];
            if (ids.Length is < 1 or > 100)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assetIds"] = ["Etiket için 1–100 arası farklı varlık seçin."] });
            var rows = await db.Assets.AsNoTracking()
                .Where(x => x.CompanyId == companyId && ids.Contains(x.Id))
                .Select(x => new
                {
                    x.Id, x.AssetTag, x.Name, x.Status, x.SerialNumber, locationName = x.Location,
                    modelName = db.AssetModels.Where(model => model.CompanyId == companyId && model.Id == x.ModelId)
                        .Select(model => model.Name).FirstOrDefault()
                }).ToListAsync(cancellationToken);
            if (rows.Count != ids.Length) return Results.ValidationProblem(new Dictionary<string, string[]> { ["assetIds"] = ["Seçimde mevcut olmayan veya bu şirkete ait olmayan varlık bulunuyor."] });
            var order = ids.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
            return Results.Ok(rows.OrderBy(x => order[x.Id]));
        }).RequireAuthorization("asset.read");

        api.MapPost("/assets", async ([FromBody] CreateAssetRequest request, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            if (string.IsNullOrWhiteSpace(request.AssetTag) || string.IsNullOrWhiteSpace(request.Name))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["asset"] = ["Etiket (en fazla 80 karakter), ad ve kategori zorunludur."]
                });
            var tag = request.AssetTag.Trim().ToUpperInvariant();
            var name = request.Name.Trim();
            var serial = Clean(request.SerialNumber);
            var location = Clean(request.Location) ?? await GetDefaultLocationPathAsync(db, companyId, cancellationToken);
            if (tag.Length > 80 || name.Length > 240 || request.CategoryId == Guid.Empty)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["asset"] = ["Etiket (en fazla 80 karakter), ad ve kategori zorunludur."]
                });
            if (serial?.Length > 160 || location?.Length > 160)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["asset"] = ["Seri numarası ve konum en fazla 160 karakter olabilir."] });
            if (request.PurchaseCost is decimal purchaseCost &&
                (purchaseCost < 0 || purchaseCost > 9999999999999999.99m))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["purchaseCost"] = ["Alış bedeli 0 ile 9.999.999.999.999.999,99 arasında olmalıdır."] });
            var currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant();
            if (currency.Length != 3)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["currency"] = ["Para birimi üç harfli kod olmalıdır."] });
            if (await db.Assets.AnyAsync(x => x.CompanyId == companyId && x.AssetTag == tag, cancellationToken))
                return Results.Conflict(new { message = "Bu şirket içinde varlık etiketi zaten kayıtlı." });
            if (!await db.AssetCategories.AnyAsync(x => x.CompanyId == companyId && x.Id == request.CategoryId, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["categoryId"] = ["Kategori bu şirkette bulunamadı."] });
            if (request.BrandId is Guid brandId && !await db.AssetBrands.AnyAsync(x => x.CompanyId == companyId && x.Id == brandId, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["brandId"] = ["Marka bu şirkette bulunamadı."] });
            if (request.ModelId is Guid modelId && !await db.AssetModels.AnyAsync(x => x.CompanyId == companyId && x.Id == modelId && x.BrandId == request.BrandId, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["modelId"] = ["Model seçilen marka ve şirketle eşleşmiyor."] });

            var locationCatalogId = await ResolveLocationCatalogIdAsync(db, companyId, location, cancellationToken);
            var asset = new Asset
            {
                CompanyId = companyId, AssetTag = tag, Name = name, CategoryId = request.CategoryId,
                BrandId = request.BrandId, ModelId = request.ModelId,
                SerialNumber = serial, Location = location, LocationCatalogId = locationCatalogId,
                PurchaseDate = request.PurchaseDate, WarrantyExpiresAt = request.WarrantyExpiresAt, PurchaseCost = request.PurchaseCost,
                Currency = currency
            };
            db.Assets.Add(asset);
            db.AssetEvents.Add(new AssetEvent
            {
                CompanyId = companyId, AssetId = asset.Id,
                ActorUserId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null,
                EventType = "Created",
                Details = "Varlık kaydı oluşturuldu."
            });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { message = "Kayıt çakıştı. Etiket ve ilişkili kayıtları kontrol edin." });
            }
            return Results.Created($"/api/assets/{asset.Id}", new AssetListItem(asset.Id, asset.AssetTag,
                asset.Name, asset.Status, asset.SerialNumber, asset.Location, asset.CategoryId,
                await db.AssetCategories.Where(x => x.CompanyId == companyId && x.Id == asset.CategoryId).Select(x => x.Name).SingleAsync(cancellationToken),
                asset.BrandId,
                asset.BrandId is Guid savedBrand ? await db.AssetBrands.Where(x => x.CompanyId == companyId && x.Id == savedBrand).Select(x => x.Name).SingleAsync(cancellationToken) : null,
                asset.ModelId,
                asset.ModelId is Guid savedModel ? await db.AssetModels.Where(x => x.CompanyId == companyId && x.Id == savedModel).Select(x => x.Name).SingleAsync(cancellationToken) : null,
                null, asset.AssignedEmployeeId, asset.WarrantyExpiresAt, asset.UpdatedAt, null, asset.LocationCatalogId));
        }).RequireAuthorization("asset.write");

        api.MapPatch("/assets/{id:guid}", async (Guid id, [FromBody] UpdateAssetRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var tag = request.AssetTag?.Trim().ToUpperInvariant();
            var name = request.Name?.Trim();
            var serial = Clean(request.SerialNumber);
            var location = Clean(request.Location);
            var currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(tag) || tag.Length > 80 || string.IsNullOrWhiteSpace(name) || name.Length > 240 || request.CategoryId == Guid.Empty)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["asset"] = ["Etiket (en fazla 80 karakter), ad ve kategori zorunludur."] });
            if (serial?.Length > 160 || location?.Length > 160)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["asset"] = ["Seri numarası ve konum en fazla 160 karakter olabilir."] });
            if (request.PurchaseCost is decimal cost && (cost < 0 || cost > 9999999999999999.99m))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["purchaseCost"] = ["Alış bedeli 0 ile 9.999.999.999.999.999,99 arasında olmalıdır."] });
            if (currency.Length != 3)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["currency"] = ["Para birimi üç harfli kod olmalıdır."] });

            var current = await db.Assets.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (current is null) return Results.NotFound();
            if (current.UpdatedAt != request.ExpectedUpdatedAt)
                return Results.Conflict(new { message = "Bu varlık başka bir kullanıcı tarafından güncellenmiş. Son bilgileri yenileyip tekrar düzenleyin." });
            if (await db.Assets.AnyAsync(x => x.CompanyId == companyId && x.Id != id && x.AssetTag == tag, cancellationToken))
                return Results.Conflict(new { message = "Bu şirket içinde varlık etiketi zaten kayıtlı." });
            if (!await db.AssetCategories.AnyAsync(x => x.CompanyId == companyId && x.Id == request.CategoryId, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["categoryId"] = ["Kategori bu şirkette bulunamadı."] });
            if (request.BrandId is Guid brandId && !await db.AssetBrands.AnyAsync(x => x.CompanyId == companyId && x.Id == brandId, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["brandId"] = ["Marka bu şirkette bulunamadı."] });
            if (request.ModelId is Guid modelId && !await db.AssetModels.AnyAsync(x => x.CompanyId == companyId && x.Id == modelId && x.BrandId == request.BrandId, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["modelId"] = ["Model seçilen marka ve şirketle eşleşmiyor."] });

            var locationCatalogId = await ResolveLocationCatalogIdAsync(db, companyId, location, cancellationToken);
            var changed = new List<string>();
            if (current.AssetTag != tag) changed.Add("etiket");
            if (current.Name != name) changed.Add("ad");
            if (current.CategoryId != request.CategoryId) changed.Add("kategori");
            if (current.BrandId != request.BrandId) changed.Add("marka");
            if (current.ModelId != request.ModelId) changed.Add("model");
            if (current.SerialNumber != serial) changed.Add("seri numarası");
            if (current.Location != location) changed.Add("konum");
            if (current.LocationCatalogId != locationCatalogId) changed.Add("konum kataloğu bağlantısı");
            if (current.PurchaseDate != request.PurchaseDate) changed.Add("alış tarihi");
            if (current.WarrantyExpiresAt != request.WarrantyExpiresAt) changed.Add("garanti bitiş tarihi");
            if (current.PurchaseCost != request.PurchaseCost || current.Currency != currency) changed.Add("alış bedeli");
            if (changed.Count == 0) return Results.Ok(new { id, updatedAt = current.UpdatedAt, changed = false });

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;
            try
            {
                var affected = await db.Assets.Where(x => x.CompanyId == companyId && x.Id == id && x.UpdatedAt == request.ExpectedUpdatedAt)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.AssetTag, tag!)
                        .SetProperty(x => x.Name, name!)
                        .SetProperty(x => x.CategoryId, request.CategoryId)
                        .SetProperty(x => x.BrandId, request.BrandId)
                        .SetProperty(x => x.ModelId, request.ModelId)
                        .SetProperty(x => x.SerialNumber, serial)
                        .SetProperty(x => x.Location, location)
                        .SetProperty(x => x.LocationCatalogId, locationCatalogId)
                        .SetProperty(x => x.PurchaseDate, request.PurchaseDate)
                        .SetProperty(x => x.WarrantyExpiresAt, request.WarrantyExpiresAt)
                        .SetProperty(x => x.PurchaseCost, request.PurchaseCost)
                        .SetProperty(x => x.Currency, currency)
                        .SetProperty(x => x.UpdatedAt, now), cancellationToken);
                if (affected == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Results.Conflict(new { message = "Bu varlık başka bir kullanıcı tarafından güncellenmiş. Son bilgileri yenileyip tekrar düzenleyin." });
                }
                db.AssetEvents.Add(new AssetEvent
                {
                    CompanyId = companyId,
                    AssetId = id,
                    ActorUserId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null,
                    EventType = "Updated",
                    Details = $"Varlık bilgileri güncellendi: {string.Join(", ", changed)}."
                });
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "Kayıt çakıştı. Etiket ve ilişkili kayıtları kontrol edin." });
            }
            return Results.Ok(new { id, updatedAt = now, changed = true });
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/{id:guid}/audit", async (Guid id, [FromBody] RecordAssetAuditRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            if (request.AuditDate == default || request.AuditDate > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) || request.Notes?.Length > 1900)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["audit"] = ["Denetim tarihi gelecekte olamaz; notlar 1.900 karakteri aşamaz."] });
            var asset = await db.Assets.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (asset is null) return Results.NotFound();
            if (request.NextAuditOn is { } nextAudit && (nextAudit < DateOnly.FromDateTime(DateTime.UtcNow) || nextAudit > DateOnly.FromDateTime(DateTime.UtcNow.AddYears(10))))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["nextAuditOn"] = ["Sonraki denetim tarihi bugün ile 10 yıl sonrası arasında olmalıdır."] });
            asset.NextAuditOn = request.NextAuditOn;
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            var details = $"Fiziksel denetim tamamlandı · {request.AuditDate:yyyy-MM-dd}";
            if (!string.IsNullOrWhiteSpace(request.Notes)) details += $" · {request.Notes.Trim()}";
            if (request.NextAuditOn is { } next) details += $" · Sonraki denetim: {next:yyyy-MM-dd}";
            db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = id,
                ActorUserId = CurrentUserId(user), EventType = "Audited", Details = details });
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/bulk-audit", async ([FromBody] BulkRecordAssetAuditRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var ids = request.AssetIds ?? [];
            if (ids.Length is < 1 or > 100 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assetIds"] = ["1 ile 100 arasında, tekrarsız varlık seçin."] });
            if (request.AuditDate == default || request.AuditDate > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) || request.Notes?.Length > 1900)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["audit"] = ["Denetim tarihi gelecekte olamaz; toplu denetim notu en fazla 1.900 karakter olabilir."] });
            if (request.NextAuditOn is { } nextAudit && (nextAudit < DateOnly.FromDateTime(DateTime.UtcNow) || nextAudit > DateOnly.FromDateTime(DateTime.UtcNow.AddYears(10))))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["nextAuditOn"] = ["Sonraki denetim tarihi bugün ile 10 yıl sonrası arasında olmalıdır."] });

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var assets = await db.Assets.FromSqlInterpolated(
                    $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = ANY ({ids}) ORDER BY \"Id\" FOR UPDATE")
                .ToListAsync(cancellationToken);
            if (assets.Count != ids.Length)
                return Results.NotFound(new { message = "Seçilen varlıklardan biri bulunamadı veya bu şirkete ait değil. Hiçbir denetim kaydedilmedi." });

            var note = request.Notes?.Trim();
            foreach (var asset in assets)
            {
                var details = $"Toplu fiziksel denetim tamamlandı · {request.AuditDate:yyyy-MM-dd}";
                if (!string.IsNullOrWhiteSpace(note)) details += $" · {note}";
                if (request.NextAuditOn is { } scheduled) details += $" · Sonraki denetim: {scheduled:yyyy-MM-dd}";
                db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = asset.Id,
                    ActorUserId = actorId, EventType = "Audited", Details = details });
                asset.NextAuditOn = request.NextAuditOn;
                asset.UpdatedAt = DateTimeOffset.UtcNow;
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                    TargetType = "Asset", TargetId = asset.Id, Action = "BulkAudited", Details = details });
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { recorded = assets.Count, auditDate = request.AuditDate, nextAuditOn = request.NextAuditOn });
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/{id:guid}/transfer", async (Guid id, [FromBody] TransferAssetRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var target = request.TargetLocation?.Trim();
            if (string.IsNullOrWhiteSpace(target) || target.Length > 160 || request.Notes?.Length > 1600)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["transfer"] = ["Yeni konum zorunludur (en fazla 160 karakter); transfer notları en fazla 1.600 karakter olabilir."] });
            var asset = await db.Assets.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (asset is null) return Results.NotFound();
            if (asset.AssignedEmployeeId is not null || asset.Status != AssetStatuses.Available)
                return Results.Conflict(new { message = "Transfer için varlık kullanılabilir durumda ve zimmet dışı olmalıdır." });
            var source = asset.Location?.Trim();
            if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["targetLocation"] = ["Yeni konum mevcut konumla aynı olamaz."] });
            if (string.IsNullOrWhiteSpace(source)) source = "Konum belirtilmemiş";
            asset.Location = target;
            asset.LocationCatalogId = await ResolveLocationCatalogIdAsync(db, companyId, target, cancellationToken);
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            var details = $"Konum transferi: {source} → {target}";
            if (!string.IsNullOrWhiteSpace(request.Notes)) details += $" · {request.Notes.Trim()}";
            db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = id,
                ActorUserId = CurrentUserId(user), EventType = "Transferred", Details = details });
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/bulk-transfer", async ([FromBody] BulkTransferAssetsRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var ids = request.AssetIds ?? [];
            var target = request.TargetLocation?.Trim();
            if (ids.Length is < 1 or > 100 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assetIds"] = ["1 ile 100 arasında, tekrarsız varlık seçin."] });
            if (string.IsNullOrWhiteSpace(target) || target.Length > 160 || request.Notes?.Length > 1600)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["transfer"] = ["Yeni konum zorunludur (en fazla 160 karakter); transfer notları en fazla 1.600 karakter olabilir."] });

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var assets = await db.Assets.FromSqlInterpolated(
                    $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = ANY ({ids}) ORDER BY \"Id\" FOR UPDATE")
                .ToListAsync(cancellationToken);
            if (assets.Count != ids.Length)
                return Results.NotFound(new { message = "Seçilen varlıklardan biri bulunamadı veya bu şirkete ait değil. Hiçbir kayıt değiştirilmedi." });
            if (assets.Any(asset => asset.AssignedEmployeeId is not null || asset.Status != AssetStatuses.Available))
                return Results.Conflict(new { message = "Toplu transfer için seçilen bütün varlıklar kullanılabilir ve zimmet dışı olmalı. Hiçbir kayıt değiştirilmedi." });
            if (assets.Any(asset => string.Equals(
                    string.IsNullOrWhiteSpace(asset.Location) ? "Konum belirtilmemiş" : asset.Location.Trim(),
                    target, StringComparison.OrdinalIgnoreCase)))
                return Results.Conflict(new { message = "Hedef konum seçilen varlıklardan en az biri için zaten geçerli. Hiçbir kayıt değiştirilmedi." });

            var locationCatalogId = await ResolveLocationCatalogIdAsync(db, companyId, target, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var notes = request.Notes?.Trim();
            foreach (var asset in assets)
            {
                var source = string.IsNullOrWhiteSpace(asset.Location) ? "Konum belirtilmemiş" : asset.Location.Trim();
                asset.Location = target;
                asset.LocationCatalogId = locationCatalogId;
                asset.UpdatedAt = now;
                var details = $"Toplu konum transferi: {source} → {target}";
                if (!string.IsNullOrWhiteSpace(notes)) details += $" · {notes}";
                db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = asset.Id,
                    ActorUserId = actorId, EventType = "Transferred", Details = details });
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                    TargetType = "Asset", TargetId = asset.Id, Action = "BulkTransferred", Details = details });
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { updated = assets.Count, targetLocation = target });
        }).RequireAuthorization("asset.write");

        api.MapGet("/calendar", async (ClaimsPrincipal user, AppDbContext db, DateOnly? from,
            DateOnly? to, string? kind, int? limit, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = from ?? today.AddDays(-30);
            var end = to ?? today.AddDays(90);
            kind = string.IsNullOrWhiteSpace(kind) ? null : kind.Trim().ToLowerInvariant();
            if (start > end || end.DayNumber - start.DayNumber > 365)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["dateRange"] = ["Takvim aralığı ters olamaz ve en fazla 366 gün olabilir."] });
            if (!string.IsNullOrWhiteSpace(kind) && kind is not ("maintenance" or "license" or "warranty" or "audit"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["kind"] = ["Tür maintenance, license, warranty veya audit olmalıdır."] });

            var maxItems = Math.Clamp(limit ?? 200, 1, 500);
            var total = 0;
            var events = new List<CalendarEntry>(maxItems * 3);
            if (kind is null or "maintenance")
            {
                var maintenanceQuery =
                    from maintenance in db.AssetMaintenances.AsNoTracking()
                    join asset in db.Assets.AsNoTracking() on new { maintenance.CompanyId, maintenance.AssetId } equals new { asset.CompanyId, AssetId = asset.Id }
                    where maintenance.CompanyId == companyId && maintenance.Status == "Scheduled" &&
                        maintenance.PlannedOn >= start && maintenance.PlannedOn <= end
                    select new { maintenance, asset };
                total += await maintenanceQuery.CountAsync(cancellationToken);
                var maintenanceEvents = await maintenanceQuery.OrderBy(item => item.maintenance.PlannedOn).ThenBy(item => item.asset.AssetTag).ThenBy(item => item.maintenance.Id)
                    .Take(maxItems).Select(item => new CalendarEntry("maintenance", item.maintenance.PlannedOn!.Value, item.asset.Id, item.maintenance.Id,
                        item.asset.AssetTag + " · " + item.asset.Name, item.maintenance.MaintenanceType + (item.asset.Location == null ? "" : " · " + item.asset.Location), "Scheduled", null))
                    .ToListAsync(cancellationToken);
                events.AddRange(maintenanceEvents);
            }
            if (kind is null or "license")
            {
                var licenseQuery = db.SoftwareLicenses.AsNoTracking()
                    .Where(license => license.CompanyId == companyId && license.ExpirationDate >= start && license.ExpirationDate <= end);
                total += await licenseQuery.CountAsync(cancellationToken);
                var licenseEvents = await licenseQuery.OrderBy(item => item.ExpirationDate).ThenBy(item => item.Name).ThenBy(item => item.Id)
                    .Take(maxItems).Select(license => new CalendarEntry("license", license.ExpirationDate!.Value, null, license.Id,
                        license.Name, license.Publisher, "Expiration", license.AutoRenew)).ToListAsync(cancellationToken);
                events.AddRange(licenseEvents);
            }
            if (kind is null or "warranty")
            {
                var warrantyQuery = db.Assets.AsNoTracking()
                    .Where(asset => asset.CompanyId == companyId && asset.WarrantyExpiresAt >= start && asset.WarrantyExpiresAt <= end);
                total += await warrantyQuery.CountAsync(cancellationToken);
                var warrantyEvents = await warrantyQuery.OrderBy(asset => asset.WarrantyExpiresAt).ThenBy(asset => asset.AssetTag).ThenBy(asset => asset.Id)
                    .Take(maxItems).Select(asset => new CalendarEntry("warranty", asset.WarrantyExpiresAt!.Value, asset.Id, asset.Id,
                        asset.AssetTag + " · " + asset.Name, asset.Location, "WarrantyExpiration", null))
                    .ToListAsync(cancellationToken);
                events.AddRange(warrantyEvents);
            }
            if (kind is null or "audit")
            {
                var auditQuery = db.Assets.AsNoTracking()
                    .Where(asset => asset.CompanyId == companyId && asset.Status != AssetStatuses.Archived &&
                        asset.NextAuditOn >= start && asset.NextAuditOn <= end);
                total += await auditQuery.CountAsync(cancellationToken);
                var auditEvents = await auditQuery.OrderBy(asset => asset.NextAuditOn).ThenBy(asset => asset.AssetTag).ThenBy(asset => asset.Id)
                    .Take(maxItems).Select(asset => new CalendarEntry("audit", asset.NextAuditOn!.Value, asset.Id, asset.Id,
                        asset.AssetTag + " · " + asset.Name, asset.Location, "AuditScheduled", null))
                    .ToListAsync(cancellationToken);
                events.AddRange(auditEvents);
            }

            var ordered = events.OrderBy(item => item.Date).ThenBy(item => item.Kind).ThenBy(item => item.Title).ToArray();
            return Results.Ok(new { from = start, to = end, kind, total,
                items = ordered.Take(maxItems) });
        }).RequireAuthorization("asset.read");

        api.MapGet("/maintenances", async (ClaimsPrincipal user, AppDbContext db, string? q, string? status,
            DateOnly? from, DateOnly? to, int? page, int? pageSize, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            if (from.HasValue && to.HasValue && from > to)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["dateRange"] = ["Başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            if (q?.Length > 120)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
            var query = from maintenance in db.AssetMaintenances.AsNoTracking()
                        join asset in db.Assets.AsNoTracking() on new { maintenance.CompanyId, maintenance.AssetId } equals new { asset.CompanyId, AssetId = asset.Id }
                        where maintenance.CompanyId == companyId
                        select new { maintenance, asset };
            if (status is "Scheduled" or "InProgress" or "Completed" or "Cancelled") query = query.Where(x => x.maintenance.Status == status);
            else if (!string.IsNullOrWhiteSpace(status))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Durum Scheduled, InProgress, Completed veya Cancelled olmalıdır."] });
            if (from.HasValue) query = query.Where(x => x.maintenance.Status == "Scheduled" || x.maintenance.Status == "Cancelled"
                ? x.maintenance.PlannedOn >= from.Value : x.maintenance.StartedOn >= from.Value);
            if (to.HasValue) query = query.Where(x => x.maintenance.Status == "Scheduled" || x.maintenance.Status == "Cancelled"
                ? x.maintenance.PlannedOn <= to.Value : x.maintenance.StartedOn <= to.Value);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.maintenance.MaintenanceType.Contains(term) ||
                    (x.maintenance.Supplier != null && x.maintenance.Supplier.Contains(term)) ||
                    (x.maintenance.Description != null && x.maintenance.Description.Contains(term)) ||
                    (x.maintenance.Notes != null && x.maintenance.Notes.Contains(term)) ||
                    (x.maintenance.CompletionNotes != null && x.maintenance.CompletionNotes.Contains(term)) ||
                    x.asset.AssetTag.Contains(term) || x.asset.Name.Contains(term) ||
                    (x.asset.SerialNumber != null && x.asset.SerialNumber.Contains(term)) ||
                    (x.asset.Location != null && x.asset.Location.Contains(term)));
            }
            var total = await query.CountAsync(cancellationToken);
            var size = Math.Clamp(pageSize ?? 50, 1, 100);
            var pageCount = (int)Math.Ceiling(total / (double)size);
            var pageNumber = Math.Clamp(page ?? 1, 1, Math.Max(1, pageCount));
            var rows = await query.OrderByDescending(x => x.maintenance.PlannedOn ?? x.maintenance.StartedOn)
                .ThenByDescending(x => x.maintenance.CreatedAt).Skip((pageNumber - 1) * size).Take(size)
                .Select(x => new
                {
                    x.maintenance.Id, x.maintenance.AssetId, x.asset.AssetTag, assetName = x.asset.Name,
                    x.asset.Location, x.maintenance.MaintenanceType, x.maintenance.Supplier,
                    x.maintenance.Description, x.maintenance.PlannedOn, x.maintenance.StartedOn, x.maintenance.CompletedOn,
                    x.maintenance.Status, x.maintenance.Cost, x.maintenance.Currency,
                    x.maintenance.Notes, x.maintenance.CompletionNotes,
                    actorName = db.Users.Where(actor => actor.Id == x.maintenance.ActorUserId)
                        .Select(actor => actor.DisplayName).FirstOrDefault()
                }).ToListAsync(cancellationToken);
            return Results.Ok(new { rows, total, page = pageNumber, pageSize = size, pageCount = Math.Max(1, pageCount) });
        }).RequireAuthorization("asset.read");

        api.MapGet("/maintenances/export.csv", async (ClaimsPrincipal user, AppDbContext db,
            string? q, string? status, DateOnly? from, DateOnly? to, HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) { context.Response.StatusCode = 403; return; }
            if (q?.Length > 120 || (from.HasValue && to.HasValue && from > to) ||
                (!string.IsNullOrWhiteSpace(status) && status is not ("Scheduled" or "InProgress" or "Completed" or "Cancelled")))
            {
                await Results.ValidationProblem(new Dictionary<string, string[]> {
                    ["filters"] = ["Arama, durum veya tarih filtrelerini kontrol edin."]
                }).ExecuteAsync(context);
                return;
            }
            var query = from maintenance in db.AssetMaintenances.AsNoTracking()
                        join asset in db.Assets.AsNoTracking() on new { maintenance.CompanyId, maintenance.AssetId } equals new { asset.CompanyId, AssetId = asset.Id }
                        where maintenance.CompanyId == companyId
                        select new { maintenance, asset };
            if (status is "Scheduled" or "InProgress" or "Completed" or "Cancelled") query = query.Where(x => x.maintenance.Status == status);
            if (from.HasValue) query = query.Where(x => x.maintenance.Status == "Scheduled" || x.maintenance.Status == "Cancelled"
                ? x.maintenance.PlannedOn >= from.Value : x.maintenance.StartedOn >= from.Value);
            if (to.HasValue) query = query.Where(x => x.maintenance.Status == "Scheduled" || x.maintenance.Status == "Cancelled"
                ? x.maintenance.PlannedOn <= to.Value : x.maintenance.StartedOn <= to.Value);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.maintenance.MaintenanceType.Contains(term) ||
                    (x.maintenance.Supplier != null && x.maintenance.Supplier.Contains(term)) ||
                    (x.maintenance.Description != null && x.maintenance.Description.Contains(term)) ||
                    (x.maintenance.Notes != null && x.maintenance.Notes.Contains(term)) ||
                    (x.maintenance.CompletionNotes != null && x.maintenance.CompletionNotes.Contains(term)) ||
                    x.asset.AssetTag.Contains(term) || x.asset.Name.Contains(term) ||
                    (x.asset.SerialNumber != null && x.asset.SerialNumber.Contains(term)) ||
                    (x.asset.Location != null && x.asset.Location.Contains(term)));
            }
            context.Response.ContentType = "text/csv; charset=utf-8";
            context.Response.Headers["Content-Disposition"] = $"attachment; filename=envantertr-bakim-{DateTime.UtcNow:yyyyMMdd}.csv";
            context.Response.Headers["Cache-Control"] = "no-store";
            await using var writer = new StreamWriter(context.Response.Body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), 16 * 1024, leaveOpen: true);
            await writer.WriteLineAsync("Varlık Etiketi;Varlık;Konum;Bakım Türü;Servis;Durum;Planlanan Tarih;Başlangıç;Tamamlanma;Maliyet;Para Birimi;İşlemi Yapan;Açıklama;Not;Tamamlanma Notu".AsMemory(), cancellationToken);
            var rows = query.OrderByDescending(x => x.maintenance.PlannedOn ?? x.maintenance.StartedOn).ThenByDescending(x => x.maintenance.CreatedAt)
                .Select(x => new { x.asset.AssetTag, AssetName = x.asset.Name, x.asset.Location,
                    x.maintenance.MaintenanceType, x.maintenance.Supplier, x.maintenance.Status,
                    x.maintenance.PlannedOn, x.maintenance.StartedOn, x.maintenance.CompletedOn, x.maintenance.Cost, x.maintenance.Currency,
                    ActorName = db.Users.Where(actor => actor.Id == x.maintenance.ActorUserId).Select(actor => actor.DisplayName).FirstOrDefault(),
                    x.maintenance.Description, x.maintenance.Notes, x.maintenance.CompletionNotes });
            await foreach (var row in rows.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                var line = string.Join(';', new[] { CsvCell(row.AssetTag), CsvCell(row.AssetName), CsvCell(row.Location),
                    CsvCell(row.MaintenanceType), CsvCell(row.Supplier), CsvCell(row.Status),
                    CsvCell(row.PlannedOn?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)),
                    CsvCell(row.Status is "Scheduled" or "Cancelled" ? null : row.StartedOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)),
                    CsvCell(row.CompletedOn?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)),
                    CsvCell(row.Cost?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)),
                    CsvCell(row.Currency), CsvCell(row.ActorName), CsvCell(row.Description), CsvCell(row.Notes), CsvCell(row.CompletionNotes) });
                await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
            }
            await writer.FlushAsync(cancellationToken);
        }).RequireAuthorization("asset.read");

        api.MapGet("/assets/{id:guid}/maintenances", async (Guid id, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            if (!await db.Assets.AnyAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken))
                return Results.NotFound();
            var rows = await db.AssetMaintenances.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.AssetId == id)
                .OrderByDescending(x => x.PlannedOn ?? x.StartedOn).ThenByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id, x.MaintenanceType, x.Supplier, x.Description, x.PlannedOn, x.StartedOn, x.CompletedOn,
                    x.Status, x.Cost, x.Currency, x.Notes, x.CompletionNotes, x.CreatedAt,
                    actorName = db.Users.Where(actor => actor.Id == x.ActorUserId)
                        .Select(actor => actor.DisplayName).FirstOrDefault()
                }).ToListAsync(cancellationToken);
            return Results.Ok(rows);
        }).RequireAuthorization("asset.read");

        api.MapPost("/assets/{id:guid}/maintenances/schedule", async (Guid id, [FromBody] ScheduleMaintenanceRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var type = request.MaintenanceType?.Trim();
            var supplier = request.Supplier?.Trim();
            var description = request.Description?.Trim();
            var notes = request.Notes?.Trim();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (string.IsNullOrWhiteSpace(type) || type.Length > 120 || supplier?.Length > 200 ||
                description?.Length > 1000 || notes?.Length > 2000 || request.PlannedOn < today ||
                request.PlannedOn > today.AddYears(5) || request.Cost is < 0 or > 1_000_000_000m || !IsCurrency(request.Currency))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["maintenance"] = ["Bakım türü, gelecekteki plan tarihi, para birimi veya alan uzunluklarını kontrol edin."] });

            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            var asset = await db.Assets.FromSqlInterpolated(
                    $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (asset is null) return Results.NotFound();
            if (asset.Status is not (AssetStatuses.Available or AssetStatuses.Deployed))
                return Results.Conflict(new { message = "Yalnızca kullanılabilir veya zimmetli varlık için bakım planlanabilir." });
            if (await db.AssetMaintenances.AnyAsync(x => x.CompanyId == companyId && x.AssetId == id && (x.Status == "InProgress" || x.Status == "Scheduled"), cancellationToken))
                return Results.Conflict(new { message = "Bu varlık için devam eden bakım veya başka bir planlanmış bakım bulunuyor." });

            var now = DateTimeOffset.UtcNow;
            var maintenance = new AssetMaintenance
            {
                CompanyId = companyId, AssetId = id, ActorUserId = CurrentUserId(user),
                MaintenanceType = type, Supplier = Clean(supplier), SupplierId = await SupplierCatalogEndpoints.ResolveSupplierIdAsync(db, companyId, supplier, cancellationToken), Description = Clean(description),
                PlannedOn = request.PlannedOn, StartedOn = request.PlannedOn, Cost = request.Cost,
                Currency = request.Currency.Trim().ToUpperInvariant(), Notes = Clean(notes),
                Status = "Scheduled", CreatedAt = now, UpdatedAt = now
            };
            db.AssetMaintenances.Add(maintenance);
            db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = id,
                ActorUserId = CurrentUserId(user), EventType = "MaintenanceScheduled",
                Details = $"Bakım planlandı: {type} · {request.PlannedOn:yyyy-MM-dd}" });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "Bakım planı çakıştı; varlığın güncel durumunu yenileyip tekrar deneyin." });
            }
            return Results.Created($"/api/assets/{id}/maintenances/{maintenance.Id}", new { maintenance.Id, maintenance.Status, maintenance.PlannedOn });
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/{assetId:guid}/maintenances/{maintenanceId:guid}/start",
            async (Guid assetId, Guid maintenanceId, ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
            {
                if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
                await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
                var maintenance = await db.AssetMaintenances.FromSqlInterpolated(
                        $"SELECT * FROM \"AssetMaintenances\" WHERE \"CompanyId\" = {companyId} AND \"AssetId\" = {assetId} AND \"Id\" = {maintenanceId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken);
                if (maintenance is null) return Results.NotFound();
                if (maintenance.Status != "Scheduled") return Results.Conflict(new { message = "Yalnızca planlanmış bakım başlatılabilir." });
                var asset = await db.Assets.FromSqlInterpolated(
                        $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {assetId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken);
                if (asset is null) return Results.NotFound();
                if (asset.Status != AssetStatuses.Available || asset.AssignedEmployeeId is not null)
                    return Results.Conflict(new { message = "Bakım başlatmak için planlı varlık kullanılabilir ve zimmet dışı olmalıdır. Varlığı iade alın, sonra tekrar deneyin." });
                var now = DateTimeOffset.UtcNow;
                var startedOn = DateOnly.FromDateTime(now.UtcDateTime);
                maintenance.Status = "InProgress"; maintenance.StartedOn = startedOn; maintenance.UpdatedAt = now;
                asset.Status = AssetStatuses.Maintenance; asset.UpdatedAt = now;
                db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = assetId,
                    ActorUserId = CurrentUserId(user), EventType = "MaintenanceStarted",
                    Details = $"Planlı bakım başlatıldı: {maintenance.MaintenanceType}" });
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Results.NoContent();
            }).RequireAuthorization("asset.write");

        api.MapPost("/assets/{assetId:guid}/maintenances/{maintenanceId:guid}/cancel",
            async (Guid assetId, Guid maintenanceId, ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
            {
                if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
                await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
                var maintenance = await db.AssetMaintenances.FromSqlInterpolated(
                        $"SELECT * FROM \"AssetMaintenances\" WHERE \"CompanyId\" = {companyId} AND \"AssetId\" = {assetId} AND \"Id\" = {maintenanceId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken);
                if (maintenance is null) return Results.NotFound();
                if (maintenance.Status != "Scheduled") return Results.Conflict(new { message = "Yalnızca planlanmış bakım iptal edilebilir." });
                maintenance.Status = "Cancelled"; maintenance.UpdatedAt = DateTimeOffset.UtcNow;
                db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = assetId,
                    ActorUserId = CurrentUserId(user), EventType = "MaintenanceScheduleCancelled",
                    Details = $"Bakım planı iptal edildi: {maintenance.MaintenanceType}" });
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Results.NoContent();
            }).RequireAuthorization("asset.write");

        api.MapPost("/assets/{id:guid}/maintenances", async (Guid id, [FromBody] StartMaintenanceRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var type = request.MaintenanceType?.Trim();
            var supplier = request.Supplier?.Trim();
            var description = request.Description?.Trim();
            var notes = request.Notes?.Trim();
            if (string.IsNullOrWhiteSpace(type) || type.Length > 120 || supplier?.Length > 200 ||
                description?.Length > 1000 || notes?.Length > 2000 || request.StartedOn == default ||
                request.StartedOn > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) ||
                request.Cost is < 0 or > 1_000_000_000m || !IsCurrency(request.Currency))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["maintenance"] = ["Bakım türü, tarih, para birimi veya alan uzunluklarını kontrol edin."] });

            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            var asset = await db.Assets.FromSqlInterpolated(
                    $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (asset is null) return Results.NotFound();
            if (asset.Status is not (AssetStatuses.Available or AssetStatuses.Maintenance) || asset.AssignedEmployeeId is not null)
                return Results.Conflict(new { message = "Bakım başlatmak için varlık kullanılabilir durumda, zimmet dışı olmalıdır." });
            if (await db.AssetMaintenances.AnyAsync(x => x.CompanyId == companyId && x.AssetId == id && (x.Status == "InProgress" || x.Status == "Scheduled"), cancellationToken))
                return Results.Conflict(new { message = "Bu varlık için devam eden bakım veya planlanmış bir bakım kaydı zaten var." });

            var now = DateTimeOffset.UtcNow;
            var maintenance = new AssetMaintenance
            {
                CompanyId = companyId, AssetId = id, ActorUserId = CurrentUserId(user),
                MaintenanceType = type, Supplier = Clean(supplier), SupplierId = await SupplierCatalogEndpoints.ResolveSupplierIdAsync(db, companyId, supplier, cancellationToken), Description = Clean(description),
                StartedOn = request.StartedOn, Cost = request.Cost, Currency = request.Currency.Trim().ToUpperInvariant(),
                Notes = Clean(notes), Status = "InProgress", CreatedAt = now, UpdatedAt = now
            };
            asset.Status = AssetStatuses.Maintenance; asset.UpdatedAt = now;
            db.AssetMaintenances.Add(maintenance);
            db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = id,
                ActorUserId = CurrentUserId(user), EventType = "MaintenanceStarted",
                Details = $"Bakım başladı: {type}" + (string.IsNullOrWhiteSpace(supplier) ? "" : $" · {supplier}") });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "Bakım kaydı çakıştı; varlığın güncel durumunu yenileyip tekrar deneyin." });
            }
            return Results.Created($"/api/assets/{id}/maintenances/{maintenance.Id}", new { maintenance.Id, maintenance.Status });
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/{assetId:guid}/maintenances/{maintenanceId:guid}/complete",
            async (Guid assetId, Guid maintenanceId, [FromBody] CompleteMaintenanceRequest request,
                ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
            {
                if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
                if (request.CompletedOn == default || request.CompletedOn > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) || request.Notes?.Length > 2000)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["completion"] = ["Tamamlanma tarihi veya not uzunluğunu kontrol edin."] });

                await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
                var maintenance = await db.AssetMaintenances.FromSqlInterpolated(
                        $"SELECT * FROM \"AssetMaintenances\" WHERE \"CompanyId\" = {companyId} AND \"AssetId\" = {assetId} AND \"Id\" = {maintenanceId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken);
                if (maintenance is null) return Results.NotFound();
                if (maintenance.Status != "InProgress") return Results.Conflict(new { message = "Bu bakım kaydı zaten kapatılmış." });
                if (request.CompletedOn < maintenance.StartedOn)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["completedOn"] = ["Tamamlanma tarihi başlangıç tarihinden önce olamaz."] });
                var asset = await db.Assets.FromSqlInterpolated(
                        $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {assetId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken);
                if (asset is null) return Results.NotFound();
                var now = DateTimeOffset.UtcNow;
                maintenance.Status = "Completed"; maintenance.CompletedOn = request.CompletedOn;
                maintenance.CompletionNotes = Clean(request.Notes); maintenance.UpdatedAt = now;
                if (asset.Status == AssetStatuses.Maintenance) asset.Status = AssetStatuses.Available;
                asset.UpdatedAt = now;
                db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = assetId,
                    ActorUserId = CurrentUserId(user), EventType = "MaintenanceCompleted",
                    Details = $"Bakım tamamlandı: {maintenance.MaintenanceType} · {request.CompletedOn:yyyy-MM-dd}" });
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Results.NoContent();
            }).RequireAuthorization("asset.write");

        api.MapGet("/lookups/{kind}", async (string kind, ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            switch (kind.ToLowerInvariant())
            {
                case "categories":
                    return Results.Ok((object)await db.AssetCategories.AsNoTracking().Where(x => x.CompanyId == companyId)
                        .OrderBy(x => x.Name).Select(x => new { x.Id, x.Name,
                            AssetCount = db.Assets.Count(a => a.CompanyId == companyId && a.CategoryId == x.Id) }).ToListAsync(cancellationToken));
                case "brands":
                    return Results.Ok((object)await db.AssetBrands.AsNoTracking().Where(x => x.CompanyId == companyId)
                        .OrderBy(x => x.Name).Select(x => new { x.Id, x.Name,
                            AssetCount = db.Assets.Count(a => a.CompanyId == companyId && a.BrandId == x.Id),
                            ModelCount = db.AssetModels.Count(m => m.CompanyId == companyId && m.BrandId == x.Id) }).ToListAsync(cancellationToken));
                case "models":
                    return Results.Ok((object)await db.AssetModels.AsNoTracking().Where(x => x.CompanyId == companyId)
                        .OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.BrandId,
                            BrandName = db.AssetBrands.Where(b => b.CompanyId == companyId && b.Id == x.BrandId).Select(b => b.Name).FirstOrDefault(),
                            AssetCount = db.Assets.Count(a => a.CompanyId == companyId && a.ModelId == x.Id) }).ToListAsync(cancellationToken));
                default:
                    return Results.NotFound();
            }
        }).RequireAuthorization("asset.read");

        api.MapGet("/lookups/{kind}/{id:guid}", async (string kind, Guid id, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            object? item = kind.ToLowerInvariant() switch
            {
                "categories" => await db.AssetCategories.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                    .Select(x => new { x.Id, x.Name }).SingleOrDefaultAsync(cancellationToken),
                "brands" => await db.AssetBrands.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                    .Select(x => new { x.Id, x.Name }).SingleOrDefaultAsync(cancellationToken),
                "models" => await db.AssetModels.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                    .Select(x => new { x.Id, x.Name, x.BrandId }).SingleOrDefaultAsync(cancellationToken),
                _ => null
            };
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).RequireAuthorization("asset.read");

        api.MapPost("/lookups/{kind}", async (string kind, [FromBody] CreateLookupRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Ad zorunludur ve en fazla 120 karakter olabilir."] });
            if (request.BrandId is Guid brandId && !await db.AssetBrands.AnyAsync(x => x.CompanyId == companyId && x.Id == brandId, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["brandId"] = ["Marka bu şirkette bulunamadı."] });
            object? created = kind.ToLowerInvariant() switch
            {
                "categories" => AddCategory(db, companyId, name),
                "brands" => AddBrand(db, companyId, name),
                "models" when request.BrandId is not null => AddModel(db, companyId, name, request.BrandId.Value),
                "models" => AddModel(db, companyId, name, null),
                _ => null
            };
            if (created is null) return Results.NotFound();
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Aynı isimli kayıt zaten mevcut veya marka geçersiz." }); }
            var id = (Guid)created.GetType().GetProperty("Id")!.GetValue(created)!;
            return Results.Created($"/api/lookups/{kind}/{id}", created);
        }).RequireAuthorization("asset.write");

        api.MapPatch("/lookups/{kind}/{id:guid}", async (string kind, Guid id, [FromBody] RenameLookupRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Ad zorunludur ve en fazla 120 karakter olabilir."] });
            var normalizedKind = kind.ToLowerInvariant();
            string? previous = null;
            switch (normalizedKind)
            {
                case "categories":
                    var category = await db.AssetCategories.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
                    if (category is null) return Results.NotFound();
                    if (await db.AssetCategories.AnyAsync(x => x.CompanyId == companyId && x.Id != id && x.Name.ToUpper() == name.ToUpper(), cancellationToken)) return Results.Conflict(new { message = "Bu isimde kategori zaten var." });
                    previous = category.Name; category.Name = name; break;
                case "brands":
                    var brand = await db.AssetBrands.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
                    if (brand is null) return Results.NotFound();
                    if (await db.AssetBrands.AnyAsync(x => x.CompanyId == companyId && x.Id != id && x.Name.ToUpper() == name.ToUpper(), cancellationToken)) return Results.Conflict(new { message = "Bu isimde marka zaten var." });
                    previous = brand.Name; brand.Name = name; break;
                case "models":
                    var model = await db.AssetModels.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
                    if (model is null) return Results.NotFound();
                    if (await db.AssetModels.AnyAsync(x => x.CompanyId == companyId && x.Id != id && x.BrandId == model.BrandId && x.Name.ToUpper() == name.ToUpper(), cancellationToken)) return Results.Conflict(new { message = "Bu marka altında aynı isimde model zaten var." });
                    previous = model.Name; model.Name = name; break;
                default: return Results.NotFound();
            }
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId,
                ActorUserId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null,
                TargetType = normalizedKind, TargetId = id, Action = "Renamed", Details = $"{previous} → {name}" });
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Aynı isimde kayıt zaten var." }); }
            return Results.Ok(new { id, name });
        }).RequireAuthorization("asset.write");

        api.MapDelete("/lookups/{kind}/{id:guid}", async (string kind, Guid id, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var normalizedKind = kind.ToLowerInvariant();
            string? deletedName = null;
            switch (normalizedKind)
            {
                case "categories":
                    var category = await db.AssetCategories.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
                    if (category is null) return Results.NotFound();
                    if (await db.Assets.AnyAsync(x => x.CompanyId == companyId && x.CategoryId == id, cancellationToken)) return Results.Conflict(new { message = "Bu kategori varlıklarda kullanılıyor; silinemez." });
                    deletedName = category.Name; db.AssetCategories.Remove(category); break;
                case "brands":
                    var brand = await db.AssetBrands.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
                    if (brand is null) return Results.NotFound();
                    if (await db.Assets.AnyAsync(x => x.CompanyId == companyId && x.BrandId == id, cancellationToken) || await db.AssetModels.AnyAsync(x => x.CompanyId == companyId && x.BrandId == id, cancellationToken)) return Results.Conflict(new { message = "Bu marka varlıklarda veya modellerde kullanılıyor; önce ilişkili kayıtları düzenleyin." });
                    deletedName = brand.Name; db.AssetBrands.Remove(brand); break;
                case "models":
                    var model = await db.AssetModels.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
                    if (model is null) return Results.NotFound();
                    if (await db.Assets.AnyAsync(x => x.CompanyId == companyId && x.ModelId == id, cancellationToken)) return Results.Conflict(new { message = "Bu model varlıklarda kullanılıyor; silinemez." });
                    deletedName = model.Name; db.AssetModels.Remove(model); break;
                default: return Results.NotFound();
            }
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId,
                ActorUserId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null,
                TargetType = normalizedKind, TargetId = id, Action = "Deleted", Details = deletedName });
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Kayıt başka bir veride kullanıldığı için silinemedi." }); }
            return Results.NoContent();
        }).RequireAuthorization("asset.write");

        api.MapGet("/assets/export-all.csv", async ([FromQuery] string? q, [FromQuery] string? status,
            [FromQuery] Guid? categoryId, [FromQuery] Guid? brandId, [FromQuery] Guid? modelId,
            [FromQuery] Guid? employeeId, [FromQuery] string? location, [FromQuery] string? warranty,
            [FromQuery] string? sortBy, [FromQuery] string? sortDirection,
            HttpContext context, ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            if (q is not null && q.Trim().Length > 120)
            {
                await Results.ValidationProblem(new Dictionary<string, string[]> {
                    ["q"] = ["Arama metni 120 karakteri aşamaz."]
                }).ExecuteAsync(context);
                return;
            }

            var query = db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
            if (categoryId is Guid selectedCategory) query = query.Where(x => x.CategoryId == selectedCategory);
            if (brandId is Guid selectedBrand) query = query.Where(x => x.BrandId == selectedBrand);
            if (modelId is Guid selectedModel) query = query.Where(x => x.ModelId == selectedModel);
            if (employeeId is Guid selectedEmployee) query = query.Where(x => x.AssignedEmployeeId == selectedEmployee);
            if (!string.IsNullOrWhiteSpace(location))
            {
                var selectedLocation = location.Trim();
                if (selectedLocation.Length > 160)
                {
                    await Results.ValidationProblem(new Dictionary<string, string[]> { ["location"] = ["Konum filtresi 160 karakteri aşamaz."] }).ExecuteAsync(context);
                    return;
                }
                query = query.Where(x => x.Location == selectedLocation);
            }
            if (!TryApplyWarrantyFilter(query, warranty, out var warrantyFiltered))
            {
                await Results.ValidationProblem(new Dictionary<string, string[]> { ["warranty"] = ["Garanti filtresi expired, expiring, active veya unspecified olmalıdır."] }).ExecuteAsync(context);
                return;
            }
            query = warrantyFiltered;
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.AssetTag.Contains(term) || x.Name.Contains(term) ||
                    (x.SerialNumber != null && x.SerialNumber.Contains(term)) ||
                    (x.Location != null && x.Location.Contains(term)) ||
                    db.Employees.Any(e => e.CompanyId == companyId && e.Id == x.AssignedEmployeeId &&
                        (e.DisplayName.Contains(term) || e.EmployeeNumber.Contains(term))) ||
                    db.AssetCategories.Any(c => c.CompanyId == companyId && c.Id == x.CategoryId && c.Name.Contains(term)) ||
                    db.AssetBrands.Any(b => b.CompanyId == companyId && b.Id == x.BrandId && b.Name.Contains(term)) ||
                    db.AssetModels.Any(m => m.CompanyId == companyId && m.Id == x.ModelId && m.Name.Contains(term)));
            }

            var ordered = OrderAssets(query, db, companyId, sortBy, sortDirection);
            if (ordered is null)
            {
                await Results.ValidationProblem(new Dictionary<string, string[]> { ["sortBy"] = ["Sıralama alanı veya yönü desteklenmiyor."] }).ExecuteAsync(context);
                return;
            }
            var total = await query.CountAsync(cancellationToken);
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/csv; charset=utf-8";
            context.Response.Headers["Content-Disposition"] = $"attachment; filename=envantertr-varliklar-{DateTime.UtcNow:yyyyMMdd}.csv";
            context.Response.Headers["Cache-Control"] = "no-store";
            context.Response.Headers["X-Total-Count"] = total.ToString(System.Globalization.CultureInfo.InvariantCulture);
            await using var writer = new StreamWriter(context.Response.Body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), 16 * 1024, leaveOpen: true);
            await writer.WriteLineAsync("Etiket;Ad;Durum;Kategori;Marka;Model;Seri No;Konum;Personel No;Çalışan;Alış Tarihi;Garanti Bitiş Tarihi;Alış Bedeli;Para Birimi".AsMemory(), cancellationToken);
            var rows = ordered.Select(x => new AssetExportRow(
                x.AssetTag, x.Name, x.Status,
                db.AssetCategories.Where(c => c.CompanyId == companyId && c.Id == x.CategoryId).Select(c => c.Name).FirstOrDefault(),
                db.AssetBrands.Where(b => b.CompanyId == companyId && b.Id == x.BrandId).Select(b => b.Name).FirstOrDefault(),
                db.AssetModels.Where(m => m.CompanyId == companyId && m.Id == x.ModelId).Select(m => m.Name).FirstOrDefault(),
                x.SerialNumber, x.Location,
                db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.AssignedEmployeeId)
                    .Select(e => e.EmployeeNumber).FirstOrDefault(),
                db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.AssignedEmployeeId)
                    .Select(e => e.DisplayName).FirstOrDefault(),
                x.PurchaseDate, x.WarrantyExpiresAt, x.PurchaseCost, x.Currency));
            var written = 0;
            await foreach (var row in rows.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                var line = string.Join(';', new[] { CsvCell(row.AssetTag), CsvCell(row.Name), CsvCell(row.Status),
                    CsvCell(row.Category), CsvCell(row.Brand), CsvCell(row.Model), CsvCell(row.SerialNumber),
                    CsvCell(row.Location), CsvCell(row.EmployeeNumber), CsvCell(row.EmployeeName),
                    CsvCell(row.PurchaseDate?.ToString("yyyy-MM-dd")),
                    CsvCell(row.WarrantyExpiresAt?.ToString("yyyy-MM-dd")),
                    CsvCell(row.PurchaseCost?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)),
                    CsvCell(row.Currency) });
                await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
                if (++written % 500 == 0) await writer.FlushAsync(cancellationToken);
            }
            await writer.FlushAsync(cancellationToken);
        }).RequireAuthorization("asset.read");

        return endpoints;
    }

    private static IOrderedQueryable<Asset>? OrderAssets(IQueryable<Asset> query, AppDbContext db,
        Guid companyId, string? sortBy, string? sortDirection)
    {
        var sort = (sortBy ?? "tag").Trim().ToLowerInvariant();
        var direction = (sortDirection ?? "asc").Trim().ToLowerInvariant();
        var descending = direction == "desc";
        if (direction is not ("asc" or "desc")) return null;
        IOrderedQueryable<Asset>? ordered = (sort, descending) switch
        {
            ("tag", false) => query.OrderBy(x => x.AssetTag),
            ("tag", true) => query.OrderByDescending(x => x.AssetTag),
            ("name", false) => query.OrderBy(x => x.Name),
            ("name", true) => query.OrderByDescending(x => x.Name),
            ("category", false) => query.OrderBy(x => db.AssetCategories.Where(c => c.CompanyId == companyId && c.Id == x.CategoryId).Select(c => c.Name).FirstOrDefault()),
            ("category", true) => query.OrderByDescending(x => db.AssetCategories.Where(c => c.CompanyId == companyId && c.Id == x.CategoryId).Select(c => c.Name).FirstOrDefault()),
            ("brand", false) => query.OrderBy(x => db.AssetBrands.Where(b => b.CompanyId == companyId && b.Id == x.BrandId).Select(b => b.Name).FirstOrDefault()).ThenBy(x => db.AssetModels.Where(m => m.CompanyId == companyId && m.Id == x.ModelId).Select(m => m.Name).FirstOrDefault()),
            ("brand", true) => query.OrderByDescending(x => db.AssetBrands.Where(b => b.CompanyId == companyId && b.Id == x.BrandId).Select(b => b.Name).FirstOrDefault()).ThenByDescending(x => db.AssetModels.Where(m => m.CompanyId == companyId && m.Id == x.ModelId).Select(m => m.Name).FirstOrDefault()),
            ("employee", false) => query.OrderBy(x => db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.AssignedEmployeeId).Select(e => e.DisplayName).FirstOrDefault()),
            ("employee", true) => query.OrderByDescending(x => db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.AssignedEmployeeId).Select(e => e.DisplayName).FirstOrDefault()),
            ("location", false) => query.OrderBy(x => x.Location),
            ("location", true) => query.OrderByDescending(x => x.Location),
            ("warranty", false) => query.OrderBy(x => x.WarrantyExpiresAt),
            ("warranty", true) => query.OrderByDescending(x => x.WarrantyExpiresAt),
            ("status", false) => query.OrderBy(x => x.Status),
            ("status", true) => query.OrderByDescending(x => x.Status),
            ("updated", false) => query.OrderBy(x => x.UpdatedAt),
            ("updated", true) => query.OrderByDescending(x => x.UpdatedAt),
            _ => null
        };
        return ordered?.ThenBy(x => x.AssetTag).ThenBy(x => x.Id);
    }

    private static bool TryApplyWarrantyFilter(IQueryable<Asset> query, string? warranty, out IQueryable<Asset> filtered)
    {
        filtered = query;
        if (string.IsNullOrWhiteSpace(warranty)) return true;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var state = warranty.Trim().ToLowerInvariant();
        if (state is not ("expired" or "expiring" or "active" or "unspecified")) return false;
        filtered = state switch
        {
            "expired" => query.Where(x => x.WarrantyExpiresAt < today),
            "expiring" => query.Where(x => x.WarrantyExpiresAt >= today && x.WarrantyExpiresAt <= today.AddDays(30)),
            "active" => query.Where(x => x.WarrantyExpiresAt > today.AddDays(30)),
            "unspecified" => query.Where(x => x.WarrantyExpiresAt == null),
            _ => query
        };
        return true;
    }

    private static object AddCategory(AppDbContext db, Guid companyId, string name)
    {
        var row = new AssetCategory { CompanyId = companyId, Name = name };
        db.AssetCategories.Add(row);
        return new { row.Id, row.Name };
    }

    private static object AddBrand(AppDbContext db, Guid companyId, string name)
    {
        var row = new AssetBrand { CompanyId = companyId, Name = name };
        db.AssetBrands.Add(row);
        return new { row.Id, row.Name };
    }

    private static object AddModel(AppDbContext db, Guid companyId, string name, Guid? brandId)
    {
        var row = new AssetModel { CompanyId = companyId, Name = name, BrandId = brandId };
        db.AssetModels.Add(row);
        return new { row.Id, row.Name, row.BrandId };
    }

    private static async Task<List<CompanyLocation>> GetLocationChainAsync(AppDbContext db, Guid companyId,
        CompanyLocation location, CancellationToken cancellationToken)
    {
        var chain = new List<CompanyLocation> { location };
        var parentId = location.ParentLocationId;
        while (parentId is Guid id)
        {
            var parent = await db.CompanyLocations.AsNoTracking().SingleOrDefaultAsync(x =>
                x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (parent is null) break;
            chain.Insert(0, parent);
            parentId = parent.ParentLocationId;
            if (chain.Count > 3) break;
        }
        return chain;
    }

    internal static async Task<Guid?> ResolveLocationCatalogIdAsync(AppDbContext db, Guid companyId,
        string? locationPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(locationPath)) return null;
        var paths = await GetActiveLocationPathMapAsync(db, companyId, cancellationToken);
        return paths.TryGetValue(locationPath.Trim(), out var id) ? id : null;
    }

    internal static async Task<string?> GetDefaultLocationPathAsync(AppDbContext db, Guid companyId,
        CancellationToken cancellationToken)
    {
        var defaultId = await db.CompanyLocations.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive && x.IsDefault)
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken);
        if (defaultId is not Guid id) return null;
        var paths = await GetActiveLocationPathMapAsync(db, companyId, cancellationToken);
        var path = paths.FirstOrDefault(x => x.Value == id).Key;
        return path is { Length: <= 160 } ? path : null;
    }

    internal static async Task<Dictionary<string, Guid>> GetActiveLocationPathMapAsync(AppDbContext db,
        Guid companyId, CancellationToken cancellationToken)
    {
        var locations = await db.CompanyLocations.AsNoTracking().Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        var byId = locations.ToDictionary(x => x.Id);
        var paths = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var location in locations.Where(x => x.IsActive))
        {
            var names = new List<string>();
            var current = location;
            var valid = true;
            while (true)
            {
                if (!current.IsActive) { valid = false; break; }
                names.Add(current.Name);
                if (names.Count > 3) { valid = false; break; }
                if (current.ParentLocationId is not Guid parentId) break;
                if (!byId.TryGetValue(parentId, out var parentLocation)) { valid = false; break; }
                current = parentLocation;
            }
            if (valid) paths.TryAdd(string.Join(" / ", names.AsEnumerable().Reverse()), location.Id);
        }
        return paths;
    }

    private static bool TryCompanyId(ClaimsPrincipal user, out Guid companyId) =>
        Guid.TryParse(user.FindFirstValue("company_id"), out companyId);

    private static string NormalizeLocationName(string value) => value.Trim().ToUpperInvariant();

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool IsCurrency(string? value) => value is not null &&
        (value.Trim().ToUpperInvariant() is "TRY" or "USD" or "EUR");

    private static Guid? CurrentUserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

    private static string AssetAttachmentDirectory(IConfiguration configuration, Guid companyId, Guid assetId) =>
        Path.Combine(configuration["Storage:CompanyMediaPath"] ?? "/var/lib/envantertr/company-media",
            companyId.ToString("N"), "assets", assetId.ToString("N"));

    private static string CleanAttachmentFileName(string? fileName)
    {
        var leaf = Path.GetFileName((fileName ?? "").Replace('\\', '/'));
        var safe = new string(leaf.Where(character => !char.IsControl(character)).ToArray()).Trim();
        if (safe.Length == 0) safe = "dosya";
        return safe.Length <= 255 ? safe : safe[..255];
    }

    private static bool IsSafeAssetAttachmentFileName(string fileName)
    {
        var dot = fileName.LastIndexOf('.');
        return dot > 0 && Guid.TryParseExact(fileName[..dot], "N", out _) &&
            fileName[(dot + 1)..] is "png" or "jpg" or "webp" or "pdf";
    }

    private static (string ContentType, string Extension)? DetectAssetAttachment(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return ("image/png", "png");
        if (bytes.Length >= 3 && bytes[..3].SequenceEqual(new byte[] { 255, 216, 255 }))
            return ("image/jpeg", "jpg");
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8))
            return ("image/webp", "webp");
        if (bytes.Length >= 5 && bytes[..5].SequenceEqual("%PDF-"u8))
            return ("application/pdf", "pdf");
        return null;
    }

    private static Dictionary<string, string[]> ValidateAccessory(string? name, int minimumQuantity, int totalQuantity,
        decimal? unitCost, string? currency, string? category, string? modelNumber, string? itemNumber,
        string? supplier, string? location, string? notes)
    {
        var errors = new Dictionary<string, string[]>();
        void Add(string key, string message) => errors[key] = [message];
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) Add("name", "Ad zorunludur ve 200 karakteri aşamaz.");
        if (minimumQuantity is < 0 or > 100000) Add("minimumQuantity", "Minimum adet 0 ile 100.000 arasında olmalı.");
        if (totalQuantity is < 0 or > 100000) Add("quantity", "Toplam adet 0 ile 100.000 arasında olmalı.");
        if (unitCost < 0) Add("unitCost", "Birim maliyet negatif olamaz.");
        if (currency is null || currency.Trim().Length != 3 || currency.Any(c => !char.IsAsciiLetter(c))) Add("currency", "Para birimi üç harfli ISO kodu olmalı.");
        if (category?.Length > 120) Add("category", "Kategori 120 karakteri aşamaz.");
        if (modelNumber?.Length > 120) Add("modelNumber", "Model numarası 120 karakteri aşamaz.");
        if (itemNumber?.Length > 120) Add("itemNumber", "Ürün numarası 120 karakteri aşamaz.");
        if (supplier?.Length > 200) Add("supplier", "Tedarikçi 200 karakteri aşamaz.");
        if (location?.Length > 160) Add("location", "Konum 160 karakteri aşamaz.");
        if (notes?.Length > 2000) Add("notes", "Notlar 2.000 karakteri aşamaz.");
        return errors;
    }

    public sealed record CreateAccessoryRequest(string Name, string? Category, string? ModelNumber, string? ItemNumber,
        string? Supplier, string? Location, int MinimumQuantity, int Quantity, decimal? UnitCost,
        string Currency, DateOnly? PurchaseDate, string? Notes, bool IsRequestable);
    public sealed record UpdateAccessoryRequest(string Name, string? Category, string? ModelNumber, string? ItemNumber,
        string? Supplier, string? Location, int MinimumQuantity, decimal? UnitCost,
        string Currency, DateOnly? PurchaseDate, string? Notes, bool IsRequestable);
    public sealed record AccessoryStockRequest(int Quantity, string? Notes);
    public sealed record AccessoryCheckoutRequest(string TargetType, Guid? EmployeeId, Guid? AssetId, string? Location, int Quantity, string? Notes);
    public sealed record BulkAccessoryCheckoutRequest(string? TargetType, Guid? EmployeeId, Guid? AssetId, string? Location,
        BulkAccessoryCheckoutItem[]? Items, string? Notes);
    public sealed record BulkAccessoryCheckoutItem(Guid AccessoryId, int Quantity);
    public sealed record BulkAccessoryStockAdjustmentRequest(BulkAccessoryStockAdjustmentItem[]? Items, string? Notes);
    public sealed record BulkAccessoryStockAdjustmentItem(Guid AccessoryId, int Quantity);
    public sealed record AccessoryCheckinRequest(Guid AssignmentId, int Quantity, string? Notes);
    public sealed record BulkAccessoryCheckinRequest(BulkAccessoryCheckinItem[]? Items, string? Notes);
    public sealed record BulkAccessoryCheckinItem(Guid AssignmentId, int Quantity);
    public sealed record RecordAssetAuditRequest(DateOnly AuditDate, string? Notes, DateOnly? NextAuditOn = null);
    public sealed record TransferAssetRequest(string? TargetLocation, string? Notes);
    public sealed record StartMaintenanceRequest(string? MaintenanceType, string? Supplier, string? Description,
        DateOnly StartedOn, decimal? Cost, string Currency, string? Notes);
    public sealed record ScheduleMaintenanceRequest(string? MaintenanceType, string? Supplier, string? Description,
        DateOnly PlannedOn, decimal? Cost, string Currency, string? Notes);
    public sealed record CompleteMaintenanceRequest(DateOnly CompletedOn, string? Notes);
    public sealed record SelectedAssetLabelsRequest(Guid[]? AssetIds);
    private sealed record LocationDirectoryEntry(string Name, int AssetCount, int AssignedCount, int AvailableCount,
        int MaintenanceCount, int AccessoryCount, int AccessoryAssignmentCount, int ConsumableCount, int ComponentCount,
        Guid? LocationId, Guid? ParentLocationId, int Depth, bool IsCatalogEntry, bool IsActive, bool IsDefault,
        Guid? ManagerEmployeeId, string? ManagerName);
    private sealed record CalendarEntry(string Kind, DateOnly Date, Guid? AssetId, Guid RecordId,
        string Title, string? Subtitle, string Status, bool? AutoRenew);

    private sealed record AssetExportRow(string AssetTag, string Name, string Status, string? Category,
        string? Brand, string? Model, string? SerialNumber, string? Location, string? EmployeeNumber,
        string? EmployeeName, DateOnly? PurchaseDate, DateOnly? WarrantyExpiresAt, decimal? PurchaseCost, string Currency);

    private static string CsvCell(string? value)
    {
        var safe = value?.Trim() ?? "";
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@' or '\t' or '\r') safe = "'" + safe;
        return "\"" + safe.Replace("\"", "\"\"") + "\"";
    }
}
