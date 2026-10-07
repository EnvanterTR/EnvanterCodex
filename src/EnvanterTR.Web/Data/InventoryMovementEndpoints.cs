using System.Security.Claims;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class InventoryMovementEndpoints
{
    public static RouteGroupBuilder MapInventoryMovementEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/inventory/movements", async ([FromQuery] string? q, [FromQuery] string? kind,
            [FromQuery] string? movementType, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct,
            [FromQuery] bool export = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
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

            // Keep the union in a server-translatable anonymous projection. Projecting
            // directly into the presentation record before Concat makes EF Core unable
            // to translate the set operation for the combined inventory types.
            var query = db.AccessoryMovements.AsNoTracking().Where(x => x.CompanyId == companyId && false)
                .Select(x => new
                {
                    x.Id,
                    KindLabel = "Aksesuar",
                    Kind = "accessory",
                    Item = db.Accessories.Where(i => i.CompanyId == companyId && i.Id == x.AccessoryId).Select(i => i.Name).FirstOrDefault() ?? "Silinmiş kayıt",
                    x.MovementType,
                    x.Quantity,
                    Target = db.AccessoryAssignments.Where(a => a.CompanyId == companyId && a.Id == x.AssignmentId)
                        .Select(a => a.TargetType == "Employee"
                            ? db.Employees.Where(e => e.CompanyId == companyId && e.Id == a.EmployeeId).Select(e => e.DisplayName).FirstOrDefault()
                            : a.TargetType == "Asset"
                                ? db.Assets.Where(asset => asset.CompanyId == companyId && asset.Id == a.AssetId).Select(asset => asset.AssetTag + " · " + asset.Name).FirstOrDefault()
                                : a.Location).FirstOrDefault()
                        ?? db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                    Actor = db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.DisplayName).FirstOrDefault(),
                    x.Notes,
                    x.OccurredAt
                });
            if (kind is null or "accessory")
                query = query.Concat(db.AccessoryMovements.AsNoTracking().Where(x => x.CompanyId == companyId)
                    .Select(x => new
                    {
                        x.Id,
                        KindLabel = "Aksesuar",
                        Kind = "accessory",
                        Item = db.Accessories.Where(i => i.CompanyId == companyId && i.Id == x.AccessoryId).Select(i => i.Name).FirstOrDefault() ?? "Silinmiş kayıt",
                        x.MovementType,
                        x.Quantity,
                        Target = db.AccessoryAssignments.Where(a => a.CompanyId == companyId && a.Id == x.AssignmentId)
                            .Select(a => a.TargetType == "Employee"
                                ? db.Employees.Where(e => e.CompanyId == companyId && e.Id == a.EmployeeId).Select(e => e.DisplayName).FirstOrDefault()
                                : a.TargetType == "Asset"
                                    ? db.Assets.Where(asset => asset.CompanyId == companyId && asset.Id == a.AssetId).Select(asset => asset.AssetTag + " · " + asset.Name).FirstOrDefault()
                                    : a.Location).FirstOrDefault()
                            ?? db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                        Actor = db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.DisplayName).FirstOrDefault(),
                        x.Notes,
                        x.OccurredAt
                    }));
            if (kind is null or "consumable")
            {
                var consumables = db.ConsumableMovements.AsNoTracking().Where(x => x.CompanyId == companyId)
                    .Select(x => new
                    {
                        x.Id,
                        KindLabel = "Sarf",
                        Kind = "consumable",
                        Item = db.Consumables.Where(i => i.CompanyId == companyId && i.Id == x.ConsumableId).Select(i => i.Name).FirstOrDefault() ?? "Silinmiş kayıt",
                        x.MovementType,
                        x.Quantity,
                        Target = db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                        Actor = db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.DisplayName).FirstOrDefault(),
                        x.Notes,
                        x.OccurredAt
                    });
                query = query.Concat(consumables);
            }
            if (kind is null or "component")
            {
                var components = db.ComponentMovements.AsNoTracking().Where(x => x.CompanyId == companyId)
                    .Select(x => new
                    {
                        x.Id,
                        KindLabel = "Bileşen",
                        Kind = "component",
                        Item = db.InventoryComponents.Where(i => i.CompanyId == companyId && i.Id == x.ComponentId).Select(i => i.Name).FirstOrDefault() ?? "Silinmiş kayıt",
                        x.MovementType,
                        x.Quantity,
                        Target = db.Assets.Where(a => a.CompanyId == companyId && a.Id == x.AssetId).Select(a => a.AssetTag + " · " + a.Name).FirstOrDefault(),
                        Actor = db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.DisplayName).FirstOrDefault(),
                        x.Notes,
                        x.OccurredAt
                    });
                query = query.Concat(components);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query!.Where(x => x.Item.Contains(term) || (x.Target != null && x.Target.Contains(term)) || (x.Actor != null && x.Actor.Contains(term)) || (x.Notes != null && x.Notes.Contains(term)));
            }
            if (movementType is not null) query = query!.Where(x => x.MovementType == movementType);
            if (from.HasValue) query = query!.Where(x => x.OccurredAt >= new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
            if (to.HasValue && to.Value < DateOnly.MaxValue) query = query!.Where(x => x.OccurredAt < new DateTimeOffset(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
            if (export)
            {
                if (await query!.CountAsync(ct) > 10000)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["export"] = ["Dışa aktarım en fazla 10.000 satır içerir; filtreyi daraltın."] });
                var rows = await query.OrderByDescending(x => x.OccurredAt).ThenBy(x => x.Id)
                    .Select(x => new InventoryMovementRow(x.Id, x.KindLabel, x.Kind, x.Item, x.MovementType, x.Quantity, x.Target, x.Actor, x.Notes, x.OccurredAt))
                    .ToListAsync(ct);
                var csv = new StringBuilder("\uFEFF\"Tarih (UTC)\",\"Tür\",\"Ürün\",\"Hareket\",\"Miktar\",\"Hedef / çalışan\",\"İşlemi yapan\",\"Not\"\r\n");
                foreach (var row in rows)
                    csv.AppendJoin(',', new[] { row.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), row.KindLabel, row.Item,
                        row.MovementType, row.Quantity.ToString(CultureInfo.InvariantCulture), row.Target, row.Actor, row.Notes }.Select(CsvCell)).Append("\r\n");
                return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"stok-hareketleri-{DateTime.UtcNow:yyyyMMdd}.csv");
            }
            var total = await query.CountAsync(ct);
            var items = await query.OrderByDescending(x => x.OccurredAt).ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new InventoryMovementRow(x.Id, x.KindLabel, x.Kind, x.Item, x.MovementType, x.Quantity, x.Target, x.Actor, x.Notes, x.OccurredAt))
                .ToListAsync(ct);
            return Results.Ok(new { items, page, pageSize, total });
        }).RequireAuthorization("asset.read");
        return api;
    }

    private sealed record InventoryMovementRow(Guid Id, string KindLabel, string Kind, string Item,
        string MovementType, int Quantity, string? Target, string? Actor, string? Notes, DateTimeOffset OccurredAt);

    private static string CsvCell(string? value)
    {
        var safe = value?.Trim() ?? "";
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@' or '\t' or '\r') safe = "'" + safe;
        return "\"" + safe.Replace("\"", "\"\"") + "\"";
    }
}
