using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class CompanyTaskEndpoints
{
    private static readonly string[] Statuses = ["Open", "InProgress", "Completed", "Cancelled"];
    private static readonly string[] Priorities = ["Low", "Normal", "High", "Urgent"];

    public static IEndpointRouteBuilder MapCompanyTaskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api").RequireAuthorization();
        api.AddEndpointFilter(async (invocation, next) =>
        {
            var principal = invocation.HttpContext.User;
            if (!TryScope(principal, out var companyId, out var userId, out var role))
                return Results.Forbid();

            var db = invocation.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var activeMembership = await db.Companies.AsNoTracking().AnyAsync(company =>
                company.Id == companyId && company.IsActive && db.CompanyMemberships.Any(membership =>
                    membership.CompanyId == companyId && membership.UserId == userId && membership.RoleKey == role),
                invocation.HttpContext.RequestAborted);
            if (!activeMembership) return Results.Forbid();

            if (HttpMethods.IsGet(invocation.HttpContext.Request.Method) || HttpMethods.IsHead(invocation.HttpContext.Request.Method))
                return await next(invocation);

            var antiforgery = invocation.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            try { await antiforgery.ValidateRequestAsync(invocation.HttpContext); }
            catch (AntiforgeryValidationException)
            {
                return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            }
            return await next(invocation);
        });

        api.MapGet("/tasks", async ([FromQuery] string? q, [FromQuery] string? status,
            [FromQuery] string? priority, [FromQuery] Guid? assignedEmployeeId,
            ClaimsPrincipal principal, AppDbContext db, CancellationToken ct,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        {
            if (!TryScope(principal, out var companyId, out var userId, out var role)) return Results.Forbid();
            if (!string.IsNullOrWhiteSpace(status) && !Statuses.Contains(status, StringComparer.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Görev durumu geçerli değil."] });
            if (!string.IsNullOrWhiteSpace(priority) && !Priorities.Contains(priority, StringComparer.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["priority"] = ["Görev önceliği geçerli değil."] });

            var term = q?.Trim();
            if (term?.Length > 120)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 100);
            var canManage = CanManage(role);
            var visibleTasks = db.CompanyTasks.AsNoTracking().Where(task => task.CompanyId == companyId);
            if (!canManage)
            {
                var employeeId = await db.CompanyMemberships.AsNoTracking()
                    .Where(membership => membership.CompanyId == companyId && membership.UserId == userId)
                    .Select(membership => membership.EmployeeId).SingleOrDefaultAsync(ct);
                visibleTasks = employeeId is Guid id
                    ? visibleTasks.Where(task => task.AssignedEmployeeId == id)
                    : visibleTasks.Where(_ => false);
            }
            var summaryRows = await visibleTasks.GroupBy(task => task.Status)
                .Select(group => new { Status = group.Key, Count = group.Count() }).ToListAsync(ct);
            var summary = new
            {
                open = summaryRows.Where(row => row.Status == "Open").Select(row => row.Count).FirstOrDefault(),
                inProgress = summaryRows.Where(row => row.Status == "InProgress").Select(row => row.Count).FirstOrDefault(),
                completed = summaryRows.Where(row => row.Status == "Completed").Select(row => row.Count).FirstOrDefault(),
                cancelled = summaryRows.Where(row => row.Status == "Cancelled").Select(row => row.Count).FirstOrDefault()
            };
            var query = visibleTasks;
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(task => task.Status == status);
            if (!string.IsNullOrWhiteSpace(priority)) query = query.Where(task => task.Priority == priority);
            if (assignedEmployeeId is Guid assignedId) query = query.Where(task => task.AssignedEmployeeId == assignedId);
            if (!string.IsNullOrWhiteSpace(term))
                query = query.Where(task => EF.Functions.ILike(task.Title, $"%{term}%") ||
                    (task.Description != null && EF.Functions.ILike(task.Description, $"%{term}%")) ||
                    (task.AssignedEmployeeId != null && db.Employees.Any(employee => employee.CompanyId == companyId &&
                        employee.Id == task.AssignedEmployeeId &&
                        (EF.Functions.ILike(employee.DisplayName, $"%{term}%") || EF.Functions.ILike(employee.EmployeeNumber, $"%{term}%")))));

            var total = await query.CountAsync(ct);
            var items = await query.OrderBy(task => task.DueAt == null).ThenBy(task => task.DueAt)
                .ThenByDescending(task => task.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(task => new
                {
                    task.Id, task.Title, task.Description, task.Status, task.Priority,
                    task.AssignedEmployeeId, task.CreatedByUserId, task.DueAt,
                    task.CreatedAt, task.UpdatedAt, task.CompletedAt,
                    employeeName = db.Employees.Where(employee => employee.CompanyId == companyId &&
                        employee.Id == task.AssignedEmployeeId).Select(employee => employee.DisplayName).FirstOrDefault(),
                    employeeNumber = db.Employees.Where(employee => employee.CompanyId == companyId &&
                        employee.Id == task.AssignedEmployeeId).Select(employee => employee.EmployeeNumber).FirstOrDefault(),
                    createdBy = db.Users.Where(user => user.Id == task.CreatedByUserId)
                        .Select(user => user.DisplayName).FirstOrDefault()
                }).ToListAsync(ct);

            var employees = canManage
                ? await db.Employees.AsNoTracking().Where(employee => employee.CompanyId == companyId && employee.IsActive)
                    .OrderBy(employee => employee.DisplayName).Take(500)
                    .Select(employee => new { employee.Id, employee.DisplayName, employee.EmployeeNumber }).ToListAsync(ct)
                : await (from membership in db.CompanyMemberships.AsNoTracking()
                         join employee in db.Employees.AsNoTracking() on new { membership.CompanyId, Id = membership.EmployeeId }
                             equals new { employee.CompanyId, Id = (Guid?)employee.Id }
                         where membership.CompanyId == companyId && membership.UserId == userId && employee.IsActive
                         select new { employee.Id, employee.DisplayName, employee.EmployeeNumber }).ToListAsync(ct);

            return Results.Ok(new { items, employees, page, pageSize, total, canManage, summary });
        }).RequireAuthorization("company.member");

        api.MapPost("/tasks", async ([FromBody] CompanyTaskWriteRequest request,
            ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryScope(principal, out var companyId, out var userId, out _)) return Results.Forbid();
            var validation = Validate(request);
            if (validation.Count > 0) return Results.ValidationProblem(validation);
            if (request.AssignedEmployeeId is Guid employeeId && !await db.Employees.AsNoTracking()
                    .AnyAsync(employee => employee.CompanyId == companyId && employee.Id == employeeId && employee.IsActive, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assignedEmployeeId"] = ["Atanan çalışan bu şirkette etkin değil."] });

            var now = DateTimeOffset.UtcNow;
            var task = new CompanyTask
            {
                CompanyId = companyId,
                Title = request.Title.Trim(),
                Description = Clean(request.Description),
                Priority = request.Priority.Trim(),
                AssignedEmployeeId = request.AssignedEmployeeId,
                DueAt = request.DueAt?.ToUniversalTime(),
                CreatedByUserId = userId,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.CompanyTasks.Add(task);
            AddAudit(db, companyId, userId, task, "TaskCreated", $"Görev oluşturuldu · {task.Title}");
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/tasks/{task.Id}", new { task.Id, task.Title, task.Status, task.Priority, task.DueAt });
        }).RequireAuthorization("people.manage");

        api.MapPut("/tasks/{id:guid}", async (Guid id, [FromBody] CompanyTaskWriteRequest request,
            ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryScope(principal, out var companyId, out var userId, out _)) return Results.Forbid();
            var validation = Validate(request);
            if (validation.Count > 0) return Results.ValidationProblem(validation);
            if (request.AssignedEmployeeId is Guid employeeId && !await db.Employees.AsNoTracking()
                    .AnyAsync(employee => employee.CompanyId == companyId && employee.Id == employeeId && employee.IsActive, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assignedEmployeeId"] = ["Atanan çalışan bu şirkette etkin değil."] });

            var task = await db.CompanyTasks.SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == id, ct);
            if (task is null) return Results.NotFound();
            task.Title = request.Title.Trim();
            task.Description = Clean(request.Description);
            task.Priority = request.Priority.Trim();
            task.AssignedEmployeeId = request.AssignedEmployeeId;
            task.DueAt = request.DueAt?.ToUniversalTime();
            task.UpdatedAt = DateTimeOffset.UtcNow;
            AddAudit(db, companyId, userId, task, "TaskUpdated", $"Görev güncellendi · {task.Title}");
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { task.Id, task.Title, task.Status, task.Priority, task.DueAt, task.UpdatedAt });
        }).RequireAuthorization("people.manage");

        api.MapPatch("/tasks/{id:guid}/status", async (Guid id, [FromBody] CompanyTaskStatusRequest request,
            ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryScope(principal, out var companyId, out var userId, out var role)) return Results.Forbid();
            if (!Statuses.Contains(request.Status, StringComparer.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Görev durumu geçerli değil."] });
            var task = await db.CompanyTasks.SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == id, ct);
            if (task is null) return Results.NotFound();
            if (!CanManage(role))
            {
                var assignedToCurrentUser = task.AssignedEmployeeId is Guid assignedId &&
                    await db.CompanyMemberships.AsNoTracking().AnyAsync(membership =>
                        membership.CompanyId == companyId && membership.UserId == userId && membership.EmployeeId == assignedId, ct);
                var validTransition = task.Status == "Open" && request.Status == "InProgress" ||
                    task.Status == "InProgress" && request.Status == "Completed";
                if (!assignedToCurrentUser || !validTransition) return Results.Forbid();
            }

            if (task.Status != request.Status)
            {
                task.Status = request.Status;
                task.CompletedAt = request.Status == "Completed" ? DateTimeOffset.UtcNow : null;
                task.UpdatedAt = DateTimeOffset.UtcNow;
                AddAudit(db, companyId, userId, task, "TaskStatusChanged", $"Görev durumu değişti · {task.Title} · {task.Status}");
                await db.SaveChangesAsync(ct);
            }
            return Results.Ok(new { task.Id, task.Status, task.CompletedAt, task.UpdatedAt });
        }).RequireAuthorization("company.member");

        return endpoints;
    }

    private static Dictionary<string, string[]> Validate(CompanyTaskWriteRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var title = request.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 160)
            errors["title"] = ["Görev başlığı 1–160 karakter olmalıdır."];
        if (request.Description?.Length > 4000)
            errors["description"] = ["Görev açıklaması 4.000 karakteri aşamaz."];
        if (string.IsNullOrWhiteSpace(request.Priority) || !Priorities.Contains(request.Priority.Trim(), StringComparer.Ordinal))
            errors["priority"] = ["Görev önceliği geçerli değil."];
        return errors;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool TryScope(ClaimsPrincipal principal, out Guid companyId, out Guid userId, out string role)
    {
        var hasCompany = Guid.TryParse(principal.FindFirstValue("company_id"), out companyId);
        var hasUser = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        role = principal.FindFirstValue("company_role") ?? string.Empty;
        return hasCompany && hasUser && !string.IsNullOrWhiteSpace(role);
    }

    private static bool CanManage(string role) => role is "owner" or "admin" or "asset_manager" or "hr_operator";

    private static void AddAudit(AppDbContext db, Guid companyId, Guid userId, CompanyTask task, string action, string details) =>
        db.AuditEntries.Add(new AuditEntry
        {
            CompanyId = companyId,
            ActorUserId = userId,
            TargetType = "CompanyTask",
            TargetId = task.Id,
            Action = action,
            Details = details.Length <= 2000 ? details : details[..2000]
        });
}

public sealed record CompanyTaskWriteRequest(
    string Title,
    string? Description,
    string Priority,
    Guid? AssignedEmployeeId,
    DateTimeOffset? DueAt);

public sealed record CompanyTaskStatusRequest(string Status);
