using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AssetContractEndpoints
{
    public static RouteGroupBuilder MapAssetContractEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/contracts", async ([FromQuery] string? q, [FromQuery] string? status,
            [FromQuery] bool includeArchived, [FromQuery] int page, [FromQuery] int pageSize,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, ct);
            var allowedLocations = employeeScope.AllowedLocations;
            if (q?.Trim().Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama en fazla 120 karakter olabilir."] });
            if (status is not (null or "" or "all" or "active" or "expired" or "expiring" or "archived"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Durum active, expired, expiring, archived veya all olmalıdır."] });
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 100);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var query = db.AssetContracts.AsNoTracking().Where(x => x.CompanyId == companyId)
                .InEmployeeScope(db, companyId, employeeScope);
            if (!includeArchived && status != "archived") query = query.Where(x => x.IsActive);
            if (status == "archived") query = query.Where(x => !x.IsActive);
            else if (status == "expired") query = query.Where(x => x.IsActive && x.EndDate != null && x.EndDate < today);
            else if (status == "expiring") query = query.Where(x => x.IsActive && x.EndDate >= today && x.EndDate <= today.AddDays(30));
            else if (status == "active") query = query.Where(x => x.IsActive && (x.EndDate == null || x.EndDate >= today));
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.Name.Contains(term) || (x.ContractNumber != null && x.ContractNumber.Contains(term)) ||
                    (x.ContractType != null && x.ContractType.Contains(term)) || (x.Supplier != null && x.Supplier.Contains(term)) ||
                    (x.Notes != null && x.Notes.Contains(term)));
            }
            var total = await query.CountAsync(ct);
            var items = await query.OrderBy(x => x.EndDate == null).ThenBy(x => x.EndDate).ThenBy(x => x.Name).ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
                {
                    x.Id, x.Name, x.ContractNumber, x.ContractType, x.Supplier, x.SupplierId, x.StartDate, x.EndDate,
                    x.Cost, x.Currency, x.Notes, x.IsActive, x.CreatedAt, x.UpdatedAt,
                    status = !x.IsActive ? "archived" : x.EndDate != null && x.EndDate < today ? "expired" :
                        x.EndDate != null && x.EndDate <= today.AddDays(30) ? "expiring" : "active",
                    assetCount = db.AssetContractAssets.Count(link => link.CompanyId == companyId && link.ContractId == x.Id),
                    licenseCount = db.AssetContractLicenses.Count(link => link.CompanyId == companyId && link.ContractId == x.Id),
                    attachmentCount = db.AssetContractAttachments.Count(file => file.CompanyId == companyId && file.ContractId == x.Id)
                }).ToListAsync(ct);
            return Results.Ok(new { items, page, pageSize, total });
        }).RequireAuthorization("asset.read");

        api.MapGet("/contracts/lookups", async ([FromQuery] string? q, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, ct);
            var allowedLocations = employeeScope.AllowedLocations;
            if (q?.Trim().Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama en fazla 120 karakter olabilir."] });
            var assets = db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId && x.DeletedAt == null)
                .AssetsInLocationScope(companyId, allowedLocations);
            var licenses = db.SoftwareLicenses.AsNoTracking().Where(x => x.CompanyId == companyId)
                .LicensesInEmployeeScope(db, companyId, employeeScope);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                assets = assets.Where(x => x.Name.Contains(term) || x.AssetTag.Contains(term) || (x.SerialNumber != null && x.SerialNumber.Contains(term)));
                licenses = licenses.Where(x => x.Name.Contains(term) || (x.LicenseReference != null && x.LicenseReference.Contains(term)));
            }
            var assetItems = await assets.OrderBy(x => x.AssetTag).Take(100).Select(x => new { x.Id, x.AssetTag, x.Name, x.SerialNumber }).ToListAsync(ct);
            var licenseItems = await licenses.OrderBy(x => x.Name).Take(100).Select(x => new { x.Id, x.Name, x.LicenseReference, x.ExpirationDate }).ToListAsync(ct);
            var suppliers = await db.SupplierCatalogEntries.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive)
                .OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).Take(200).ToListAsync(ct);
            return Results.Ok(new { assets = assetItems, licenses = licenseItems, suppliers });
        }).RequireAuthorization("asset.read");

        api.MapGet("/contracts/export.csv", async ([FromQuery] string? q, [FromQuery] string? status,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, ct);
            var allowedLocations = employeeScope.AllowedLocations;
            if (q?.Trim().Length > 120 || status is not (null or "" or "all" or "active" or "expired" or "expiring" or "archived"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["filter"] = ["Sözleşme arama veya durum filtresi geçersiz."] });
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var query = db.AssetContracts.AsNoTracking().Where(x => x.CompanyId == companyId)
                .InEmployeeScope(db, companyId, employeeScope);
            if (status == "archived") query = query.Where(x => !x.IsActive);
            else if (status is "active" or "expired" or "expiring")
            {
                query = query.Where(x => x.IsActive);
                if (status == "expired") query = query.Where(x => x.EndDate != null && x.EndDate < today);
                else if (status == "expiring") query = query.Where(x => x.EndDate >= today && x.EndDate <= today.AddDays(30));
                else if (status == "active") query = query.Where(x => x.EndDate == null || x.EndDate >= today);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.Name.Contains(term) || (x.ContractNumber != null && x.ContractNumber.Contains(term)) ||
                    (x.ContractType != null && x.ContractType.Contains(term)) || (x.Supplier != null && x.Supplier.Contains(term)));
            }
            var rows = await query.OrderBy(x => x.EndDate == null).ThenBy(x => x.EndDate).ThenBy(x => x.Name).Take(50_000)
                .Select(x => new
                {
                    x.Name, x.ContractNumber, x.ContractType, x.Supplier, x.StartDate, x.EndDate,
                    x.Cost, x.Currency, x.IsActive, x.Notes,
                    assetCount = db.AssetContractAssets.Count(link => link.CompanyId == companyId && link.ContractId == x.Id),
                    licenseCount = db.AssetContractLicenses.Count(link => link.CompanyId == companyId && link.ContractId == x.Id)
                }).ToListAsync(ct);
            var builder = new StringBuilder("Sözleşme Adı,Sözleşme No,Tür,Tedarikçi,Başlangıç,Bitiş,Maliyet,Para Birimi,Durum,Varlık Sayısı,Lisans Sayısı,Notlar\r\n");
            foreach (var row in rows)
            {
                var values = new[] { row.Name, row.ContractNumber, row.ContractType, row.Supplier,
                    row.StartDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), row.EndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    row.Cost?.ToString("0.00", CultureInfo.InvariantCulture), row.Currency,
                    !row.IsActive ? "Arşiv" : row.EndDate is DateOnly end && end < today ? "Süresi dolmuş" :
                        row.EndDate is DateOnly expiring && expiring <= today.AddDays(30) ? "Süresi yaklaşıyor" : "Etkin",
                    row.assetCount.ToString(CultureInfo.InvariantCulture), row.licenseCount.ToString(CultureInfo.InvariantCulture), row.Notes };
                builder.AppendJoin(',', values.Select(Csv)); builder.Append("\r\n");
            }
            return Results.File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray(), "text/csv; charset=utf-8", "sozlesmeler.csv");
        }).RequireAuthorization("asset.read");

        api.MapGet("/contracts/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, ct);
            var allowedLocations = employeeScope.AllowedLocations;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var contract = await db.AssetContracts.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .InEmployeeScope(db, companyId, employeeScope)
                .Select(x => new { x.Id, x.Name, x.ContractNumber, x.ContractType, x.Supplier, x.SupplierId, x.StartDate,
                    x.EndDate, x.Cost, x.Currency, x.Notes, x.IsActive, x.CreatedAt, x.UpdatedAt,
                    status = !x.IsActive ? "archived" : x.EndDate != null && x.EndDate < today ? "expired" :
                        x.EndDate != null && x.EndDate <= today.AddDays(30) ? "expiring" : "active" }).SingleOrDefaultAsync(ct);
            if (contract is null) return Results.NotFound();
            var visibleAssets = db.Assets.AsNoTracking().AssetsInLocationScope(companyId, allowedLocations);
            var assets = await (from link in db.AssetContractAssets.AsNoTracking().Where(x => x.CompanyId == companyId && x.ContractId == id)
                                join asset in visibleAssets on new { link.CompanyId, Id = link.AssetId } equals new { asset.CompanyId, asset.Id }
                                orderby asset.AssetTag
                                select new { asset.Id, asset.AssetTag, asset.Name, asset.SerialNumber }).ToListAsync(ct);
            var visibleLicenses = db.SoftwareLicenses.AsNoTracking().LicensesInEmployeeScope(db, companyId, employeeScope);
            var licenses = await (from link in db.AssetContractLicenses.AsNoTracking().Where(x => x.CompanyId == companyId && x.ContractId == id)
                                  join license in visibleLicenses on new { link.CompanyId, Id = link.LicenseId } equals new { license.CompanyId, license.Id }
                                  orderby license.Name
                                  select new { license.Id, license.Name, license.LicenseReference, license.ExpirationDate }).ToListAsync(ct);
            return Results.Ok(new { contract, assets, licenses });
        }).RequireAuthorization("asset.read");

        api.MapPost("/contracts", async (ContractRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, ct);
            var allowedLocations = employeeScope.AllowedLocations;
            var error = await ValidateAsync(request, companyId, db, employeeScope, ct);
            if (error is not null) return Results.ValidationProblem(error);
            var supplier = request.SupplierId is Guid supplierId
                ? await db.SupplierCatalogEntries.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == supplierId).Select(x => x.Name).SingleAsync(ct)
                : Clean(request.Supplier);
            var item = new AssetContract
            {
                CompanyId = companyId, Name = request.Name!.Trim(), ContractNumber = Clean(request.ContractNumber),
                ContractType = Clean(request.ContractType), Supplier = supplier, SupplierId = request.SupplierId,
                StartDate = request.StartDate, EndDate = request.EndDate, Cost = request.Cost,
                Currency = request.Currency!.Trim().ToUpperInvariant(), Notes = Clean(request.Notes), CreatedByUserId = actorId
            };
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            AddLinks(db, item, request.AssetIds, request.LicenseIds);
            db.AssetContracts.Add(item);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetContract", TargetId = item.Id,
                Action = "contract.created", Details = item.Name });
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
            return Results.Created($"/api/contracts/{item.Id}", new { item.Id, item.Name });
        }).RequireAuthorization("asset.write");

        api.MapPut("/contracts/{id:guid}", async (Guid id, ContractRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, ct);
            var allowedLocations = employeeScope.AllowedLocations;
            if (!await AssetContractLocationAccess.CanAccessAsync(db, companyId, id, employeeScope, ct)) return Results.NotFound();
            var item = await db.AssetContracts.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (item is null) return Results.NotFound();
            if (!item.IsActive) return Results.Conflict(new { message = "Arşivlenmiş sözleşmeyi değiştirmeden önce geri yükleyin." });
            var error = await ValidateAsync(request, companyId, db, employeeScope, ct);
            if (error is not null) return Results.ValidationProblem(error);
            var supplier = request.SupplierId is Guid supplierId
                ? await db.SupplierCatalogEntries.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == supplierId).Select(x => x.Name).SingleAsync(ct)
                : Clean(request.Supplier);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            item.Name = request.Name!.Trim(); item.ContractNumber = Clean(request.ContractNumber); item.ContractType = Clean(request.ContractType);
            item.Supplier = supplier; item.SupplierId = request.SupplierId; item.StartDate = request.StartDate; item.EndDate = request.EndDate;
            item.Cost = request.Cost; item.Currency = request.Currency!.Trim().ToUpperInvariant(); item.Notes = Clean(request.Notes); item.UpdatedAt = DateTimeOffset.UtcNow;
            await db.AssetContractAssets.Where(x => x.CompanyId == companyId && x.ContractId == id).ExecuteDeleteAsync(ct);
            await db.AssetContractLicenses.Where(x => x.CompanyId == companyId && x.ContractId == id).ExecuteDeleteAsync(ct);
            AddLinks(db, item, request.AssetIds, request.LicenseIds);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetContract", TargetId = id,
                Action = "contract.updated", Details = $"{item.Name}; varlık/lisans bağlantıları güncellendi." });
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
            return Results.Ok(new { item.Id, item.Name, item.UpdatedAt });
        }).RequireAuthorization("asset.write");

        api.MapPost("/contracts/{id:guid}/archive", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, ct);
            var allowedLocations = employeeScope.AllowedLocations;
            if (!await AssetContractLocationAccess.CanAccessAsync(db, companyId, id, employeeScope, ct)) return Results.NotFound();
            var item = await db.AssetContracts.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (item is null) return Results.NotFound();
            if (item.IsActive)
            {
                item.IsActive = false; item.UpdatedAt = DateTimeOffset.UtcNow;
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetContract", TargetId = id,
                    Action = "contract.archived", Details = item.Name });
                await db.SaveChangesAsync(ct);
            }
            return Results.NoContent();
        }).RequireAuthorization("asset.write");

        api.MapPost("/contracts/{id:guid}/restore", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, ct);
            var allowedLocations = employeeScope.AllowedLocations;
            if (!await AssetContractLocationAccess.CanAccessAsync(db, companyId, id, employeeScope, ct)) return Results.NotFound();
            var item = await db.AssetContracts.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (item is null) return Results.NotFound();
            if (!item.IsActive)
            {
                item.IsActive = true; item.UpdatedAt = DateTimeOffset.UtcNow;
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetContract", TargetId = id,
                    Action = "contract.restored", Details = item.Name });
                await db.SaveChangesAsync(ct);
            }
            return Results.NoContent();
        }).RequireAuthorization("asset.write");
        return api;
    }

    private static async Task<Dictionary<string, string[]>?> ValidateAsync(ContractRequest request, Guid companyId, AppDbContext db,
        EmployeeAccessScope employeeScope, CancellationToken ct)
    {
        var allowedLocations = employeeScope.AllowedLocations;
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200) errors["name"] = ["Sözleşme adı zorunludur ve en fazla 200 karakter olabilir."];
        if (Clean(request.ContractNumber)?.Length > 120) errors["contractNumber"] = ["Sözleşme numarası en fazla 120 karakter olabilir."];
        if (Clean(request.ContractType)?.Length > 120) errors["contractType"] = ["Sözleşme türü en fazla 120 karakter olabilir."];
        if (Clean(request.Supplier)?.Length > 200) errors["supplier"] = ["Tedarikçi en fazla 200 karakter olabilir."];
        if (request.Notes?.Length > 4000) errors["notes"] = ["Notlar en fazla 4.000 karakter olabilir."];
        if (request.Cost is < 0 or > 1_000_000_000m) errors["cost"] = ["Maliyet 0 ile 1.000.000.000 arasında olmalıdır."];
        if (request.Currency is null || request.Currency.Trim().Length != 3 || !request.Currency.Trim().All(char.IsAsciiLetter)) errors["currency"] = ["Para birimi üç harfli kod olmalıdır."];
        if (request.StartDate is DateOnly start && request.EndDate is DateOnly end && end < start) errors["endDate"] = ["Bitiş tarihi başlangıç tarihinden önce olamaz."];
        if (request.SupplierId is Guid supplierId && !await db.SupplierCatalogEntries.AnyAsync(x => x.CompanyId == companyId && x.Id == supplierId, ct))
            errors["supplierId"] = ["Seçilen tedarikçi bu şirkette bulunamadı."];
        var assetIds = request.AssetIds ?? [];
        if (assetIds.Count > 2000 || assetIds.Distinct().Count() != assetIds.Count) errors["assetIds"] = ["En fazla 2.000 farklı varlık bağlanabilir."];
        else if (assetIds.Count > 0 && await db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId && x.DeletedAt == null)
                     .AssetsInLocationScope(companyId, allowedLocations).CountAsync(x => assetIds.Contains(x.Id), ct) != assetIds.Count)
            errors["assetIds"] = ["Seçilen varlıklardan biri bu şirkette yok, arşivlenmiş veya silinmiş."];
        var licenseIds = request.LicenseIds ?? [];
        if (licenseIds.Count > 2000 || licenseIds.Distinct().Count() != licenseIds.Count) errors["licenseIds"] = ["En fazla 2.000 farklı lisans bağlanabilir."];
        else if (licenseIds.Count > 0 && await db.SoftwareLicenses.AsNoTracking().Where(x => x.CompanyId == companyId)
                     .LicensesInEmployeeScope(db, companyId, employeeScope).CountAsync(x => licenseIds.Contains(x.Id), ct) != licenseIds.Count)
            errors["licenseIds"] = ["Seçilen lisanslardan biri bu şirkette bulunamadı."];
        if (employeeScope.IsRestricted && assetIds.Count + licenseIds.Count == 0)
            errors["assetIds"] = ["Konumla sınırlı kullanıcılar yalnız erişebildikleri varlık veya lisanslara bağlı sözleşme oluşturabilir."];
        return errors.Count == 0 ? null : errors;
    }

    private static void AddLinks(AppDbContext db, AssetContract contract, IReadOnlyCollection<Guid>? assetIds, IReadOnlyCollection<Guid>? licenseIds)
    {
        foreach (var id in assetIds ?? []) db.AssetContractAssets.Add(new AssetContractAsset { CompanyId = contract.CompanyId, ContractId = contract.Id, AssetId = id });
        foreach (var id in licenseIds ?? []) db.AssetContractLicenses.Add(new AssetContractLicense { CompanyId = contract.CompanyId, ContractId = contract.Id, LicenseId = id });
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        var safe = value.TrimStart() is { Length: > 0 } trimmed && trimmed[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + value : value;
        return "\"" + safe.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool Company(ClaimsPrincipal user, out Guid companyId) => Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
    private static bool Actor(ClaimsPrincipal user, out Guid userId) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private sealed record ContractRequest(string? Name, string? ContractNumber, string? ContractType, Guid? SupplierId,
        string? Supplier, DateOnly? StartDate, DateOnly? EndDate, decimal? Cost, string? Currency, string? Notes,
        List<Guid>? AssetIds, List<Guid>? LicenseIds);
}
