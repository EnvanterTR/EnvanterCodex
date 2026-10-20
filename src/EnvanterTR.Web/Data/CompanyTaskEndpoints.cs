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
            var employeeScope = await GetEmployeeScopeAsync(db, companyId, userId, ct);
            visibleTasks = ApplyEmployeeScope(db, companyId, visibleTasks, employeeScope);
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
                    task.AssignedEmployeeId,
                    CreatedByUserId = (Guid?)task.CreatedByUserId,
                    task.DueAt,
                    task.CreatedAt, task.UpdatedAt, task.CompletedAt,
                    employeeName = db.Employees.Where(employee => employee.CompanyId == companyId &&
                        employee.Id == task.AssignedEmployeeId).Select(employee => employee.DisplayName).FirstOrDefault(),
                    employeeNumber = db.Employees.Where(employee => employee.CompanyId == companyId &&
                        employee.Id == task.AssignedEmployeeId).Select(employee => employee.EmployeeNumber).FirstOrDefault(),
                    createdBy = db.Users.Where(user => user.Id == task.CreatedByUserId)
                        .Select(user => user.DisplayName).FirstOrDefault()
                }).ToListAsync(ct);

            if (employeeScope.IsRestricted && items.Count > 0)
            {
                var creatorIds = items.Select(item => item.CreatedByUserId).Where(id => id.HasValue)
                    .Select(id => id!.Value).Distinct().ToArray();
                if (creatorIds.Length > 0)
                {
                    var creatorMemberships = await db.CompanyMemberships.AsNoTracking()
                        .Where(membership => membership.CompanyId == companyId && creatorIds.Contains(membership.UserId) &&
                            membership.EmployeeId.HasValue)
                        .Select(membership => new { membership.UserId, EmployeeId = membership.EmployeeId!.Value })
                        .ToListAsync(ct);
                    if (creatorMemberships.Count > 0)
                    {
                        var creatorEmployeeIds = creatorMemberships.Select(membership => membership.EmployeeId).Distinct().ToArray();
                        var visibleEmployeeIds = await employeeScope.Apply(
                            db.Employees.AsNoTracking().Where(employee => employee.CompanyId == companyId && employee.IsActive &&
                                creatorEmployeeIds.Contains(employee.Id)))
                            .Select(employee => employee.Id).ToListAsync(ct);
                        var visibleEmployeeIdSet = visibleEmployeeIds.ToHashSet();
                        var hiddenCreatorIds = creatorMemberships.Where(membership => !visibleEmployeeIdSet.Contains(membership.EmployeeId))
                            .Select(membership => membership.UserId).ToHashSet();
                        items = items.Select(item => item.CreatedByUserId is Guid creatorId && hiddenCreatorIds.Contains(creatorId)
                            ? new
                            {
                                item.Id, item.Title, item.Description, item.Status, item.Priority, item.AssignedEmployeeId,
                                CreatedByUserId = (Guid?)null, item.DueAt, item.CreatedAt, item.UpdatedAt, item.CompletedAt,
                                item.employeeName, item.employeeNumber, createdBy = (string?)null
                            }
                            : item).ToList();
                    }
                }
            }

            var memberEmployeeQuery = from membership in db.CompanyMemberships.AsNoTracking()
                                      join employee in db.Employees.AsNoTracking()
                                          on new { membership.CompanyId, Id = membership.EmployeeId }
                                          equals new { employee.CompanyId, Id = (Guid?)employee.Id }
                                      where membership.CompanyId == companyId && membership.UserId == userId && employee.IsActive
                                      select employee;
            var employees = canManage
                ? await employeeScope.Apply(
                    db.Employees.AsNoTracking().Where(employee => employee.CompanyId == companyId && employee.IsActive))
                    .OrderBy(employee => employee.DisplayName).Take(500)
                    .Select(employee => new { employee.Id, employee.DisplayName, employee.EmployeeNumber }).ToListAsync(ct)
                : await employeeScope.Apply(memberEmployeeQuery)
                    .Select(employee => new { employee.Id, employee.DisplayName, employee.EmployeeNumber }).ToListAsync(ct);

            return Results.Ok(new { items, employees, page, pageSize, total, canManage, summary });
        }).RequireAuthorization("company.member");

        api.MapPost("/tasks", async ([FromBody] CompanyTaskWriteRequest request,
            ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryScope(principal, out var companyId, out var userId, out _)) return Results.Forbid();
            var validation = Validate(request);
            if (validation.Count > 0) return Results.ValidationProblem(validation);
            var employeeScope = await GetEmployeeScopeAsync(db, companyId, userId, ct);
            if (employeeScope.IsRestricted && request.AssignedEmployeeId is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assignedEmployeeId"] = ["Erişim kapsamı sınırlı üyeler görevleri yalnızca erişebildikleri etkin bir çalışana atayabilir."] });
            if (request.AssignedEmployeeId is Guid employeeId &&
                !await EmployeeIsInScopeAsync(db, companyId, employeeId, employeeScope, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assignedEmployeeId"] = ["Atanan çalışan etkin ve erişilebilir bir şirket çalışanı olmalıdır."] });

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
            var employeeScope = await GetEmployeeScopeAsync(db, companyId, userId, ct);
            if (employeeScope.IsRestricted && request.AssignedEmployeeId is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assignedEmployeeId"] = ["Erişim kapsamı sınırlı üyeler görevleri yalnızca erişebildikleri etkin bir çalışana atayabilir."] });
            if (request.AssignedEmployeeId is Guid employeeId &&
                !await EmployeeIsInScopeAsync(db, companyId, employeeId, employeeScope, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assignedEmployeeId"] = ["Atanan çalışan etkin ve erişilebilir bir şirket çalışanı olmalıdır."] });

            var taskQuery = db.CompanyTasks.Where(item => item.CompanyId == companyId);
            taskQuery = ApplyEmployeeScope(db, companyId, taskQuery, employeeScope);
            var task = await taskQuery.SingleOrDefaultAsync(item => item.Id == id, ct);
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
            var employeeScope = await GetEmployeeScopeAsync(db, companyId, userId, ct);
            var taskQuery = ApplyEmployeeScope(db, companyId,
                db.CompanyTasks.Where(item => item.CompanyId == companyId), employeeScope);
            var task = await taskQuery.SingleOrDefaultAsync(item => item.Id == id, ct);
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

    private static Task<EmployeeAccessScope> GetEmployeeScopeAsync(AppDbContext db, Guid companyId, Guid userId,
        CancellationToken ct) => CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, userId, ct);

    private static IQueryable<CompanyTask> ApplyEmployeeScope(AppDbContext db, Guid companyId,
        IQueryable<CompanyTask> query, EmployeeAccessScope scope)
    {
        if (!scope.IsRestricted) return query;
        var scopedEmployees = scope.Apply(db.Employees.AsNoTracking()
            .Where(employee => employee.CompanyId == companyId && employee.IsActive));
        return query.Where(task => task.AssignedEmployeeId.HasValue &&
            scopedEmployees.Any(employee => employee.Id == task.AssignedEmployeeId));
    }

    private static Task<bool> EmployeeIsInScopeAsync(AppDbContext db, Guid companyId, Guid employeeId,
        EmployeeAccessScope scope, CancellationToken ct) => scope.Apply(db.Employees.AsNoTracking()
            .Where(employee => employee.CompanyId == companyId && employee.IsActive))
            .AnyAsync(employee => employee.Id == employeeId, ct);

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
