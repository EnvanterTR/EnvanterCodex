using System.Security.Claims;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class ComponentEndpoints
{
    public static RouteGroupBuilder MapComponentEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/components", async ([FromQuery]string? q,[FromQuery]string? stock,[FromQuery]int page,[FromQuery]int pageSize,ClaimsPrincipal user,AppDbContext db,CancellationToken ct)=>
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
        api.MapGet("/components/{id:guid}",async(Guid id,ClaimsPrincipal user,AppDbContext db,CancellationToken ct)=>
        {
            if(!Tenant(user,out var companyId))return Results.Forbid();var item=await db.InventoryComponents.AsNoTracking().Where(x=>x.CompanyId==companyId&&x.Id==id).Select(x=>new{x.Id,x.Name,x.Category,x.ModelNumber,x.ItemNumber,x.SerialNumber,x.Supplier,x.Location,x.MinimumQuantity,x.TotalQuantity,x.UnitCost,x.Currency,x.PurchaseDate,x.Notes,x.UpdatedAt,allocated=db.ComponentAllocations.Where(a=>a.CompanyId==companyId&&a.ComponentId==x.Id).Sum(a=>(int?)(a.Quantity-a.RemovedQuantity))??0}).SingleOrDefaultAsync(ct);if(item is null)return Results.NotFound();
            var allocations=await db.ComponentAllocations.AsNoTracking().Where(a=>a.CompanyId==companyId&&a.ComponentId==id).OrderBy(a=>a.RemovedQuantity>=a.Quantity).ThenByDescending(a=>a.AllocatedAt).Take(200).Select(a=>new{a.Id,a.AssetId,asset=db.Assets.Where(x=>x.CompanyId==companyId&&x.Id==a.AssetId).Select(x=>x.AssetTag+" · "+x.Name).FirstOrDefault(),a.Quantity,a.RemovedQuantity,remaining=a.Quantity-a.RemovedQuantity,a.AllocatedAt,a.LastRemovedAt,a.Notes}).ToListAsync(ct);
            var movements=await db.ComponentMovements.AsNoTracking().Where(m=>m.CompanyId==companyId&&m.ComponentId==id).OrderByDescending(m=>m.OccurredAt).Take(100).Select(m=>new{m.MovementType,m.Quantity,m.Notes,m.OccurredAt,asset=db.Assets.Where(a=>a.CompanyId==companyId&&a.Id==m.AssetId).Select(a=>a.AssetTag+" · "+a.Name).FirstOrDefault()}).ToListAsync(ct);return Results.Ok(new{item,allocations,movements});
        }).RequireAuthorization("asset.read");
        api.MapPost("/components",async(CreateRequest r,ClaimsPrincipal user,AppDbContext db,CancellationToken ct)=>
        {
            if(!Tenant(user,out var companyId))return Results.Forbid();var err=Validate(r.Name,r.MinimumQuantity,r.Quantity,r.UnitCost,r.Currency,r.Category,r.ModelNumber,r.ItemNumber,r.SerialNumber,r.Supplier,r.Location,r.Notes);if(err.Count>0)return Results.ValidationProblem(err);
            var x=new InventoryComponent{CompanyId=companyId,Name=r.Name.Trim(),Category=Clean(r.Category),ModelNumber=Clean(r.ModelNumber),ItemNumber=Clean(r.ItemNumber),SerialNumber=Clean(r.SerialNumber),Supplier=Clean(r.Supplier),Location=Clean(r.Location),MinimumQuantity=r.MinimumQuantity,TotalQuantity=r.Quantity,UnitCost=r.UnitCost,Currency=r.Currency.Trim().ToUpperInvariant(),PurchaseDate=r.PurchaseDate,Notes=Clean(r.Notes)};db.InventoryComponents.Add(x);if(r.Quantity>0)db.ComponentMovements.Add(new ComponentMovement{CompanyId=companyId,ComponentId=x.Id,ActorUserId=UserId(user),MovementType="Received",Quantity=r.Quantity,Notes="İlk stok kaydı"});Audit(db,companyId,user,x.Id,"component.created",x.Name);await db.SaveChangesAsync(ct);return Results.Created($"/api/components/{x.Id}",new{ x.Id});
        }).RequireAuthorization("asset.write");
        api.MapPut("/components/{id:guid}",async(Guid id,UpdateRequest r,ClaimsPrincipal user,AppDbContext db,CancellationToken ct)=>
        {
            if(!Tenant(user,out var companyId))return Results.Forbid();var x=await db.InventoryComponents.SingleOrDefaultAsync(a=>a.CompanyId==companyId&&a.Id==id,ct);if(x is null)return Results.NotFound();var assigned=await db.ComponentAllocations.Where(a=>a.CompanyId==companyId&&a.ComponentId==id).SumAsync(a=>(int?)(a.Quantity-a.RemovedQuantity),ct)??0;var errors=Validate(r.Name,r.MinimumQuantity,x.TotalQuantity,r.UnitCost,r.Currency,r.Category,r.ModelNumber,r.ItemNumber,r.SerialNumber,r.Supplier,r.Location,r.Notes);if(x.TotalQuantity<assigned)errors["quantity"]=["Toplam stok atanmış miktardan az olamaz."];if(errors.Count>0)return Results.ValidationProblem(errors);x.Name=r.Name.Trim();x.Category=Clean(r.Category);x.ModelNumber=Clean(r.ModelNumber);x.ItemNumber=Clean(r.ItemNumber);x.SerialNumber=Clean(r.SerialNumber);x.Supplier=Clean(r.Supplier);x.Location=Clean(r.Location);x.MinimumQuantity=r.MinimumQuantity;x.UnitCost=r.UnitCost;x.Currency=r.Currency.Trim().ToUpperInvariant();x.PurchaseDate=r.PurchaseDate;x.Notes=Clean(r.Notes);x.UpdatedAt=DateTimeOffset.UtcNow;Audit(db,companyId,user,id,"component.updated",x.Name);await db.SaveChangesAsync(ct);return Results.NoContent();
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
            if(!Tenant(user,out var companyId))return Results.Forbid();if(r.Quantity is <1 or >100000)return Results.ValidationProblem(new Dictionary<string,string[]>{{"quantity",["Atama miktarı 1 ile 100.000 arasında olmalı."]}});await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);var x=await db.InventoryComponents.FromSqlInterpolated($"SELECT * FROM \"InventoryComponents\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);if(x is null)return Results.NotFound();if(!await db.Assets.AnyAsync(a=>a.CompanyId==companyId&&a.Id==r.AssetId&&a.Status!="Archived",ct))return Results.ValidationProblem(new Dictionary<string,string[]>{{"assetId",["Aktif varlık seçilmelidir."]}});var allocated=await db.ComponentAllocations.Where(a=>a.CompanyId==companyId&&a.ComponentId==id).SumAsync(a=>(int?)(a.Quantity-a.RemovedQuantity),ct)??0;if(r.Quantity>x.TotalQuantity-allocated)return Results.Conflict(new{message=$"Yeterli bileşen stoğu yok. Kullanılabilir miktar: {x.TotalQuantity-allocated}."});var arow=new ComponentAllocation{CompanyId=companyId,ComponentId=id,AssetId=r.AssetId,ActorUserId=UserId(user),Quantity=r.Quantity,Notes=Clean(r.Notes)};db.ComponentAllocations.Add(arow);db.ComponentMovements.Add(new ComponentMovement{CompanyId=companyId,ComponentId=id,AllocationId=arow.Id,AssetId=r.AssetId,ActorUserId=UserId(user),MovementType="Checkout",Quantity=r.Quantity,Notes=Clean(r.Notes)});Audit(db,companyId,user,id,"component.allocated",$"{r.Quantity} adet · varlık {r.AssetId}");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return Results.Created($"/api/components/{id}",new{arow.Id,available=x.TotalQuantity-allocated-r.Quantity});
        }).RequireAuthorization("asset.write");
        api.MapPost("/components/{id:guid}/remove",async(Guid id,RemoveRequest r,ClaimsPrincipal user,AppDbContext db,CancellationToken ct)=>
        {
            if(!Tenant(user,out var companyId))return Results.Forbid();if(r.Quantity is <1 or >100000)return Results.ValidationProblem(new Dictionary<string,string[]>{{"quantity",["Çıkarma miktarı 1 ile 100.000 arasında olmalı."]}});await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);var x=await db.InventoryComponents.FromSqlInterpolated($"SELECT * FROM \"InventoryComponents\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);if(x is null)return Results.NotFound();var allocation=await db.ComponentAllocations.SingleOrDefaultAsync(a=>a.CompanyId==companyId&&a.ComponentId==id&&a.Id==r.AllocationId,ct);if(allocation is null)return Results.NotFound();var remaining=allocation.Quantity-allocation.RemovedQuantity;if(r.Quantity>remaining)return Results.ValidationProblem(new Dictionary<string,string[]>{{"quantity",[$"Bu varlığa atanmış kalan miktar {remaining}."]}});allocation.RemovedQuantity+=r.Quantity;allocation.LastRemovedAt=DateTimeOffset.UtcNow;db.ComponentMovements.Add(new ComponentMovement{CompanyId=companyId,ComponentId=id,AllocationId=allocation.Id,AssetId=allocation.AssetId,ActorUserId=UserId(user),MovementType="Checkin",Quantity=r.Quantity,Notes=Clean(r.Notes)});Audit(db,companyId,user,id,"component.removed",$"{r.Quantity} adet varlıktan çıkarıldı.");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return Results.NoContent();
        }).RequireAuthorization("asset.write");
        return api;
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
