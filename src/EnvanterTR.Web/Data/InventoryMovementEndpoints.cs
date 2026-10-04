using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class InventoryMovementEndpoints
{
    public static RouteGroupBuilder MapInventoryMovementEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/inventory/movements", async ([FromQuery] string? q, [FromQuery] string? kind,
            [FromQuery] string? movementType, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
            [FromQuery] int page, [FromQuery] int pageSize, ClaimsPrincipal user,
            AppDbContext db, CancellationToken ct) =>
        {
            if (!Guid.TryParse(user.FindFirstValue("company_id"), out var companyId)) return Results.Forbid();
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            if (!string.IsNullOrWhiteSpace(q) && q.Trim().Length > 120)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama 120 karakteri aşamaz."] });
            if (kind is not (null or "accessory" or "consumable" or "component"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["kind"] = ["Geçerli bir stok türü seçin."] });
            if (movementType is not (null or "Received" or "Adjustment" or "Checkout" or "Checkin"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["movementType"] = ["Geçerli bir hareket türü seçin."] });
            if (from is { Year: < 1970 or > 2100 } || to is { Year: < 1970 or > 2100 } || (from.HasValue && to.HasValue && from > to))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["date"] = ["Tarih aralığı 1970–2100 arasında ve başlangıç bitişten önce olmalı."] });

            IQueryable<InventoryMovementRow>? query = null;
            if (kind is null or "accessory")
                query = db.AccessoryMovements.AsNoTracking().Where(x => x.CompanyId == companyId)
                    .Select(x => new InventoryMovementRow(x.Id, "Aksesuar", "accessory",
                        db.Accessories.Where(i => i.CompanyId == companyId && i.Id == x.AccessoryId).Select(i => i.Name).FirstOrDefault() ?? "Silinmiş kayıt",
                        x.MovementType, x.Quantity,
                        db.AccessoryAssignments.Where(a => a.CompanyId == companyId && a.Id == x.AssignmentId)
                            .Select(a => a.TargetType == "Employee"
                                ? db.Employees.Where(e => e.CompanyId == companyId && e.Id == a.EmployeeId).Select(e => e.DisplayName).FirstOrDefault()
                                : a.TargetType == "Asset"
                                    ? db.Assets.Where(asset => asset.CompanyId == companyId && asset.Id == a.AssetId).Select(asset => asset.AssetTag + " · " + asset.Name).FirstOrDefault()
                                    : a.Location).FirstOrDefault()
                            ?? db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                        db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.DisplayName).FirstOrDefault(), x.Notes, x.OccurredAt));
            if (kind is null or "consumable")
            {
                var consumables = db.ConsumableMovements.AsNoTracking().Where(x => x.CompanyId == companyId)
                    .Select(x => new InventoryMovementRow(x.Id, "Sarf", "consumable",
                        db.Consumables.Where(i => i.CompanyId == companyId && i.Id == x.ConsumableId).Select(i => i.Name).FirstOrDefault() ?? "Silinmiş kayıt",
                        x.MovementType, x.Quantity,
                        db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                        db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.DisplayName).FirstOrDefault(), x.Notes, x.OccurredAt));
                query = query is null ? consumables : query.Concat(consumables);
            }
            if (kind is null or "component")
            {
                var components = db.ComponentMovements.AsNoTracking().Where(x => x.CompanyId == companyId)
                    .Select(x => new InventoryMovementRow(x.Id, "Bileşen", "component",
                        db.InventoryComponents.Where(i => i.CompanyId == companyId && i.Id == x.ComponentId).Select(i => i.Name).FirstOrDefault() ?? "Silinmiş kayıt",
                        x.MovementType, x.Quantity,
                        db.Assets.Where(a => a.CompanyId == companyId && a.Id == x.AssetId).Select(a => a.AssetTag + " · " + a.Name).FirstOrDefault(),
                        db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.DisplayName).FirstOrDefault(), x.Notes, x.OccurredAt));
                query = query is null ? components : query.Concat(components);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query!.Where(x => x.Item.Contains(term) || (x.Target != null && x.Target.Contains(term)) || (x.Notes != null && x.Notes.Contains(term)));
            }
            if (movementType is not null) query = query!.Where(x => x.MovementType == movementType);
            if (from.HasValue) query = query!.Where(x => x.OccurredAt >= new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
            if (to.HasValue && to.Value < DateOnly.MaxValue) query = query!.Where(x => x.OccurredAt < new DateTimeOffset(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
            var total = await query!.CountAsync(ct);
            var items = await query!.OrderByDescending(x => x.OccurredAt).ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return Results.Ok(new { items, page, pageSize, total });
        }).RequireAuthorization("asset.read");
        return api;
    }

    private sealed record InventoryMovementRow(Guid Id, string KindLabel, string Kind, string Item,
        string MovementType, int Quantity, string? Target, string? Actor, string? Notes, DateTimeOffset OccurredAt);
}
