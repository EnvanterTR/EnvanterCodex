using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class SoftwareLicenseEndpoints
{
    public static RouteGroupBuilder MapSoftwareLicenseEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/software-licenses", async ([FromQuery] string? q, [FromQuery] int page, [FromQuery] int pageSize,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 200);
            var query = db.SoftwareLicenses.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(q)) { var term = q.Trim(); if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string,string[]> { ["q"]=["Arama metni 120 karakteri aşamaz."] }); query = query.Where(x => x.Name.Contains(term) || (x.Publisher != null && x.Publisher.Contains(term)) || (x.Supplier != null && x.Supplier.Contains(term)) || (x.LicenseReference != null && x.LicenseReference.Contains(term))); }
            var total = await query.CountAsync(ct);
            var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
            {
                x.Id, x.Name, x.Publisher, x.ProductVersion, x.LicenseType, x.LicenseReference, x.Supplier, x.Seats,
                x.UnitCost, x.Currency, x.PurchaseDate, x.ExpirationDate, x.AutoRenew, x.UpdatedAt,
                assigned = db.SoftwareLicenseAssignments.Count(a => a.CompanyId == companyId && a.LicenseId == x.Id && a.RevokedAt == null)
            }).ToListAsync(ct);
            return Results.Ok(new { items, page, pageSize, total });
        }).RequireAuthorization("asset.read");

        api.MapGet("/software-licenses/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            var license = await db.SoftwareLicenses.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id).Select(x => new
            {
                x.Id, x.Name, x.Publisher, x.ProductVersion, x.LicenseType, x.LicenseReference, x.Supplier, x.Seats, x.UnitCost, x.Currency,
                x.PurchaseDate, x.ExpirationDate, x.AutoRenew, x.Notes, x.UpdatedAt,
                assigned = db.SoftwareLicenseAssignments.Count(a => a.CompanyId == companyId && a.LicenseId == x.Id && a.RevokedAt == null)
            }).SingleOrDefaultAsync(ct);
            if (license is null) return Results.NotFound();
            var assignments = await db.SoftwareLicenseAssignments.AsNoTracking().Where(a => a.CompanyId == companyId && a.LicenseId == id)
                .OrderBy(a => a.RevokedAt != null).ThenByDescending(a => a.AssignedAt).Take(200).Select(a => new
                { a.Id, a.EmployeeId, employee = db.Employees.Where(e => e.CompanyId == companyId && e.Id == a.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                    employeeNumber = db.Employees.Where(e => e.CompanyId == companyId && e.Id == a.EmployeeId).Select(e => e.EmployeeNumber).FirstOrDefault(),
                    a.AssignedAt, a.RevokedAt, a.Notes }).ToListAsync(ct);
            var events = await db.SoftwareLicenseEvents.AsNoTracking().Where(e => e.CompanyId == companyId && e.LicenseId == id)
                .OrderByDescending(e => e.OccurredAt).Take(100).Select(e => new { e.EventType, e.Details, e.OccurredAt,
                    employee = db.Employees.Where(p => p.CompanyId == companyId && p.Id == e.EmployeeId).Select(p => p.DisplayName).FirstOrDefault() }).ToListAsync(ct);
            return Results.Ok(new { license, assignments, events });
        }).RequireAuthorization("asset.read");

        api.MapPost("/software-licenses", async (LicenseRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            var errors = Validate(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var item = new SoftwareLicense { CompanyId = companyId, Name = request.Name.Trim(), Publisher = Clean(request.Publisher), ProductVersion = Clean(request.ProductVersion),
                LicenseType = request.LicenseType, LicenseReference = Clean(request.LicenseReference), Supplier = Clean(request.Supplier), Seats = request.Seats,
                UnitCost = request.UnitCost, Currency = request.Currency.Trim().ToUpperInvariant(), PurchaseDate = request.PurchaseDate, ExpirationDate = request.ExpirationDate,
                AutoRenew = request.AutoRenew, Notes = Clean(request.Notes) };
            db.SoftwareLicenses.Add(item); AddEvent(db, item, user, "Created", "Lisans kaydı oluşturuldu.");
            await db.SaveChangesAsync(ct); return Results.Created($"/api/software-licenses/{item.Id}", new { item.Id });
        }).RequireAuthorization("asset.write");

        api.MapPut("/software-licenses/{id:guid}", async (Guid id, LicenseRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            var item = await db.SoftwareLicenses.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct); if (item is null) return Results.NotFound();
            var errors = Validate(request); var active = await db.SoftwareLicenseAssignments.CountAsync(a => a.CompanyId == companyId && a.LicenseId == id && a.RevokedAt == null, ct);
            if (request.Seats < active) errors["seats"] = [$"Koltuk sayısı mevcut {active} atamadan düşük olamaz."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            item.Name = request.Name.Trim(); item.Publisher = Clean(request.Publisher); item.ProductVersion = Clean(request.ProductVersion); item.LicenseType = request.LicenseType;
            item.LicenseReference = Clean(request.LicenseReference); item.Supplier = Clean(request.Supplier); item.Seats = request.Seats; item.UnitCost = request.UnitCost;
            item.Currency = request.Currency.Trim().ToUpperInvariant(); item.PurchaseDate = request.PurchaseDate; item.ExpirationDate = request.ExpirationDate;
            item.AutoRenew = request.AutoRenew; item.Notes = Clean(request.Notes); item.UpdatedAt = DateTimeOffset.UtcNow;
            AddEvent(db, item, user, "Updated", "Lisans bilgileri güncellendi."); await db.SaveChangesAsync(ct); return Results.NoContent();
        }).RequireAuthorization("asset.write");

        api.MapPost("/software-licenses/{id:guid}/assign", async (Guid id, AssignmentRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var item = await db.SoftwareLicenses.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct); if (item is null) return Results.NotFound();
            if (!await db.Employees.AnyAsync(e => e.CompanyId == companyId && e.Id == request.EmployeeId && e.IsActive, ct))
                return Results.ValidationProblem(new Dictionary<string,string[]> { ["employeeId"]=["Aktif çalışan seçilmelidir."] });
            if (await db.SoftwareLicenseAssignments.AnyAsync(a => a.CompanyId == companyId && a.LicenseId == id && a.EmployeeId == request.EmployeeId && a.RevokedAt == null, ct))
                return Results.Conflict(new { message = "Bu lisans çalışana zaten atanmış." });
            var count = await db.SoftwareLicenseAssignments.CountAsync(a => a.CompanyId == companyId && a.LicenseId == id && a.RevokedAt == null, ct);
            if (count >= item.Seats) return Results.Conflict(new { message = "Kullanılabilir lisans koltuğu kalmadı." });
            var assignment = new SoftwareLicenseAssignment { CompanyId = companyId, LicenseId = id, EmployeeId = request.EmployeeId, ActorUserId = UserId(user), Notes = Clean(request.Notes) };
            db.SoftwareLicenseAssignments.Add(assignment); AddEvent(db, item, user, "Assigned", $"Çalışana atandı: {request.EmployeeId}", request.EmployeeId);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Results.Created($"/api/software-licenses/{id}", new { assignment.Id, assigned = count + 1, available = item.Seats - count - 1 });
        }).RequireAuthorization("asset.write");

        api.MapPost("/software-licenses/{id:guid}/revoke", async (Guid id, AssignmentRevokeRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Company(user, out var companyId)) return Results.Forbid();
            var item = await db.SoftwareLicenses.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct); if (item is null) return Results.NotFound();
            var assignment = await db.SoftwareLicenseAssignments.SingleOrDefaultAsync(a => a.CompanyId == companyId && a.LicenseId == id && a.Id == request.AssignmentId && a.RevokedAt == null, ct);
            if (assignment is null) return Results.NotFound();
            assignment.RevokedAt = DateTimeOffset.UtcNow; AddEvent(db, item, user, "Revoked", "Lisans ataması kaldırıldı.", assignment.EmployeeId);
            await db.SaveChangesAsync(ct); return Results.NoContent();
        }).RequireAuthorization("asset.write");
        return api;
    }

    private static bool Company(ClaimsPrincipal user, out Guid id) => Guid.TryParse(user.FindFirstValue("company_id"), out id);
    private static Guid? UserId(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static void AddEvent(AppDbContext db, SoftwareLicense license, ClaimsPrincipal user, string type, string details, Guid? employeeId = null) => db.SoftwareLicenseEvents.Add(new SoftwareLicenseEvent { CompanyId = license.CompanyId, LicenseId = license.Id, ActorUserId = UserId(user), EmployeeId = employeeId, EventType = type, Details = details });
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
        return e;
    }
    public sealed record LicenseRequest(string Name, string? Publisher, string? ProductVersion, string LicenseType, string? LicenseReference,
        string? Supplier, int Seats, decimal? UnitCost, string Currency, DateOnly? PurchaseDate, DateOnly? ExpirationDate, bool AutoRenew, string? Notes);
    public sealed record AssignmentRequest(Guid EmployeeId, string? Notes);
    public sealed record AssignmentRevokeRequest(Guid AssignmentId);
}
