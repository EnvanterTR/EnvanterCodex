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

        api.MapGet("/assets", async ([FromQuery] string? q, [FromQuery] string? status,
            [FromQuery] Guid? categoryId, [FromQuery] Guid? brandId, [FromQuery] Guid? modelId,
            [FromQuery] Guid? employeeId, [FromQuery] string? location,
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
                    x.AssignedEmployeeId, x.UpdatedAt)).ToListAsync(cancellationToken);
            return Results.Ok(new { items, page, pageSize, total });
        }).RequireAuthorization("asset.read");

        // Locations are currently free-text values on assets. The directory therefore
        // summarizes exact stored location labels and never implies a managed hierarchy.
        api.MapGet("/locations", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var locations = await db.Assets.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.Location != null && x.Location.Trim() != "")
                .GroupBy(x => x.Location!.Trim())
                .OrderBy(group => group.Key)
                .Select(group => new
                {
                    name = group.Key,
                    assetCount = group.Count(),
                    assignedCount = group.Count(asset => asset.AssignedEmployeeId != null),
                    availableCount = group.Count(asset => asset.Status == AssetStatuses.Available),
                    maintenanceCount = group.Count(asset => asset.Status == AssetStatuses.Maintenance)
                })
                .ToListAsync(cancellationToken);
            return Results.Ok(new { items = locations, total = locations.Count });
        }).RequireAuthorization("asset.read");

        api.MapGet("/reports/assets", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var companyAssets = db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId);
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
                totalAssets,
                pricedAssetCount,
                withoutPurchaseCostCount = totalAssets - pricedAssetCount,
                statusCounts,
                categories = categoryRows,
                purchaseCostByCurrency = costRowsForResponse
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
                    x.CategoryId, x.BrandId, x.ModelId, x.AssignedEmployeeId,
                    x.PurchaseDate, x.PurchaseCost, x.Currency, x.CreatedAt, x.UpdatedAt
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
                    employeeName = db.Employees
                        .Where(employee => employee.CompanyId == companyId && employee.Id == x.EmployeeId)
                        .Select(employee => employee.DisplayName)
                        .FirstOrDefault(),
                    x.OccurredAt
                })
                .ToListAsync(cancellationToken);
            return Results.Ok(new { asset, references, history });
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
            var location = Clean(request.Location);
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

            var asset = new Asset
            {
                CompanyId = companyId, AssetTag = tag, Name = name, CategoryId = request.CategoryId,
                BrandId = request.BrandId, ModelId = request.ModelId,
                SerialNumber = serial, Location = location,
                PurchaseDate = request.PurchaseDate, PurchaseCost = request.PurchaseCost,
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
                null, asset.AssignedEmployeeId, asset.UpdatedAt));
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

            var changed = new List<string>();
            if (current.AssetTag != tag) changed.Add("etiket");
            if (current.Name != name) changed.Add("ad");
            if (current.CategoryId != request.CategoryId) changed.Add("kategori");
            if (current.BrandId != request.BrandId) changed.Add("marka");
            if (current.ModelId != request.ModelId) changed.Add("model");
            if (current.SerialNumber != serial) changed.Add("seri numarası");
            if (current.Location != location) changed.Add("konum");
            if (current.PurchaseDate != request.PurchaseDate) changed.Add("alış tarihi");
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
                        .SetProperty(x => x.PurchaseDate, request.PurchaseDate)
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
            [FromQuery] Guid? employeeId, [FromQuery] string? location,
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
            await writer.WriteLineAsync("Etiket;Ad;Durum;Kategori;Marka;Model;Seri No;Konum;Personel No;Çalışan;Alış Tarihi;Alış Bedeli;Para Birimi".AsMemory(), cancellationToken);
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
                x.PurchaseDate, x.PurchaseCost, x.Currency));
            var written = 0;
            await foreach (var row in rows.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                var line = string.Join(';', new[] { CsvCell(row.AssetTag), CsvCell(row.Name), CsvCell(row.Status),
                    CsvCell(row.Category), CsvCell(row.Brand), CsvCell(row.Model), CsvCell(row.SerialNumber),
                    CsvCell(row.Location), CsvCell(row.EmployeeNumber), CsvCell(row.EmployeeName),
                    CsvCell(row.PurchaseDate?.ToString("yyyy-MM-dd")),
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
            ("status", false) => query.OrderBy(x => x.Status),
            ("status", true) => query.OrderByDescending(x => x.Status),
            ("updated", false) => query.OrderBy(x => x.UpdatedAt),
            ("updated", true) => query.OrderByDescending(x => x.UpdatedAt),
            _ => null
        };
        return ordered?.ThenBy(x => x.AssetTag).ThenBy(x => x.Id);
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

    private static bool TryCompanyId(ClaimsPrincipal user, out Guid companyId) =>
        Guid.TryParse(user.FindFirstValue("company_id"), out companyId);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record AssetExportRow(string AssetTag, string Name, string Status, string? Category,
        string? Brand, string? Model, string? SerialNumber, string? Location, string? EmployeeNumber,
        string? EmployeeName, DateOnly? PurchaseDate, decimal? PurchaseCost, string Currency);

    private static string CsvCell(string? value)
    {
        var safe = value?.Trim() ?? "";
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@' or '\t' or '\r') safe = "'" + safe;
        return "\"" + safe.Replace("\"", "\"\"") + "\"";
    }
}
