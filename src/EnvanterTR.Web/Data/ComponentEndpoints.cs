using System.Security.Claims;
using System.Globalization;
using System.Text;
using System.Data;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class ComponentEndpoints
{
    private const int CsvImportMaxBytes = 32 * 1024 * 1024;
    private const int CsvImportMaxRows = 5000;

    public static RouteGroupBuilder MapComponentEndpoints(this RouteGroupBuilder api)
    {
        api.MapComponentImportMappingProfileEndpoints();
        api.MapPost("/components/import/preview.csv", (HttpContext context, AppDbContext db, IAntiforgery antiforgery, CancellationToken ct) =>
            ImportCsvAsync(context, db, antiforgery, false, ct)).RequireAuthorization("asset.write")
            .WithMetadata(new RequestSizeLimitAttribute(CsvImportMaxBytes));
        api.MapPost("/components/import/preview.xlsx", (HttpContext context, AppDbContext db, IAntiforgery antiforgery, CancellationToken ct) =>
            ImportCsvAsync(context, db, antiforgery, false, ct)).RequireAuthorization("asset.write")
            .WithMetadata(new RequestSizeLimitAttribute(CsvImportMaxBytes));
        api.MapPost("/components/import.csv", (HttpContext context, AppDbContext db, IAntiforgery antiforgery, CancellationToken ct) =>
            ImportCsvAsync(context, db, antiforgery, true, ct)).RequireAuthorization("asset.write")
            .WithMetadata(new RequestSizeLimitAttribute(CsvImportMaxBytes));
        api.MapPost("/components/import.xlsx", (HttpContext context, AppDbContext db, IAntiforgery antiforgery, CancellationToken ct) =>
            ImportCsvAsync(context, db, antiforgery, true, ct)).RequireAuthorization("asset.write")
            .WithMetadata(new RequestSizeLimitAttribute(CsvImportMaxBytes));

        api.MapGet("/components", async ([FromQuery]string? q,[FromQuery]string? stock,ClaimsPrincipal user,AppDbContext db,CancellationToken ct,[FromQuery]int page=1,[FromQuery]int pageSize=25)=>
        {
            if(!Tenant(user,out var companyId))return Results.Forbid(); page=Math.Max(1,page);pageSize=Math.Clamp(pageSize==0?25:pageSize,1,200);
            var query=db.InventoryComponents.AsNoTracking().Where(x=>x.CompanyId==companyId);
            if(!string.IsNullOrWhiteSpace(q)){var term=q.Trim();if(term.Length>120)return Results.ValidationProblem(new Dictionary<string,string[]>{{"q",["Arama 120 karakteri aşamaz."]}});query=query.Where(x=>x.Name.Contains(term)||(x.Category!=null&&x.Category.Contains(term))||(x.ModelNumber!=null&&x.ModelNumber.Contains(term))||(x.SerialNumber!=null&&x.SerialNumber.Contains(term))||(x.ItemNumber!=null&&x.ItemNumber.Contains(term))||(x.Supplier!=null&&x.Supplier.Contains(term))||(x.Location!=null&&x.Location.Contains(term)));}
            if(stock=="low")query=query.Where(x=>x.TotalQuantity-(db.ComponentAllocations.Where(a=>a.CompanyId==companyId&&a.ComponentId==x.Id).Sum(a=>(int?)(a.Quantity-a.RemovedQuantity))??0)<=x.MinimumQuantity);
            if(stock=="available")query=query.Where(x=>x.TotalQuantity>(db.ComponentAllocations.Where(a=>a.CompanyId==companyId&&a.ComponentId==x.Id).Sum(a=>(int?)(a.Quantity-a.RemovedQuantity))??0));
            var total=await query.CountAsync(ct);var items=await query.OrderBy(x=>x.Name).ThenBy(x=>x.Id).Skip((page-1)*pageSize).Take(pageSize).Select(x=>new{x.Id,x.Name,x.Category,x.ModelNumber,x.ItemNumber,x.SerialNumber,x.Supplier,x.Location,x.MinimumQuantity,x.TotalQuantity,x.UnitCost,x.Currency,x.PurchaseDate,x.UpdatedAt,allocated=db.ComponentAllocations.Where(a=>a.CompanyId==companyId&&a.ComponentId==x.Id).Sum(a=>(int?)(a.Quantity-a.RemovedQuantity))??0}).ToListAsync(ct);
            return Results.Ok(new{items,page,pageSize,total});
        }).RequireAuthorization("asset.read");
        api.MapGet("/components/export.csv",async([FromQuery]string? q,[FromQuery]string? stock,ClaimsPrincipal user,AppDbContext db,CancellationToken ct)=>
        {
            if(!Tenant(user,out var companyId))return Results.Forbid();var query=db.InventoryComponents.AsNoTracking().Where(x=>x.CompanyId==companyId);
            if(!string.IsNullOrWhiteSpace(q)){var term=q.Trim();if(term.Length>120)return Results.ValidationProblem(new Dictionary<string,string[]>{{"q",["Arama 120 karakteri aşamaz."]}});query=query.Where(x=>x.Name.Contains(term)||(x.Category!=null&&x.Category.Contains(term))||(x.ModelNumber!=null&&x.ModelNumber.Contains(term))||(x.SerialNumber!=null&&x.SerialNumber.Contains(term))||(x.ItemNumber!=null&&x.ItemNumber.Contains(term))||(x.Supplier!=null&&x.Supplier.Contains(term))||(x.Location!=null&&x.Location.Contains(term)));}
            if(stock=="low")query=query.Where(x=>x.TotalQuantity-(db.ComponentAllocations.Where(a=>a.CompanyId==companyId&&a.ComponentId==x.Id).Sum(a=>(int?)(a.Quantity-a.RemovedQuantity))??0)<=x.MinimumQuantity);if(stock=="available")query=query.Where(x=>x.TotalQuantity>(db.ComponentAllocations.Where(a=>a.CompanyId==companyId&&a.ComponentId==x.Id).Sum(a=>(int?)(a.Quantity-a.RemovedQuantity))??0));
            if(await query.CountAsync(ct)>10000)return Results.ValidationProblem(new Dictionary<string,string[]>{{"export",["Dışa aktarım en fazla 10.000 satır içerir; filtreyi daraltın."]}});
            var rows=await query.OrderBy(x=>x.Name).Select(x=>new{x.Name,x.Category,x.ModelNumber,x.ItemNumber,x.SerialNumber,x.Supplier,x.Location,x.TotalQuantity,x.MinimumQuantity,x.UnitCost,x.Currency,Allocated=db.ComponentAllocations.Where(a=>a.CompanyId==companyId&&a.ComponentId==x.Id).Sum(a=>(int?)(a.Quantity-a.RemovedQuantity))??0}).ToListAsync(ct);
            var csv=new StringBuilder("\uFEFF\"Bileşen\",\"Kategori\",\"Model\",\"Ürün no\",\"Seri no\",\"Tedarikçi\",\"Konum\",\"Toplam\",\"Atanmış\",\"Kullanılabilir\",\"Minimum\",\"Birim maliyeti\",\"Para birimi\"\r\n");foreach(var x in rows){var cells=new[]{x.Name,x.Category,x.ModelNumber,x.ItemNumber,x.SerialNumber,x.Supplier,x.Location,x.TotalQuantity.ToString(CultureInfo.InvariantCulture),x.Allocated.ToString(CultureInfo.InvariantCulture),(x.TotalQuantity-x.Allocated).ToString(CultureInfo.InvariantCulture),x.MinimumQuantity.ToString(CultureInfo.InvariantCulture),x.UnitCost?.ToString("0.00",CultureInfo.InvariantCulture),x.Currency};csv.AppendJoin(',',cells.Select(CsvCell)).Append("\r\n");}
            return Results.File(Encoding.UTF8.GetBytes(csv.ToString()),"text/csv; charset=utf-8",$"bilesen-stoklari-{DateTime.UtcNow:yyyyMMdd}.csv");
        }).RequireAuthorization("asset.read");
        api.MapGet("/reports/components", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            var components = db.InventoryComponents.AsNoTracking().Where(x => x.CompanyId == companyId);
            var allocations = db.ComponentAllocations.AsNoTracking().Where(x => x.CompanyId == companyId);
            var rows = await components
                .GroupBy(x => x.Category ?? "Kategorisiz")
                .Select(group => new
                {
                    category = group.Key,
                    componentCount = group.Count(),
                    totalQuantity = group.Sum(x => x.TotalQuantity),
                    allocatedQuantity = group.Sum(x => allocations.Where(a => a.ComponentId == x.Id)
                        .Sum(a => (int?)(a.Quantity - a.RemovedQuantity)) ?? 0),
                    lowStockCount = group.Count(x => x.TotalQuantity -
                        (allocations.Where(a => a.ComponentId == x.Id).Sum(a => (int?)(a.Quantity - a.RemovedQuantity)) ?? 0)
                        <= x.MinimumQuantity)
                })
                .OrderBy(x => x.category).ToListAsync(ct);
            var totalComponents = await components.CountAsync(ct);
            var totalQuantity = rows.Sum(x => x.totalQuantity);
            var allocatedQuantity = rows.Sum(x => x.allocatedQuantity);
            return Results.Ok(new
            {
                componentCount = totalComponents,
                categoryCount = rows.Count,
                totalQuantity,
                allocatedQuantity,
                availableQuantity = totalQuantity - allocatedQuantity,
                lowStockCount = rows.Sum(x => x.lowStockCount),
                categories = rows
            });
        }).RequireAuthorization("asset.read");
        api.MapGet("/components/{id:guid}",async(Guid id,ClaimsPrincipal user,AppDbContext db,CancellationToken ct)=>
        {
            if(!Tenant(user,out var companyId))return Results.Forbid();var item=await db.InventoryComponents.AsNoTracking().Where(x=>x.CompanyId==companyId&&x.Id==id).Select(x=>new{x.Id,x.Name,x.Category,x.ModelNumber,x.ItemNumber,x.SerialNumber,x.Supplier,x.Location,x.MinimumQuantity,x.TotalQuantity,x.UnitCost,x.Currency,x.PurchaseDate,x.Notes,x.UpdatedAt,allocated=db.ComponentAllocations.Where(a=>a.CompanyId==companyId&&a.ComponentId==x.Id).Sum(a=>(int?)(a.Quantity-a.RemovedQuantity))??0}).SingleOrDefaultAsync(ct);if(item is null)return Results.NotFound();
            var allocations=await db.ComponentAllocations.AsNoTracking().Where(a=>a.CompanyId==companyId&&a.ComponentId==id).OrderBy(a=>a.RemovedQuantity>=a.Quantity).ThenByDescending(a=>a.AllocatedAt).Take(200).Select(a=>new{a.Id,a.AssetId,asset=db.Assets.Where(x=>x.CompanyId==companyId&&x.Id==a.AssetId).Select(x=>x.AssetTag+" · "+x.Name).FirstOrDefault(),a.Quantity,a.RemovedQuantity,remaining=a.Quantity-a.RemovedQuantity,a.AllocatedAt,a.LastRemovedAt,a.Notes}).ToListAsync(ct);
            var movements=await db.ComponentMovements.AsNoTracking().Where(m=>m.CompanyId==companyId&&m.ComponentId==id).OrderByDescending(m=>m.OccurredAt).Take(100).Select(m=>new{m.MovementType,m.Quantity,m.Notes,m.OccurredAt,asset=db.Assets.Where(a=>a.CompanyId==companyId&&a.Id==m.AssetId).Select(a=>a.AssetTag+" · "+a.Name).FirstOrDefault()}).ToListAsync(ct);return Results.Ok(new{item,allocations,movements});
        }).RequireAuthorization("asset.read");
        api.MapPost("/components",async(CreateRequest r,ClaimsPrincipal user,AppDbContext db,CancellationToken ct)=>
        {
            if(!Tenant(user,out var companyId))return Results.Forbid();var err=Validate(r.Name,r.MinimumQuantity,r.Quantity,r.UnitCost,r.Currency,r.Category,r.ModelNumber,r.ItemNumber,r.SerialNumber,r.Supplier,r.Location,r.Notes);if(err.Count>0)return Results.ValidationProblem(err);
            var location=Clean(r.Location)??await AssetEndpoints.GetDefaultLocationPathAsync(db,companyId,ct);var x=new InventoryComponent{CompanyId=companyId,Name=r.Name.Trim(),Category=Clean(r.Category),ModelNumber=Clean(r.ModelNumber),ItemNumber=Clean(r.ItemNumber),SerialNumber=Clean(r.SerialNumber),Supplier=Clean(r.Supplier),SupplierId=await SupplierCatalogEndpoints.ResolveSupplierIdAsync(db,companyId,r.Supplier,ct),Location=location,LocationCatalogId=await AssetEndpoints.ResolveLocationCatalogIdAsync(db,companyId,location,ct),MinimumQuantity=r.MinimumQuantity,TotalQuantity=r.Quantity,UnitCost=r.UnitCost,Currency=r.Currency.Trim().ToUpperInvariant(),PurchaseDate=r.PurchaseDate,Notes=Clean(r.Notes)};db.InventoryComponents.Add(x);if(r.Quantity>0)db.ComponentMovements.Add(new ComponentMovement{CompanyId=companyId,ComponentId=x.Id,ActorUserId=UserId(user),MovementType="Received",Quantity=r.Quantity,Notes="İlk stok kaydı"});Audit(db,companyId,user,x.Id,"component.created",x.Name);await db.SaveChangesAsync(ct);return Results.Created($"/api/components/{x.Id}",new{ x.Id});
        }).RequireAuthorization("asset.write");
        api.MapPut("/components/{id:guid}",async(Guid id,UpdateRequest r,ClaimsPrincipal user,AppDbContext db,CancellationToken ct)=>
        {
            if(!Tenant(user,out var companyId))return Results.Forbid();var x=await db.InventoryComponents.SingleOrDefaultAsync(a=>a.CompanyId==companyId&&a.Id==id,ct);if(x is null)return Results.NotFound();var assigned=await db.ComponentAllocations.Where(a=>a.CompanyId==companyId&&a.ComponentId==id).SumAsync(a=>(int?)(a.Quantity-a.RemovedQuantity),ct)??0;var errors=Validate(r.Name,r.MinimumQuantity,x.TotalQuantity,r.UnitCost,r.Currency,r.Category,r.ModelNumber,r.ItemNumber,r.SerialNumber,r.Supplier,r.Location,r.Notes);if(x.TotalQuantity<assigned)errors["quantity"]=["Toplam stok atanmış miktardan az olamaz."];if(errors.Count>0)return Results.ValidationProblem(errors);x.Name=r.Name.Trim();x.Category=Clean(r.Category);x.ModelNumber=Clean(r.ModelNumber);x.ItemNumber=Clean(r.ItemNumber);x.SerialNumber=Clean(r.SerialNumber);x.Supplier=Clean(r.Supplier);x.SupplierId=await SupplierCatalogEndpoints.ResolveSupplierIdAsync(db,companyId,r.Supplier,ct);x.Location=Clean(r.Location);x.LocationCatalogId=await AssetEndpoints.ResolveLocationCatalogIdAsync(db,companyId,r.Location,ct);x.MinimumQuantity=r.MinimumQuantity;x.UnitCost=r.UnitCost;x.Currency=r.Currency.Trim().ToUpperInvariant();x.PurchaseDate=r.PurchaseDate;x.Notes=Clean(r.Notes);x.UpdatedAt=DateTimeOffset.UtcNow;Audit(db,companyId,user,id,"component.updated",x.Name);await db.SaveChangesAsync(ct);return Results.NoContent();
        }).RequireAuthorization("asset.write");
        api.MapPost("/components/{id:guid}/stock",async(Guid id,StockRequest r,ClaimsPrincipal user,AppDbContext db,CancellationToken ct)=>
        {
            if(!Tenant(user,out var companyId))return Results.Forbid();if(r.Quantity is 0 or <-100000 or >100000)return Results.ValidationProblem(new Dictionary<string,string[]>{{"quantity",["Sıfır dışı stok düzeltmesi -100.000 ile 100.000 arasında olmalı."]}});await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);var x=await db.InventoryComponents.FromSqlInterpolated($"SELECT * FROM \"InventoryComponents\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);if(x is null)return Results.NotFound();var assigned=await db.ComponentAllocations.Where(a=>a.CompanyId==companyId&&a.ComponentId==id).SumAsync(a=>(int?)(a.Quantity-a.RemovedQuantity),ct)??0;var total=x.TotalQuantity+r.Quantity;if(total<assigned||total>100000)return Results.ValidationProblem(new Dictionary<string,string[]>{{"quantity",[$"Yeni toplam, atanmış {assigned} adetten az veya 100.000'den fazla olamaz."]}});x.TotalQuantity=total;x.UpdatedAt=DateTimeOffset.UtcNow;db.ComponentMovements.Add(new ComponentMovement{CompanyId=companyId,ComponentId=id,ActorUserId=UserId(user),MovementType=r.Quantity>0?"Received":"Adjustment",Quantity=Math.Abs(r.Quantity),Notes=Clean(r.Notes)});Audit(db,companyId,user,id,"component.stock-adjusted",$"{r.Quantity:+#;-#;0} adet");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return Results.Ok(new{totalQuantity=total,allocated=assigned,available=total-assigned});
        }).RequireAuthorization("asset.write");
        api.MapPost("/components/bulk-stock-adjustment", async (BulkStockRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || UserId(user) is not Guid actorId) return Results.Forbid();
            var items = request.Items ?? [];
            if (items.Length is < 1 or > 100 || items.Any(x => x.ComponentId == Guid.Empty || x.Quantity == 0 || Math.Abs((long)x.Quantity) > 100000) || items.Select(x => x.ComponentId).Distinct().Count() != items.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["1 ile 100 arasında tekrarsız bileşen seçin; fark sıfır olamaz ve ±100.000 aralığında olmalı."] });
            if (request.Notes?.Length > 1900) return Results.ValidationProblem(new Dictionary<string, string[]> { ["notes"] = ["Sayım notu en fazla 1.900 karakter olabilir."] });
            var ids = items.Select(x => x.ComponentId).ToArray();
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var components = await db.InventoryComponents.FromSqlInterpolated(
                    $"SELECT * FROM \"InventoryComponents\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = ANY ({ids}) ORDER BY \"Id\" FOR UPDATE")
                .ToListAsync(ct);
            if (components.Count != items.Length) return Results.NotFound(new { message = "Bileşenlerden biri bu şirkette bulunamadı; hiçbir stok değişmedi." });
            var allocated = await db.ComponentAllocations.Where(x => x.CompanyId == companyId && ids.Contains(x.ComponentId))
                .GroupBy(x => x.ComponentId).Select(group => new { ComponentId = group.Key, Quantity = group.Sum(x => x.Quantity - x.RemovedQuantity) })
                .ToDictionaryAsync(x => x.ComponentId, x => x.Quantity, ct);
            var invalid = new List<string>();
            foreach (var component in components)
            {
                var delta = items.Single(x => x.ComponentId == component.Id).Quantity;
                var total = component.TotalQuantity + delta;
                var assigned = allocated.GetValueOrDefault(component.Id);
                if (total is < 0 or > 100000 || total < assigned)
                    invalid.Add($"{component.Name}: yeni toplam {total}; en az {assigned}, en fazla 100.000 olmalı.");
            }
            if (invalid.Count > 0) return Results.Conflict(new { message = "Bir bileşenin stok farkı geçersiz; hiçbir stok değişmedi.", items = invalid });
            var note = Clean(request.Notes);
            foreach (var component in components)
            {
                var delta = items.Single(x => x.ComponentId == component.Id).Quantity;
                var movementNote = $"Toplu stok sayım farkı {delta:+#;-#;0} adet" + (string.IsNullOrWhiteSpace(note) ? "" : $" · {note}");
                component.TotalQuantity += delta; component.UpdatedAt = DateTimeOffset.UtcNow;
                db.ComponentMovements.Add(new ComponentMovement { CompanyId = companyId, ComponentId = component.Id, ActorUserId = actorId, MovementType = delta > 0 ? "Received" : "Adjustment", Quantity = Math.Abs(delta), Notes = movementNote });
                Audit(db, companyId, user, component.Id, delta > 0 ? "component.bulk-stock-received" : "component.bulk-stock-adjusted", movementNote);
            }
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return Results.Ok(new { adjusted = components.Count });
        }).RequireAuthorization("asset.write");

        api.MapPost("/components/bulk-allocate", async (BulkAllocateRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || UserId(user) is not Guid actorId) return Results.Forbid();
            var items = request.Items ?? [];
            if (request.AssetId == Guid.Empty || items.Length is < 1 or > 100 || items.Any(x => x.ComponentId == Guid.Empty || x.Quantity is < 1 or > 100000) || items.Select(x => x.ComponentId).Distinct().Count() != items.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Bir varlık ve 1–100 arasında tekrarsız bileşen; her biri için 1–100.000 adet seçin."] });
            if (request.Notes?.Length > 2000) return Results.ValidationProblem(new Dictionary<string, string[]> { ["notes"] = ["Not en fazla 2.000 karakter olabilir."] });
            if (!await db.Assets.AnyAsync(x => x.CompanyId == companyId && x.Id == request.AssetId && x.Status != "Archived", ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assetId"] = ["Bu şirkette etkin bir varlık seçilmelidir."] });
            var ids = items.Select(x => x.ComponentId).ToArray();
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var components = await db.InventoryComponents.FromSqlInterpolated(
                    $"SELECT * FROM \"InventoryComponents\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = ANY ({ids}) ORDER BY \"Id\" FOR UPDATE")
                .ToListAsync(ct);
            if (components.Count != items.Length) return Results.NotFound(new { message = "Bileşenlerden biri bu şirkette bulunamadı; hiçbir atama yapılmadı." });
            var allocated = await db.ComponentAllocations.Where(x => x.CompanyId == companyId && ids.Contains(x.ComponentId))
                .GroupBy(x => x.ComponentId).Select(group => new { ComponentId = group.Key, Quantity = group.Sum(x => x.Quantity - x.RemovedQuantity) })
                .ToDictionaryAsync(x => x.ComponentId, x => x.Quantity, ct);
            var shortages = components.Where(component => items.Single(x => x.ComponentId == component.Id).Quantity > component.TotalQuantity - allocated.GetValueOrDefault(component.Id))
                .Select(component => $"{component.Name}: kullanılabilir miktar {component.TotalQuantity - allocated.GetValueOrDefault(component.Id)}.").ToArray();
            if (shortages.Length > 0) return Results.Conflict(new { message = "Seçili bileşenlerden en az birinde yeterli stok yok; hiçbir atama yapılmadı.", items = shortages });
            var note = Clean(request.Notes);
            foreach (var component in components)
            {
                var quantity = items.Single(x => x.ComponentId == component.Id).Quantity;
                var allocation = new ComponentAllocation { CompanyId = companyId, ComponentId = component.Id, AssetId = request.AssetId, ActorUserId = actorId, Quantity = quantity, Notes = note };
                db.ComponentAllocations.Add(allocation);
                db.ComponentMovements.Add(new ComponentMovement { CompanyId = companyId, ComponentId = component.Id, AllocationId = allocation.Id, AssetId = request.AssetId, ActorUserId = actorId, MovementType = "Checkout", Quantity = quantity, Notes = note });
                Audit(db, companyId, user, component.Id, "component.bulk-allocated", $"{quantity} adet · varlık {request.AssetId}");
            }
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return Results.Ok(new { allocated = components.Count, assetId = request.AssetId });
        }).RequireAuthorization("asset.write");

        api.MapPost("/components/{id:guid}/allocate",async(Guid id,AllocateRequest r,ClaimsPrincipal user,AppDbContext db,CancellationToken ct)=>
        {
            if(!Tenant(user,out var companyId))return Results.Forbid();if(r.Quantity is <1 or >100000)return Results.ValidationProblem(new Dictionary<string,string[]>{{"quantity",["Atama miktarı 1 ile 100.000 arasında olmalı."]}});
            // The component row lock serializes every stock-changing request for this component.
            // ReadCommitted lets the request that waited on the lock see the preceding committed allocation.
            await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted,ct);
            var x=await db.InventoryComponents.FromSqlInterpolated($"SELECT * FROM \"InventoryComponents\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);if(x is null)return Results.NotFound();if(!await db.Assets.AnyAsync(a=>a.CompanyId==companyId&&a.Id==r.AssetId&&a.Status!="Archived",ct))return Results.ValidationProblem(new Dictionary<string,string[]>{{"assetId",["Aktif varlık seçilmelidir."]}});var allocated=await db.ComponentAllocations.Where(a=>a.CompanyId==companyId&&a.ComponentId==id).SumAsync(a=>(int?)(a.Quantity-a.RemovedQuantity),ct)??0;if(r.Quantity>x.TotalQuantity-allocated)return Results.Conflict(new{message=$"Yeterli bileşen stoğu yok. Kullanılabilir miktar: {x.TotalQuantity-allocated}."});var arow=new ComponentAllocation{CompanyId=companyId,ComponentId=id,AssetId=r.AssetId,ActorUserId=UserId(user),Quantity=r.Quantity,Notes=Clean(r.Notes)};db.ComponentAllocations.Add(arow);db.ComponentMovements.Add(new ComponentMovement{CompanyId=companyId,ComponentId=id,AllocationId=arow.Id,AssetId=r.AssetId,ActorUserId=UserId(user),MovementType="Checkout",Quantity=r.Quantity,Notes=Clean(r.Notes)});Audit(db,companyId,user,id,"component.allocated",$"{r.Quantity} adet · varlık {r.AssetId}");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return Results.Created($"/api/components/{id}",new{arow.Id,available=x.TotalQuantity-allocated-r.Quantity});
        }).RequireAuthorization("asset.write");
        api.MapPost("/components/{id:guid}/remove",async(Guid id,RemoveRequest r,ClaimsPrincipal user,AppDbContext db,CancellationToken ct)=>
        {
            if(!Tenant(user,out var companyId))return Results.Forbid();if(r.Quantity is <1 or >100000)return Results.ValidationProblem(new Dictionary<string,string[]>{{"quantity",["Çıkarma miktarı 1 ile 100.000 arasında olmalı."]}});await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);var x=await db.InventoryComponents.FromSqlInterpolated($"SELECT * FROM \"InventoryComponents\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);if(x is null)return Results.NotFound();var allocation=await db.ComponentAllocations.SingleOrDefaultAsync(a=>a.CompanyId==companyId&&a.ComponentId==id&&a.Id==r.AllocationId,ct);if(allocation is null)return Results.NotFound();var remaining=allocation.Quantity-allocation.RemovedQuantity;if(r.Quantity>remaining)return Results.ValidationProblem(new Dictionary<string,string[]>{{"quantity",[$"Bu varlığa atanmış kalan miktar {remaining}."]}});allocation.RemovedQuantity+=r.Quantity;allocation.LastRemovedAt=DateTimeOffset.UtcNow;db.ComponentMovements.Add(new ComponentMovement{CompanyId=companyId,ComponentId=id,AllocationId=allocation.Id,AssetId=allocation.AssetId,ActorUserId=UserId(user),MovementType="Checkin",Quantity=r.Quantity,Notes=Clean(r.Notes)});Audit(db,companyId,user,id,"component.removed",$"{r.Quantity} adet varlıktan çıkarıldı.");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return Results.NoContent();
        }).RequireAuthorization("asset.write");
        return api;
    }

    private static async Task<IResult> ImportCsvAsync(HttpContext context, AppDbContext db, IAntiforgery antiforgery, bool commit, CancellationToken ct)
    {
        if (!Tenant(context.User, out var companyId) || UserId(context.User) is not { } actorId) return Results.Forbid();
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
        Dictionary<string, string>? explicitMappings = null;
        var encodedMapping = context.Request.Headers["X-Component-Import-Mapping"].ToString();
        if (encodedMapping.Length > 0)
        {
            if (encodedMapping.Length > 24 * 1024) return Results.BadRequest(new { message = "Sütun eşlemesi sınırı aşıyor." });
            try { explicitMappings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(Convert.FromBase64String(encodedMapping)); }
            catch (Exception exception) when (exception is FormatException or System.Text.Json.JsonException)
            { return Results.ValidationProblem(new Dictionary<string, string[]> { ["mappings"] = ["Sütun eşlemesi okunamadı; dosyayı yeniden doğrulayın."] }); }
            if (!ComponentCsvImportMapping.ValidateExplicitMappings(explicitMappings, out var mappingError))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["mappings"] = [mappingError] });
        }
        var isXlsx = DataExportEndpoints.IsXlsxRequest(context.Request);
        if (!isXlsx && context.Request.ContentType?.StartsWith("text/csv", StringComparison.OrdinalIgnoreCase) != true) return Results.StatusCode(415);
        if (context.Request.ContentLength > CsvImportMaxBytes) return Results.StatusCode(413);
        using var buffer = new MemoryStream(); var chunk = new byte[64 * 1024];
        while (true) { var read = await context.Request.Body.ReadAsync(chunk.AsMemory(), ct); if (read == 0) break; if (buffer.Length + read > CsvImportMaxBytes) return Results.StatusCode(413); await buffer.WriteAsync(chunk.AsMemory(0, read), ct); }
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
        rows = ComponentCsvImportMapping.Normalize(rows, out var unsupportedColumns, explicitMappings);
        if (unsupportedColumns.Length > 0)
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { $"Dosyadaki sütunları eşleyin veya açıkça yoksayın: {string.Join(", ", unsupportedColumns)}. Veri kaybını önlemek için aktarım durduruldu." }, unmappedColumns = unsupportedColumns, mappingTargets = ComponentCsvImportMapping.MappingTargets() });
        string[] header = ["Bileşen", "Kategori", "Model", "Ürün no", "Seri no", "Tedarikçi", "Konum", "Toplam", "Minimum", "Birim maliyeti", "Para birimi", "Alış tarihi", "Notlar"];
        if (rows.Count is < 2 || rows.Count > CsvImportMaxRows + 1 || !rows[0].SequenceEqual(header, StringComparer.OrdinalIgnoreCase))
            return Results.UnprocessableEntity(new { isValid = false, errors = new[] { $"Şablon başlığı ve en fazla {CsvImportMaxRows:N0} bileşen satırı kullanın." } });

        var errors = new List<string>(); var components = new List<InventoryComponent>(rows.Count - 1);
        var rowKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var rowItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var rowSerials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (row, index) in rows.Skip(1).Select((value, index) => (value, index)))
        {
            var line = index + 2;
            if (row.Count != header.Length) { AddCsvError(errors, $"{line}. satırda {header.Length} sütun olmalıdır."); continue; }
            var name = row[0].Trim(); var category = Clean(row[1]); var model = Clean(row[2]); var itemNumber = Clean(row[3]);
            var serialNumber = Clean(row[4]); var supplier = Clean(row[5]); var location = Clean(row[6]); var notes = Clean(row[12]);
            if (string.IsNullOrWhiteSpace(name) || name.Length > 200) AddCsvError(errors, $"{line}. satırdaki bileşen adı boş veya 200 karakterden uzun.");
            if (!int.TryParse(row[7], NumberStyles.None, CultureInfo.InvariantCulture, out var total) || total is < 0 or > 100000) AddCsvError(errors, $"{line}. satırdaki toplam stok 0–100.000 arasında tam sayı olmalıdır.");
            if (!int.TryParse(row[8], NumberStyles.None, CultureInfo.InvariantCulture, out var minimum) || minimum is < 0 or > 100000) AddCsvError(errors, $"{line}. satırdaki minimum stok 0–100.000 arasında tam sayı olmalıdır.");
            decimal? costValue = null;
            if (!string.IsNullOrWhiteSpace(row[9])) { if (!decimal.TryParse(row[9], NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || value is < 0 or > 9999999999999999.99m) AddCsvError(errors, $"{line}. satırdaki birim maliyeti geçersiz."); else costValue = value; }
            var currency = row[10].Trim().ToUpperInvariant(); if (currency.Length != 3 || !currency.All(char.IsAsciiLetter)) AddCsvError(errors, $"{line}. satırdaki para birimi üç harfli olmalıdır.");
            DateOnly? purchaseDate = null;
            if (!string.IsNullOrWhiteSpace(row[11])) { if (DateOnly.TryParseExact(row[11].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) purchaseDate = date; else AddCsvError(errors, $"{line}. satırdaki alış tarihi yyyy-AA-gg olmalıdır."); }
            if (category?.Length > 120 || model?.Length > 120 || itemNumber?.Length > 120 || serialNumber?.Length > 160 || supplier?.Length > 200 || location?.Length > 160 || notes?.Length > 2000) AddCsvError(errors, $"{line}. satırdaki metin alanlarından biri uzunluk sınırını aşıyor.");
            var key = $"{name.ToUpperInvariant()}\u001f{itemNumber?.ToUpperInvariant() ?? ""}";
            if (!rowKeys.Add(key)) AddCsvError(errors, $"{line}. satır aynı bileşen/ürün no bileşimini yineliyor.");
            if (itemNumber is not null && !rowItems.Add(itemNumber)) AddCsvError(errors, $"{line}. satırdaki ürün no dosyada yineleniyor.");
            if (serialNumber is not null && !rowSerials.Add(serialNumber)) AddCsvError(errors, $"{line}. satırdaki seri no dosyada yineleniyor.");
            components.Add(new InventoryComponent { CompanyId = companyId, Name = name, Category = category, ModelNumber = model, ItemNumber = itemNumber,
                SerialNumber = serialNumber, Supplier = supplier, Location = location, TotalQuantity = total, MinimumQuantity = minimum,
                UnitCost = costValue, Currency = currency, PurchaseDate = purchaseDate, Notes = notes });
        }
        if (errors.Count > 0) return Results.UnprocessableEntity(new { isValid = false, rowCount = rows.Count - 1, errors = errors.Distinct(StringComparer.Ordinal).Take(30), message = "CSV'de düzeltilmesi gereken alanlar bulundu. Hiçbir bileşen eklenmedi." });

        var names = components.Select(x => x.Name.ToUpper()).Distinct().ToArray();
        var itemNumbers = components.Where(x => x.ItemNumber != null).Select(x => x.ItemNumber!.ToUpper()).Distinct().ToArray();
        var serialNumbers = components.Where(x => x.SerialNumber != null).Select(x => x.SerialNumber!.ToUpper()).Distinct().ToArray();
        var existing = await db.InventoryComponents.AsNoTracking().Where(x => x.CompanyId == companyId &&
            (names.Contains(x.Name.ToUpper()) || (x.ItemNumber != null && itemNumbers.Contains(x.ItemNumber.ToUpper())) || (x.SerialNumber != null && serialNumbers.Contains(x.SerialNumber.ToUpper()))))
            .Select(x => new { x.Name, x.ItemNumber, x.SerialNumber }).ToListAsync(ct);
        var oldKeys = existing.Select(x => $"{x.Name.ToUpperInvariant()}\u001f{x.ItemNumber?.ToUpperInvariant() ?? ""}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var oldItems = existing.Where(x => x.ItemNumber != null).Select(x => x.ItemNumber!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var oldSerials = existing.Where(x => x.SerialNumber != null).Select(x => x.SerialNumber!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var matches = components.Count(x => oldKeys.Contains($"{x.Name.ToUpperInvariant()}\u001f{x.ItemNumber?.ToUpperInvariant() ?? ""}") || x.ItemNumber != null && oldItems.Contains(x.ItemNumber) || x.SerialNumber != null && oldSerials.Contains(x.SerialNumber));
        if (matches > 0) return Results.Conflict(new { isValid = false, existingMatches = matches, errors = new[] { $"Şirkette mevcut ad/ürün no/seri no eşleşmeleri bulundu ({matches}). Yinelenen bileşen eklenmedi." } });
        var locationPaths = await AssetEndpoints.GetActiveLocationPathMapAsync(db, companyId, ct);
        var defaultLocation = await AssetEndpoints.GetDefaultLocationPathAsync(db, companyId, ct);
        var defaultLocationApplied = defaultLocation is null ? 0 : components.Count(x => x.Location is null);
        foreach (var component in components)
        {
            component.Location ??= defaultLocation;
            if (component.Location is not null && locationPaths.TryGetValue(component.Location, out var locationCatalogId)) component.LocationCatalogId = locationCatalogId;
        }
        if (!commit) return Results.Ok(new { isValid = true, rowCount = components.Count, defaultLocationApplied, errors = Array.Empty<string>(), message = $"{components.Count:N0} yeni bileşen doğrulandı. Varsayılan konum {defaultLocationApplied:N0} boş satıra uygulanacak. Önizleme kayıt eklemedi." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var latest = await db.InventoryComponents.AsNoTracking().Where(x => x.CompanyId == companyId &&
            (names.Contains(x.Name.ToUpper()) || (x.ItemNumber != null && itemNumbers.Contains(x.ItemNumber.ToUpper())) || (x.SerialNumber != null && serialNumbers.Contains(x.SerialNumber.ToUpper()))))
            .Select(x => new { x.Name, x.ItemNumber, x.SerialNumber }).ToListAsync(ct);
        var latestKeys = latest.Select(x => $"{x.Name.ToUpperInvariant()}\u001f{x.ItemNumber?.ToUpperInvariant() ?? ""}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var latestItems = latest.Where(x => x.ItemNumber != null).Select(x => x.ItemNumber!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var latestSerials = latest.Where(x => x.SerialNumber != null).Select(x => x.SerialNumber!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (components.Any(x => latestKeys.Contains($"{x.Name.ToUpperInvariant()}\u001f{x.ItemNumber?.ToUpperInvariant() ?? ""}") || x.ItemNumber != null && latestItems.Contains(x.ItemNumber) || x.SerialNumber != null && latestSerials.Contains(x.SerialNumber)))
            return Results.Conflict(new { message = "Önizlemeden sonra aynı bileşen eklendi. Hiçbir kayıt aktarılmadı; önizlemeyi yenileyin." });
        var suppliers = await db.SupplierCatalogEntries.AsNoTracking().Where(x => x.CompanyId == companyId).Select(x => new { x.Id, x.NormalizedName }).ToListAsync(ct);
        var supplierMap = suppliers.GroupBy(x => x.NormalizedName, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Select(y => y.Id).ToArray(), StringComparer.OrdinalIgnoreCase);
        foreach (var component in components)
        {
            var normalizedSupplier = component.Supplier?.Normalize(NormalizationForm.FormKC).ToUpperInvariant();
            if (normalizedSupplier is not null && supplierMap.TryGetValue(normalizedSupplier, out var ids) && ids.Length == 1) component.SupplierId = ids[0];
            db.InventoryComponents.Add(component);
            if (component.TotalQuantity > 0) db.ComponentMovements.Add(new ComponentMovement { CompanyId = companyId, ComponentId = component.Id, ActorUserId = actorId,
                MovementType = "Received", Quantity = component.TotalQuantity, Notes = $"{(isXlsx ? "Excel" : "CSV")} içe aktarımındaki başlangıç stoku" });
            Audit(db, companyId, context.User, component.Id, "component.created", $"{component.Name}; {(isXlsx ? "Excel" : "CSV")} içe aktarımı.");
        }
        db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Component", Action = isXlsx ? "XlsxImported" : "CsvImported", Details = $"{(isXlsx ? "Excel" : "CSV")} dosyasından {components.Count} bileşen içe aktarıldı." });
        try { await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); }
        catch (DbUpdateException) { await transaction.RollbackAsync(ct); return Results.Conflict(new { message = "CSV aktarılmadı; bir kayıt çakıştı. Önizlemeyi yenileyip tekrar deneyin." }); }
        return Results.Ok(new { imported = components.Count, message = $"{components.Count:N0} bileşen ve başlangıç stok hareketleri eklendi." });
    }

    private static void AddCsvError(List<string> errors, string message) { if (errors.Count < 100) errors.Add(message); }
    private static char DetectCsvDelimiter(string text)
    {
        var commas = 0; var semicolons = 0; var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var value = text[i];
            if (value == '"') { if (quoted && i + 1 < text.Length && text[i + 1] == '"') { i++; continue; } quoted = !quoted; continue; }
            if (quoted) continue;
            if (value is '\r' or '\n') break;
            if (value == ',') commas++; else if (value == ';') semicolons++;
        }
        return semicolons > commas ? ';' : ',';
    }

    private static List<List<string>> ParseCsv(string text, int maximumRecords, char delimiter)
    {
        var records = new List<List<string>>(); var row = new List<string>(); var cell = new StringBuilder(); var quoted = false; var afterQuote = false;
        for (var i = 0; i < text.Length; i++) { var ch = text[i]; if (quoted) { if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; } else if (ch == '"') { quoted = false; afterQuote = true; } else cell.Append(ch); continue; }
            if (afterQuote && ch != delimiter && ch is not ('\r' or '\n')) throw new FormatException();
            if (ch == '"') { if (cell.Length != 0) throw new FormatException(); quoted = true; } else if (ch == delimiter) { row.Add(cell.ToString()); cell.Clear(); afterQuote = false; }
            else if (ch is '\r' or '\n') { if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++; row.Add(cell.ToString()); cell.Clear(); afterQuote = false; if (row.Any(x => x.Length > 0)) { records.Add(row); if (records.Count > maximumRecords) throw new FormatException(); } row = []; }
            else { cell.Append(ch); afterQuote = false; } }
        if (quoted) throw new FormatException(); if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); if (row.Any(x => x.Length > 0)) records.Add(row); }
        return records;
    }
    private static bool Tenant(ClaimsPrincipal u,out Guid id)=>Guid.TryParse(u.FindFirstValue("company_id"),out id);
    private static Guid? UserId(ClaimsPrincipal u)=>Guid.TryParse(u.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:null;
    private static string? Clean(string? s)=>string.IsNullOrWhiteSpace(s)?null:s.Trim();
    private static void Audit(AppDbContext db,Guid companyId,ClaimsPrincipal user,Guid id,string action,string details)=>db.AuditEntries.Add(new AuditEntry{CompanyId=companyId,ActorUserId=UserId(user),TargetType="Component",TargetId=id,Action=action,Details=details});
    private static string CsvCell(string? value){var safe=value?.Trim()??"";if(safe.Length>0&&safe[0] is '=' or '+' or '-' or '@' or '\t' or '\r')safe="'"+safe;return "\""+safe.Replace("\"","\"\"")+"\"";}
    private static Dictionary<string,string[]> Validate(string name,int min,int total,decimal? cost,string currency,string? category,string? model,string? item,string? serial,string? supplier,string? location,string? notes){var e=new Dictionary<string,string[]>();void A(string k,string m)=>e[k]=[m];if(string.IsNullOrWhiteSpace(name)||name.Trim().Length>200)A("name","Ad zorunludur ve 200 karakteri aşamaz.");if(min is <0 or >100000)A("minimumQuantity","Minimum miktar 0–100.000 arasında olmalı.");if(total is <0 or >100000)A("quantity","Stok miktarı 0–100.000 arasında olmalı.");if(cost<0)A("unitCost","Maliyet negatif olamaz.");if(string.IsNullOrWhiteSpace(currency)||currency.Trim().Length!=3||currency.Any(c=>!char.IsAsciiLetter(c)))A("currency","Para birimi üç harfli kod olmalı.");if(category?.Length>120)A("category","Kategori 120 karakteri aşamaz.");if(model?.Length>120)A("modelNumber","Model 120 karakteri aşamaz.");if(item?.Length>120)A("itemNumber","Ürün no 120 karakteri aşamaz.");if(serial?.Length>160)A("serialNumber","Seri no 160 karakteri aşamaz.");if(supplier?.Length>200)A("supplier","Tedarikçi 200 karakteri aşamaz.");if(location?.Length>160)A("location","Konum 160 karakteri aşamaz.");if(notes?.Length>2000)A("notes","Not 2.000 karakteri aşamaz.");return e;}
    public sealed record CreateRequest(string Name,string? Category,string? ModelNumber,string? ItemNumber,string? SerialNumber,string? Supplier,string? Location,int MinimumQuantity,int Quantity,decimal? UnitCost,string Currency,DateOnly? PurchaseDate,string? Notes);
    public sealed record UpdateRequest(string Name,string? Category,string? ModelNumber,string? ItemNumber,string? SerialNumber,string? Supplier,string? Location,int MinimumQuantity,decimal? UnitCost,string Currency,DateOnly? PurchaseDate,string? Notes);
    public sealed record StockRequest(int Quantity,string? Notes);public sealed record BulkStockRequest(BulkStockItem[]? Items,string? Notes);public sealed record BulkStockItem(Guid ComponentId,int Quantity);public sealed record BulkAllocateRequest(Guid AssetId,BulkAllocateItem[]? Items,string? Notes);public sealed record BulkAllocateItem(Guid ComponentId,int Quantity);public sealed record AllocateRequest(Guid AssetId,int Quantity,string? Notes);public sealed record RemoveRequest(Guid AllocationId,int Quantity,string? Notes);
}
