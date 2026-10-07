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
            var lines = await (from line in db.AccessoryKitLines.AsNoTracking()
                               join accessory in db.Accessories.AsNoTracking()
                                   on new { line.CompanyId, Id = line.AccessoryId } equals new { accessory.CompanyId, Id = accessory.Id }
                               where line.CompanyId == companyId && kitIds.Contains(line.AccessoryKitId)
                               orderby accessory.Name
                               select new { line.AccessoryKitId, line.AccessoryId, line.Quantity, accessory.Name,
                                   accessory.ModelNumber, accessory.ItemNumber, accessory.TotalQuantity })
                .ToListAsync(ct);
            var accessoryIds = lines.Select(x => x.AccessoryId).Distinct().ToArray();
            var checkedOut = await db.AccessoryAssignments.AsNoTracking()
                .Where(x => x.CompanyId == companyId && accessoryIds.Contains(x.AccessoryId))
                .GroupBy(x => x.AccessoryId)
                .Select(group => new { AccessoryId = group.Key, Quantity = group.Sum(x => x.Quantity - x.ReturnedQuantity) })
                .ToDictionaryAsync(x => x.AccessoryId, x => x.Quantity, ct);

            var results = kits.Select(kit =>
            {
                var items = lines.Where(x => x.AccessoryKitId == kit.Id).Select(line => new
                {
                    line.AccessoryId,
                    accessoryName = line.Name,
                    line.ModelNumber,
                    line.ItemNumber,
                    quantity = line.Quantity,
                    remaining = Math.Max(0, line.TotalQuantity - checkedOut.GetValueOrDefault(line.AccessoryId))
                }).ToArray();
                var availableKits = items.Length == 0 ? 0 : items.Min(x => x.remaining / x.quantity);
                return new
                {
                    kit.Id, kit.Name, kit.Description, kit.IsActive, kit.CreatedAt, kit.UpdatedAt,
                    itemCount = items.Length, totalUnits = items.Sum(x => x.quantity), availableKits, items
                };
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
            var lines = await (from line in db.AccessoryKitLines.AsNoTracking()
                               join accessory in db.Accessories.AsNoTracking()
                                   on new { line.CompanyId, Id = line.AccessoryId } equals new { accessory.CompanyId, Id = accessory.Id }
                               where line.CompanyId == companyId && kitIds.Contains(line.AccessoryKitId)
                               orderby accessory.Name
                               select new { line.AccessoryKitId, line.Quantity, accessory.Name, accessory.TotalQuantity, line.AccessoryId })
                .ToListAsync(ct);
            if (lines.Count > 100000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["export"] = ["CSV dosyası en fazla 100.000 kit kalemini destekler; aramayı daraltın."] });
            var accessoryIds = lines.Select(x => x.AccessoryId).Distinct().ToArray();
            var checkedOut = await db.AccessoryAssignments.AsNoTracking()
                .Where(x => x.CompanyId == companyId && accessoryIds.Contains(x.AccessoryId))
                .GroupBy(x => x.AccessoryId)
                .Select(group => new { AccessoryId = group.Key, Quantity = group.Sum(x => x.Quantity - x.ReturnedQuantity) })
                .ToDictionaryAsync(x => x.AccessoryId, x => x.Quantity, ct);

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
                var kitLines = lines.Where(x => x.AccessoryKitId == kit.Id).ToArray();
                var content = string.Join(" | ", kitLines.Select(x => $"{x.Name} × {x.Quantity}"));
                var available = kitLines.Length == 0 ? 0 : kitLines.Min(x => Math.Max(0, x.TotalQuantity - checkedOut.GetValueOrDefault(x.AccessoryId)) / x.Quantity);
                csv.AppendJoin(',', new[] { Cell(kit.Name), Cell(kit.Description), Cell(kit.IsActive ? "Etkin" : "Arşivde"),
                    Cell(content), kitLines.Sum(x => x.Quantity).ToString(System.Globalization.CultureInfo.InvariantCulture),
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
            var items = history.Select(x => new
            {
                x.Id, accessoryName = x.Name, x.TargetType,
                target = x.EmployeeId is Guid employeeId ? employees.GetValueOrDefault(employeeId)
                    : x.AssetId is Guid assetId ? assets.GetValueOrDefault(assetId) : x.Location,
                actor = x.ActorUserId is Guid actorId ? actors.GetValueOrDefault(actorId) : null,
                x.Quantity, x.ReturnedQuantity, remaining = x.Quantity - x.ReturnedQuantity,
                x.CheckedOutAt, x.LastReturnedAt,
                notes = x.Notes!.StartsWith(marker + " ") ? x.Notes[(marker.Length + 1)..] : null
            });
            return Results.Ok(new { kitId = kit.Id, kitName = kit.Name, items, total = history.Count });
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
            var accessoryIds = request.Items!.Select(x => x.AccessoryId).ToArray();
            if (await db.Accessories.CountAsync(x => x.CompanyId == companyId && accessoryIds.Contains(x.Id), ct) != accessoryIds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Seçilen aksesuarlardan biri bu şirkette bulunamadı."] });

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
            var accessoryIds = request.Items!.Select(x => x.AccessoryId).ToArray();
            if (await db.Accessories.CountAsync(x => x.CompanyId == companyId && accessoryIds.Contains(x.Id), ct) != accessoryIds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Seçilen aksesuarlardan biri bu şirkette bulunamadı."] });

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            kit.Name = name;
            kit.Description = Clean(request.Description);
            kit.UpdatedAt = DateTimeOffset.UtcNow;
            var previousLines = await db.AccessoryKitLines.Where(x => x.CompanyId == companyId && x.AccessoryKitId == id).ToListAsync(ct);
            var requestedByAccessory = request.Items!.ToDictionary(x => x.AccessoryId, x => x.Quantity);
            foreach (var line in previousLines)
            {
                if (requestedByAccessory.Remove(line.AccessoryId, out var quantity)) line.Quantity = quantity;
                else db.AccessoryKitLines.Remove(line);
            }
            AddLines(db, companyId, kit.Id, requestedByAccessory.Select(x => new AccessoryKitItemRequest(x.Key, x.Value)).ToArray());
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
        if (items.Length is < 1 or > 50 || items.Any(x => x.AccessoryId == Guid.Empty || x.Quantity is < 1 or > 100000) ||
            items.Select(x => x.AccessoryId).Distinct().Count() != items.Length)
            errors["items"] = ["Kitte 1–50 arasında tekrarsız aksesuar bulunmalı; her satırın adedi 1–100.000 olmalı."];
        return errors;
    }

    private static void AddLines(AppDbContext db, Guid companyId, Guid kitId, AccessoryKitItemRequest[] items)
    {
        foreach (var item in items)
            db.AccessoryKitLines.Add(new AccessoryKitLine
            {
                CompanyId = companyId, AccessoryKitId = kitId,
                AccessoryId = item.AccessoryId, Quantity = item.Quantity
            });
    }

    private static bool TryCompanyId(ClaimsPrincipal user, out Guid companyId) =>
        Guid.TryParse(user.FindFirstValue("company_id"), out companyId);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed record SaveAccessoryKitRequest(string Name, string? Description, AccessoryKitItemRequest[]? Items);
    public sealed record AccessoryKitItemRequest(Guid AccessoryId, int Quantity);
}
