using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AccessoryKitEndpoints
{
    public static RouteGroupBuilder MapAccessoryKitEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/accessory-kits", async ([FromQuery] string? q, ClaimsPrincipal user, AppDbContext db,
            CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            if (q?.Trim().Length > 120)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama 120 karakteri aşamaz."] });

            var kitsQuery = db.AccessoryKits.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                kitsQuery = kitsQuery.Where(x => x.Name.Contains(term) || (x.Description != null && x.Description.Contains(term)));
            }
            var kits = await kitsQuery.OrderBy(x => x.Name).Take(500).ToListAsync(ct);
            var kitIds = kits.Select(x => x.Id).ToArray();
            var summaries = await BuildKitSummariesAsync(db, companyId, kits.Select(x => (x.Id, x.Name)).ToArray(), ct);
            var results = kits.Select(kit =>
            {
                var summary = summaries[kit.Id];
                return new { kit.Id, kit.Name, kit.Description, kit.IsActive, kit.CreatedAt, kit.UpdatedAt,
                    itemCount = summary.ItemCount, totalUnits = summary.TotalUnits, availableKits = summary.AvailableKits, items = summary.Items };
            });
            return Results.Ok(new { items = results, total = kits.Count });
        }).RequireAuthorization("asset.read");

        api.MapGet("/accessory-kits/export.csv", async ([FromQuery] string? q, ClaimsPrincipal user, AppDbContext db,
            CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            if (q?.Trim().Length > 120)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama 120 karakteri aşamaz."] });
            var kitsQuery = db.AccessoryKits.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                kitsQuery = kitsQuery.Where(x => x.Name.Contains(term) || (x.Description != null && x.Description.Contains(term)));
            }
            var count = await kitsQuery.CountAsync(ct);
            if (count > 10000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["export"] = ["CSV dışa aktarımı en fazla 10.000 kiti destekler; aramayı daraltın."] });
            var kits = await kitsQuery.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Description, x.IsActive, x.CreatedAt, x.UpdatedAt }).ToListAsync(ct);
            var kitIds = kits.Select(x => x.Id).ToArray();
            var lines = await db.AccessoryKitLines.AsNoTracking().Where(x => x.CompanyId == companyId && kitIds.Contains(x.AccessoryKitId)).ToListAsync(ct);
            if (lines.Count > 100000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["export"] = ["CSV dosyası en fazla 100.000 kit kalemini destekler; aramayı daraltın."] });
            var summaries = await BuildKitSummariesAsync(db, companyId, kits.Select(x => (x.Id, x.Name)).ToArray(), ct);

            static string Cell(string? value)
            {
                if (string.IsNullOrEmpty(value)) return "\"\"";
                var trimmed = value.TrimStart();
                var safe = trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + value : value;
                return "\"" + safe.Replace("\"", "\"\"") + "\"";
            }
            var csv = new StringBuilder("\uFEFF\"Kit\",\"Açıklama\",\"Durum\",\"İçerik\",\"Toplam Parça\",\"Hazırlanabilir Kit\",\"Oluşturulma\",\"Güncellenme\"\r\n");
            foreach (var kit in kits)
            {
                var summary = summaries[kit.Id];
                var kitLines = summary.Items;
                var content = string.Join(" | ", kitLines.Select(x => $"{x.ItemName} × {x.Quantity}"));
                var available = summary.AvailableKits;
                csv.AppendJoin(',', new[] { Cell(kit.Name), Cell(kit.Description), Cell(kit.IsActive ? "Etkin" : "Arşivde"),
                    Cell(content), summary.TotalUnits.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    available.ToString(System.Globalization.CultureInfo.InvariantCulture), Cell(kit.CreatedAt.ToString("O")), Cell(kit.UpdatedAt.ToString("O")) }).Append("\r\n");
            }
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return Results.File(bytes, "text/csv; charset=utf-8", $"envantertr-hazir-kitler-{DateTime.UtcNow:yyyyMMdd}.csv");
        }).RequireAuthorization("asset.read");

        api.MapGet("/accessory-kits/{id:guid}/history", async (Guid id, ClaimsPrincipal user, AppDbContext db,
            CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var kit = await db.AccessoryKits.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new { x.Id, x.Name }).SingleOrDefaultAsync(ct);
            if (kit is null) return Results.NotFound();

            var marker = $"[KIT:{id}]";
            var history = await (from assignment in db.AccessoryAssignments.AsNoTracking()
                                 join accessory in db.Accessories.AsNoTracking()
                                     on new { assignment.CompanyId, Id = assignment.AccessoryId } equals new { accessory.CompanyId, Id = accessory.Id }
                                 where assignment.CompanyId == companyId && assignment.Notes != null && assignment.Notes.StartsWith(marker)
                                 orderby assignment.CheckedOutAt descending
                                 select new
                                 {
                                     assignment.Id, accessory.Name, assignment.TargetType, assignment.EmployeeId, assignment.AssetId,
                                     assignment.Location, assignment.Quantity, assignment.ReturnedQuantity, assignment.CheckedOutAt,
                                     assignment.LastReturnedAt, assignment.Notes,
                                     ActorUserId = db.AccessoryMovements.AsNoTracking()
                                         .Where(movement => movement.CompanyId == companyId && movement.AssignmentId == assignment.Id && movement.MovementType == "Checkout")
                                         .Select(movement => movement.ActorUserId).FirstOrDefault()
                                 }).Take(300).ToListAsync(ct);
            var employeeIds = history.Where(x => x.EmployeeId.HasValue).Select(x => x.EmployeeId!.Value).Distinct().ToArray();
            var assetIds = history.Where(x => x.AssetId.HasValue).Select(x => x.AssetId!.Value).Distinct().ToArray();
            var employees = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && employeeIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName, ct);
            var assets = await db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId && assetIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.AssetTag, ct);
            var actorIds = history.Where(x => x.ActorUserId.HasValue).Select(x => x.ActorUserId!.Value).Distinct().ToArray();
            var actors = await db.Users.AsNoTracking().Where(x => actorIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName, ct);
            var items = history.Select(x => new KitHistoryItem(x.Id, "Accessory", x.Name, x.TargetType,
                x.EmployeeId is Guid employeeId ? employees.GetValueOrDefault(employeeId)
                    : x.AssetId is Guid assetId ? assets.GetValueOrDefault(assetId) : x.Location,
                x.ActorUserId is Guid actorId ? actors.GetValueOrDefault(actorId) : null,
                x.Quantity, x.ReturnedQuantity, x.CheckedOutAt, x.LastReturnedAt,
                x.Notes!.StartsWith(marker + " ") ? x.Notes[(marker.Length + 1)..] : null)).ToList();
            var consumableHistory = await (from movement in db.ConsumableMovements.AsNoTracking()
                join consumable in db.Consumables.AsNoTracking() on new { movement.CompanyId, Id = movement.ConsumableId } equals new { consumable.CompanyId, Id = consumable.Id }
                where movement.CompanyId == companyId && movement.MovementType == "Checkout" && movement.Notes != null && movement.Notes.StartsWith(marker)
                orderby movement.OccurredAt descending
                select new KitHistoryItem(movement.Id, "Consumable", consumable.Name, "Employee",
                    db.Employees.Where(e => e.CompanyId == companyId && e.Id == movement.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                    db.Users.Where(u => u.Id == movement.ActorUserId).Select(u => u.DisplayName).FirstOrDefault(),
                    movement.Quantity, 0, movement.OccurredAt, null, StripKitMarker(movement.Notes, marker))).Take(300).ToListAsync(ct);
            var componentHistory = await (from allocation in db.ComponentAllocations.AsNoTracking()
                join component in db.InventoryComponents.AsNoTracking() on new { allocation.CompanyId, Id = allocation.ComponentId } equals new { component.CompanyId, Id = component.Id }
                where allocation.CompanyId == companyId && allocation.Notes != null && allocation.Notes.StartsWith(marker)
                orderby allocation.AllocatedAt descending
                select new KitHistoryItem(allocation.Id, "Component", component.Name, "Asset",
                    db.Assets.Where(a => a.CompanyId == companyId && a.Id == allocation.AssetId).Select(a => a.AssetTag + " · " + a.Name).FirstOrDefault(),
                    db.Users.Where(u => u.Id == allocation.ActorUserId).Select(u => u.DisplayName).FirstOrDefault(),
                    allocation.Quantity, allocation.RemovedQuantity, allocation.AllocatedAt, allocation.LastRemovedAt, StripKitMarker(allocation.Notes, marker))).Take(300).ToListAsync(ct);
            var assetHistory = await (from movement in db.AssetEvents.AsNoTracking()
                join asset in db.Assets.IgnoreQueryFilters().AsNoTracking() on new { movement.CompanyId, Id = movement.AssetId } equals new { asset.CompanyId, Id = asset.Id }
                where movement.CompanyId == companyId && movement.EventType == "Assigned" && movement.Details != null && movement.Details.StartsWith(marker)
                orderby movement.OccurredAt descending
                select new KitHistoryItem(movement.Id, "Asset", asset.Name, "Employee",
                    db.Employees.Where(e => e.CompanyId == companyId && e.Id == movement.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                    db.Users.Where(u => u.Id == movement.ActorUserId).Select(u => u.DisplayName).FirstOrDefault(),
                    1, 0, movement.OccurredAt, null, StripKitMarker(movement.Details, marker))).Take(300).ToListAsync(ct);
            items.AddRange(consumableHistory);
            items.AddRange(componentHistory);
            items.AddRange(assetHistory);
            var total = items.Count;
            items = items.OrderByDescending(x => x.CheckedOutAt).Take(300).ToList();
            return Results.Ok(new { kitId = kit.Id, kitName = kit.Name, items, total });
        }).RequireAuthorization("asset.read");

        api.MapPost("/accessory-kits", async (SaveAccessoryKitRequest request, ClaimsPrincipal user,
            AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
            var errors = Validate(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var name = request.Name.Trim();
            if (await db.AccessoryKits.AnyAsync(x => x.CompanyId == companyId && x.Name.ToLower() == name.ToLower(), ct))
                return Results.Conflict(new { message = "Bu adla bir hazır kit zaten kayıtlı." });
            var validation = await ValidateItemsAsync(db, companyId, request.Items!, ct);
            if (validation is not null) return Results.ValidationProblem(validation);

            var kit = new AccessoryKit { CompanyId = companyId, Name = name, Description = Clean(request.Description) };
            db.AccessoryKits.Add(kit);
            AddLines(db, companyId, kit.Id, request.Items!);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId,
                TargetType = "AccessoryKit", TargetId = kit.Id, Action = "created", Details = $"Hazır kit oluşturuldu: {kit.Name}" });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/accessory-kits/{kit.Id}", new { kit.Id, kit.Name });
        }).RequireAuthorization("asset.write");

        api.MapPut("/accessory-kits/{id:guid}", async (Guid id, SaveAccessoryKitRequest request, ClaimsPrincipal user,
            AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
            var errors = Validate(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var kit = await db.AccessoryKits.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (kit is null) return Results.NotFound();
            var name = request.Name.Trim();
            if (await db.AccessoryKits.AnyAsync(x => x.CompanyId == companyId && x.Id != id && x.Name.ToLower() == name.ToLower(), ct))
                return Results.Conflict(new { message = "Bu adla başka bir hazır kit zaten kayıtlı." });
            var validation = await ValidateItemsAsync(db, companyId, request.Items!, ct);
            if (validation is not null) return Results.ValidationProblem(validation);

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            kit.Name = name;
            kit.Description = Clean(request.Description);
            kit.UpdatedAt = DateTimeOffset.UtcNow;
            var previousLines = await db.AccessoryKitLines.Where(x => x.CompanyId == companyId && x.AccessoryKitId == id).ToListAsync(ct);
            db.AccessoryKitLines.RemoveRange(previousLines);
            AddLines(db, companyId, kit.Id, request.Items!);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId,
                TargetType = "AccessoryKit", TargetId = kit.Id, Action = "updated", Details = $"Hazır kit güncellendi: {kit.Name}" });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(new { kit.Id, kit.Name });
        }).RequireAuthorization("asset.write");

        api.MapDelete("/accessory-kits/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db,
            CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
            var kit = await db.AccessoryKits.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (kit is null) return Results.NotFound();
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId,
                TargetType = "AccessoryKit", TargetId = kit.Id, Action = "archived", Details = $"Hazır kit arşivlendi: {kit.Name}" });
            kit.IsActive = false;
            kit.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization("asset.write");

        api.MapPost("/accessory-kits/{id:guid}/restore", async (Guid id, ClaimsPrincipal user, AppDbContext db,
            CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
            var kit = await db.AccessoryKits.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (kit is null) return Results.NotFound();
            if (!kit.IsActive && await db.AccessoryKits.AnyAsync(x => x.CompanyId == companyId && x.Id != id && x.IsActive && x.Name.ToLower() == kit.Name.ToLower(), ct))
                return Results.Conflict(new { message = "Etkin bir kit aynı adı kullanıyor. Önce diğer kiti yeniden adlandırın." });
            kit.IsActive = true;
            kit.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId,
                TargetType = "AccessoryKit", TargetId = kit.Id, Action = "restored", Details = $"Hazır kit arşivden geri alındı: {kit.Name}" });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { kit.Id, kit.Name, kit.IsActive });
        }).RequireAuthorization("asset.write");

        return api;
    }

    private static Dictionary<string, string[]> Validate(SaveAccessoryKitRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 120)
            errors["name"] = ["Kit adı zorunludur ve 120 karakteri aşamaz."];
        if (request.Description?.Length > 1000) errors["description"] = ["Açıklama 1.000 karakteri aşamaz."];
        var items = request.Items ?? [];
        var keys = items.Select(x => (Type: x.ItemType ?? "Accessory", Id: x.ItemId ?? x.AccessoryId)).ToArray();
        if (items.Length is < 1 or > 50 || keys.Any(x => x.Id is not Guid id || id == Guid.Empty) ||
            items.Any(x => x.Quantity is < 1 or > 100000 || (x.ItemType ?? "Accessory") is not ("Accessory" or "Consumable" or "Asset" or "Component")) ||
            items.Where(x => (x.ItemType ?? "Accessory") == "Asset").Any(x => x.Quantity != 1) || keys.Distinct().Count() != items.Length)
            errors["items"] = ["Kitte 1–50 arasında tekrarsız aksesuar, sarf, varlık veya bileşen bulunmalı. Varlık adedi 1; diğer kalemler 1–100.000 olmalı."];
        if (items.Any(x => x.ItemType == "Component") && items.Count(x => x.ItemType == "Asset") != 1)
            errors["items"] = ["Bileşen içeren kitte bileşenlerin takılacağı tam bir varlık kalemi bulunmalıdır."];
        return errors;
    }

    private static void AddLines(AppDbContext db, Guid companyId, Guid kitId, AccessoryKitItemRequest[] items)
    {
        foreach (var item in items)
        {
            var type = item.ItemType ?? "Accessory";
            var id = item.ItemId ?? item.AccessoryId!.Value;
            db.AccessoryKitLines.Add(new AccessoryKitLine
            {
                CompanyId = companyId, AccessoryKitId = kitId, ItemType = type,
                AccessoryId = type == "Accessory" ? id : null,
                ConsumableId = type == "Consumable" ? id : null,
                AssetId = type == "Asset" ? id : null,
                ComponentId = type == "Component" ? id : null,
                Quantity = item.Quantity
            });
        }
    }

    private static async Task<Dictionary<Guid, KitSummary>> BuildKitSummariesAsync(AppDbContext db, Guid companyId,
        IReadOnlyCollection<(Guid Id, string Name)> kits, CancellationToken ct)
    {
        var kitIds = kits.Select(x => x.Id).ToArray();
        var lines = await db.AccessoryKitLines.AsNoTracking().Where(x => x.CompanyId == companyId && kitIds.Contains(x.AccessoryKitId)).ToListAsync(ct);
        var accessoryIds = lines.Where(x => x.AccessoryId.HasValue).Select(x => x.AccessoryId!.Value).Distinct().ToArray();
        var consumableIds = lines.Where(x => x.ConsumableId.HasValue).Select(x => x.ConsumableId!.Value).Distinct().ToArray();
        var assetIds = lines.Where(x => x.AssetId.HasValue).Select(x => x.AssetId!.Value).Distinct().ToArray();
        var componentIds = lines.Where(x => x.ComponentId.HasValue).Select(x => x.ComponentId!.Value).Distinct().ToArray();
        var accessories = await db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId && accessoryIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var consumables = await db.Consumables.AsNoTracking().Where(x => x.CompanyId == companyId && consumableIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var assets = await db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId && assetIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var components = await db.InventoryComponents.AsNoTracking().Where(x => x.CompanyId == companyId && componentIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var accessoryUsed = await db.AccessoryAssignments.AsNoTracking().Where(x => x.CompanyId == companyId && accessoryIds.Contains(x.AccessoryId))
            .GroupBy(x => x.AccessoryId).Select(g => new { Id = g.Key, Used = g.Sum(x => x.Quantity - x.ReturnedQuantity) }).ToDictionaryAsync(x => x.Id, x => x.Used, ct);
        var consumableUsed = await db.ConsumableMovements.AsNoTracking().Where(x => x.CompanyId == companyId && consumableIds.Contains(x.ConsumableId) && x.MovementType == "Checkout")
            .GroupBy(x => x.ConsumableId).Select(g => new { Id = g.Key, Used = g.Sum(x => x.Quantity) }).ToDictionaryAsync(x => x.Id, x => x.Used, ct);
        var componentUsed = await db.ComponentAllocations.AsNoTracking().Where(x => x.CompanyId == companyId && componentIds.Contains(x.ComponentId))
            .GroupBy(x => x.ComponentId).Select(g => new { Id = g.Key, Used = g.Sum(x => x.Quantity - x.RemovedQuantity) }).ToDictionaryAsync(x => x.Id, x => x.Used, ct);
        var summaries = new Dictionary<Guid, KitSummary>();
        foreach (var kit in kits)
        {
            var kitLines = lines.Where(x => x.AccessoryKitId == kit.Id).Select(line =>
            {
                var id = line.AccessoryId ?? line.ConsumableId ?? line.AssetId ?? line.ComponentId ?? Guid.Empty;
                var (name, model, number, quantity, requestable) = line.ItemType switch
                {
                    "Accessory" when accessories.TryGetValue(id, out var x) => (x.Name, x.ModelNumber, x.ItemNumber, x.TotalQuantity - accessoryUsed.GetValueOrDefault(id), x.IsRequestable),
                    "Consumable" when consumables.TryGetValue(id, out var x) => (x.Name, x.ModelNumber, x.ItemNumber, x.TotalQuantity - consumableUsed.GetValueOrDefault(id), x.IsRequestable),
                    "Asset" when assets.TryGetValue(id, out var x) => (x.Name, (string?)null, x.AssetTag, x.Status == AssetStatuses.Available && x.AssignedEmployeeId is null && x.DeletedAt is null ? 1 : 0, x.IsRequestable),
                    "Component" when components.TryGetValue(id, out var x) => (x.Name, x.ModelNumber, x.ItemNumber, x.TotalQuantity - componentUsed.GetValueOrDefault(id), true),
                    _ => ("Bulunamayan kalem", (string?)null, (string?)null, 0, false)
                };
                return new KitItemSummary(line.ItemType, id, line.AccessoryId, name, model, number, line.Quantity, Math.Max(0, quantity), requestable);
            }).ToArray();
            summaries[kit.Id] = new KitSummary(kit.Id, kit.Name, kitLines.Length, kitLines.Sum(x => x.Quantity),
                kitLines.Length == 0 ? 0 : kitLines.Min(x => x.Remaining / x.Quantity),
                kitLines.Select(x => new KitItemSummary(x.ItemType, x.ItemId, x.AccessoryId, x.ItemName, x.ModelNumber, x.ItemNumber, x.Quantity, x.Remaining, x.Requestable)).ToArray());
        }
        return summaries;
    }

    private static async Task<Dictionary<string, string[]>?> ValidateItemsAsync(AppDbContext db, Guid companyId, AccessoryKitItemRequest[] items, CancellationToken ct)
    {
        var accessoryIds = items.Where(x => x.ItemType is null or "Accessory").Select(x => (x.ItemId ?? x.AccessoryId)!.Value).ToArray();
        var consumableIds = items.Where(x => x.ItemType == "Consumable").Select(x => (x.ItemId ?? x.AccessoryId)!.Value).ToArray();
        var assetIds = items.Where(x => x.ItemType == "Asset").Select(x => (x.ItemId ?? x.AccessoryId)!.Value).ToArray();
        var componentIds = items.Where(x => x.ItemType == "Component").Select(x => (x.ItemId ?? x.AccessoryId)!.Value).ToArray();
        var valid = await db.Accessories.CountAsync(x => x.CompanyId == companyId && accessoryIds.Contains(x.Id), ct) == accessoryIds.Length;
        valid &= await db.Consumables.CountAsync(x => x.CompanyId == companyId && consumableIds.Contains(x.Id), ct) == consumableIds.Length;
        valid &= await db.Assets.CountAsync(x => x.CompanyId == companyId && assetIds.Contains(x.Id) && x.DeletedAt == null, ct) == assetIds.Length;
        valid &= await db.InventoryComponents.CountAsync(x => x.CompanyId == companyId && componentIds.Contains(x.Id), ct) == componentIds.Length;
        return valid ? null : new Dictionary<string, string[]> { ["items"] = ["Kit kalemlerinden biri seçilen şirkette bulunamadı veya kalem türü geçersiz."] };
    }

    private static bool TryCompanyId(ClaimsPrincipal user, out Guid companyId) =>
        Guid.TryParse(user.FindFirstValue("company_id"), out companyId);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? StripKitMarker(string? value, string marker) => value?.StartsWith(marker + " ", StringComparison.Ordinal) == true ? value[(marker.Length + 1)..] : value;

    public sealed record SaveAccessoryKitRequest(string Name, string? Description, AccessoryKitItemRequest[]? Items);
    public sealed record AccessoryKitItemRequest(Guid? AccessoryId, int Quantity, string? ItemType = null, Guid? ItemId = null);
    private sealed record KitItemSummary(string ItemType, Guid ItemId, Guid? AccessoryId, string ItemName, string? ModelNumber,
        string? ItemNumber, int Quantity, int Remaining, bool Requestable);
    private sealed record KitSummary(Guid Id, string Name, int ItemCount, int TotalUnits, int AvailableKits, KitItemSummary[] Items);
    private sealed record KitHistoryItem(Guid Id, string ItemType, string ItemName, string TargetType, string? Target, string? Actor,
        int Quantity, int ReturnedQuantity, DateTimeOffset CheckedOutAt, DateTimeOffset? LastReturnedAt, string? Notes)
    {
        public int Remaining => Math.Max(0, Quantity - ReturnedQuantity);
    }
}
