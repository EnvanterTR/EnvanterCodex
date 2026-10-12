using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class SupportEndpoints
{
    private static readonly string[] Categories = ["Access", "Inventory", "Data", "Other"];
    private static readonly string[] Statuses = ["Open", "InProgress", "AwaitingCustomer", "Resolved", "Closed"];
    private static readonly string[] StaffStatuses = ["Open", "InProgress", "AwaitingCustomer", "Resolved", "Closed"];

    public static IEndpointRouteBuilder MapSupportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var customer = endpoints.MapGroup("/api/support").RequireAuthorization("company.member");
        customer.AddEndpointFilter(async (invocation, next) =>
        {
            if (!TryCompanyScope(invocation.HttpContext.User, out var companyId, out var userId, out var role))
                return Results.Forbid();
            var db = invocation.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var active = await db.Companies.AsNoTracking().AnyAsync(company => company.Id == companyId && company.IsActive &&
                db.CompanyMemberships.Any(membership => membership.CompanyId == companyId && membership.UserId == userId && membership.RoleKey == role),
                invocation.HttpContext.RequestAborted);
            if (!active) return Results.Forbid();
            if (HttpMethods.IsGet(invocation.HttpContext.Request.Method) || HttpMethods.IsHead(invocation.HttpContext.Request.Method))
                return await next(invocation);
            return await HasCsrfAsync(invocation.HttpContext)
                ? await next(invocation)
                : Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
        });

        customer.MapGet("/tickets", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct,
            [FromQuery] string? status = null, [FromQuery] string? q = null,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        {
            if (!TryCompanyScope(user, out var companyId, out var userId, out var role)) return Results.Forbid();
            if (status is not null && !Statuses.Contains(status, StringComparer.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Destek talebi durumu geçerli değil."] });
            var term = q?.Trim();
            if (term?.Length > 120)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 100);
            var canViewAll = CanManageCompanySupport(role);
            var query = db.SupportTickets.AsNoTracking().Where(ticket => ticket.CompanyId == companyId);
            if (!canViewAll) query = query.Where(ticket => ticket.CreatedByUserId == userId);
            if (status is not null) query = query.Where(ticket => ticket.Status == status);
            if (!string.IsNullOrWhiteSpace(term)) query = query.Where(ticket => EF.Functions.ILike(ticket.Subject, $"%{term}%"));
            var total = await query.CountAsync(ct);
            var items = await query.OrderByDescending(ticket => ticket.UpdatedAt).ThenBy(ticket => ticket.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(ticket => new
                {
                    ticket.Id, ticket.Subject, ticket.Category, ticket.Status, ticket.CreatedAt, ticket.UpdatedAt,
                    ticket.CreatedByUserId,
                    createdBy = db.Users.Where(account => account.Id == ticket.CreatedByUserId).Select(account => account.DisplayName).FirstOrDefault(),
                    messageCount = db.SupportTicketMessages.Count(message => message.CompanyId == companyId && message.TicketId == ticket.Id)
                }).ToListAsync(ct);
            return Results.Ok(new { items, total, page, pageSize, canViewAll });
        });

        customer.MapPost("/tickets", async ([FromBody] CreateSupportTicketRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyScope(user, out var companyId, out var userId, out _)) return Results.Forbid();
            var errors = ValidateCreate(request);
            if (errors.Count != 0) return Results.ValidationProblem(errors);
            var now = DateTimeOffset.UtcNow;
            var ticket = new SupportTicket
            {
                CompanyId = companyId,
                CreatedByUserId = userId,
                Subject = request.Subject.Trim(),
                Category = request.Category,
                Status = "Open",
                CreatedAt = now,
                UpdatedAt = now
            };
            var message = new SupportTicketMessage
            {
                CompanyId = companyId,
                TicketId = ticket.Id,
                AuthorUserId = userId,
                Body = request.Message.Trim(),
                IsSupportReply = false,
                CreatedAt = now
            };
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            db.SupportTickets.Add(ticket);
            db.SupportTicketMessages.Add(message);
            AddCompanyAudit(db, companyId, userId, ticket.Id, "SupportTicketCreated", "Yeni destek talebi oluşturuldu.");
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Created($"/api/support/tickets/{ticket.Id}", new { ticket.Id, ticket.Subject, ticket.Category, ticket.Status, ticket.CreatedAt, ticket.UpdatedAt });
        });

        customer.MapGet("/tickets/{ticketId:guid}", async (Guid ticketId, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyScope(user, out var companyId, out var userId, out var role)) return Results.Forbid();
            var canViewAll = CanManageCompanySupport(role);
            var ticket = await db.SupportTickets.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == ticketId &&
                    (canViewAll || x.CreatedByUserId == userId))
                .Select(x => new { x.Id, x.Subject, x.Category, x.Status, x.CreatedByUserId, x.CreatedAt, x.UpdatedAt })
                .SingleOrDefaultAsync(ct);
            if (ticket is null) return Results.NotFound();
            var messages = await db.SupportTicketMessages.AsNoTracking().Where(x => x.CompanyId == companyId && x.TicketId == ticketId)
                .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.Id, x.Body, x.IsSupportReply, x.CreatedAt,
                    author = x.AuthorUserId == null ? null : db.Users.Where(account => account.Id == x.AuthorUserId).Select(account => account.DisplayName).FirstOrDefault()
                }).ToListAsync(ct);
            return Results.Ok(new { ticket, messages, canViewAll, canReply = ticket.Status != "Closed" });
        });

        customer.MapPost("/tickets/{ticketId:guid}/messages", async (Guid ticketId, [FromBody] SupportTicketMessageRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyScope(user, out var companyId, out var userId, out var role)) return Results.Forbid();
            var body = request.Body?.Trim();
            if (string.IsNullOrWhiteSpace(body) || body.Length > 8000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["body"] = ["Yanıt 1–8.000 karakter olmalıdır."] });
            var canViewAll = CanManageCompanySupport(role);
            var ticket = await db.SupportTickets.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == ticketId &&
                (canViewAll || x.CreatedByUserId == userId), ct);
            if (ticket is null) return Results.NotFound();
            if (ticket.Status == "Closed") return Results.Conflict(new { message = "Kapatılan talebe yanıt eklenemez; yeni bir talep oluşturun." });
            var now = DateTimeOffset.UtcNow;
            var message = new SupportTicketMessage { CompanyId = companyId, TicketId = ticketId, AuthorUserId = userId, Body = body,
                IsSupportReply = false, CreatedAt = now };
            ticket.Status = "Open";
            ticket.UpdatedAt = now;
            db.SupportTicketMessages.Add(message);
            AddCompanyAudit(db, companyId, userId, ticketId, "SupportTicketReplied", "Müşteri destek talebine yanıt verdi.");
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/support/tickets/{ticketId}", new { message.Id, message.Body, message.IsSupportReply, message.CreatedAt });
        });

        customer.MapPost("/tickets/{ticketId:guid}/close", async (Guid ticketId, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyScope(user, out var companyId, out var userId, out var role)) return Results.Forbid();
            var canViewAll = CanManageCompanySupport(role);
            var ticket = await db.SupportTickets.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == ticketId &&
                (canViewAll || x.CreatedByUserId == userId), ct);
            if (ticket is null) return Results.NotFound();
            if (ticket.Status == "Closed") return Results.Ok(new { ticket.Id, ticket.Status, ticket.UpdatedAt });
            ticket.Status = "Closed";
            ticket.UpdatedAt = DateTimeOffset.UtcNow;
            AddCompanyAudit(db, companyId, userId, ticketId, "SupportTicketClosed", "Şirket kullanıcısı destek talebini kapattı.");
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { ticket.Id, ticket.Status, ticket.UpdatedAt });
        });

        var staff = endpoints.MapGroup("/api/platform/support/tickets").RequireAuthorization("platform.admin");
        staff.AddEndpointFilter(async (invocation, next) => await HasCsrfAsync(invocation.HttpContext)
            ? await next(invocation)
            : HttpMethods.IsGet(invocation.HttpContext.Request.Method) || HttpMethods.IsHead(invocation.HttpContext.Request.Method)
                ? await next(invocation)
                : Results.BadRequest(new { message = "İstek doğrulaması başarısız." }));
        staff.MapGet("", async (AppDbContext db, CancellationToken ct, [FromQuery] string? status = null,
            [FromQuery] string? q = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        {
            if (status is not null && !StaffStatuses.Contains(status, StringComparer.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Destek talebi durumu geçerli değil."] });
            var term = q?.Trim();
            if (term?.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 100);
            var query = db.SupportTickets.AsNoTracking();
            if (status is not null) query = query.Where(ticket => ticket.Status == status);
            if (!string.IsNullOrWhiteSpace(term)) query = query.Where(ticket => EF.Functions.ILike(ticket.Subject, $"%{term}%") ||
                db.Companies.Any(company => company.Id == ticket.CompanyId && EF.Functions.ILike(company.Name, $"%{term}%")));
            var total = await query.CountAsync(ct);
            var items = await query.OrderByDescending(ticket => ticket.UpdatedAt).ThenBy(ticket => ticket.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(ticket => new
                {
                    ticket.Id, ticket.CompanyId, companyName = db.Companies.Where(company => company.Id == ticket.CompanyId).Select(company => company.Name).FirstOrDefault(),
                    ticket.Subject, ticket.Category, ticket.Status, ticket.CreatedAt, ticket.UpdatedAt,
                    requester = db.Users.Where(account => account.Id == ticket.CreatedByUserId).Select(account => account.DisplayName).FirstOrDefault(),
                    messageCount = db.SupportTicketMessages.Count(message => message.CompanyId == ticket.CompanyId && message.TicketId == ticket.Id)
                }).ToListAsync(ct);
            return Results.Ok(new { items, total, page, pageSize });
        });

        staff.MapGet("/{ticketId:guid}", async (Guid ticketId, AppDbContext db, CancellationToken ct) =>
        {
            var ticket = await db.SupportTickets.AsNoTracking().Where(x => x.Id == ticketId)
                .Select(x => new
                {
                    x.Id, x.CompanyId, companyName = db.Companies.Where(company => company.Id == x.CompanyId).Select(company => company.Name).FirstOrDefault(),
                    x.Subject, x.Category, x.Status, x.CreatedByUserId, x.CreatedAt, x.UpdatedAt,
                    requester = db.Users.Where(account => account.Id == x.CreatedByUserId).Select(account => account.DisplayName).FirstOrDefault()
                }).SingleOrDefaultAsync(ct);
            if (ticket is null) return Results.NotFound();
            var messages = await db.SupportTicketMessages.AsNoTracking().Where(x => x.CompanyId == ticket.CompanyId && x.TicketId == ticketId)
                .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
                .Select(x => new { x.Id, x.Body, x.IsSupportReply, x.CreatedAt,
                    author = x.AuthorUserId == null ? null : db.Users.Where(account => account.Id == x.AuthorUserId).Select(account => account.DisplayName).FirstOrDefault() })
                .ToListAsync(ct);
            return Results.Ok(new { ticket, messages });
        });

        staff.MapPost("/{ticketId:guid}/messages", async (Guid ticketId, [FromBody] SupportTicketMessageRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
            var body = request.Body?.Trim();
            if (string.IsNullOrWhiteSpace(body) || body.Length > 8000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["body"] = ["Yanıt 1–8.000 karakter olmalıdır."] });
            var ticket = await db.SupportTickets.SingleOrDefaultAsync(x => x.Id == ticketId, ct);
            if (ticket is null) return Results.NotFound();
            if (ticket.Status == "Closed") return Results.Conflict(new { message = "Kapatılan destek talebine yanıt eklenemez." });
            var now = DateTimeOffset.UtcNow;
            var message = new SupportTicketMessage { CompanyId = ticket.CompanyId, TicketId = ticket.Id, AuthorUserId = userId,
                IsSupportReply = true, Body = body, CreatedAt = now };
            ticket.Status = "AwaitingCustomer";
            ticket.UpdatedAt = now;
            db.SupportTicketMessages.Add(message);
            db.PlatformAuditEntries.Add(new PlatformAuditEntry { ActorUserId = userId, CompanyId = ticket.CompanyId,
                TargetType = "SupportTicket", TargetId = ticket.Id, Action = "SupportTicketReplied",
                Details = "Platform destek görevlisi talebe yanıt verdi." });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/platform/support/tickets/{ticketId}", new { message.Id, message.IsSupportReply, message.CreatedAt });
        });

        staff.MapPatch("/{ticketId:guid}/status", async (Guid ticketId, [FromBody] SupportTicketStatusRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
            if (!Statuses.Contains(request.Status, StringComparer.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Destek talebi durumu geçerli değil."] });
            var ticket = await db.SupportTickets.SingleOrDefaultAsync(x => x.Id == ticketId, ct);
            if (ticket is null) return Results.NotFound();
            var previousStatus = ticket.Status;
            ticket.Status = request.Status;
            ticket.UpdatedAt = DateTimeOffset.UtcNow;
            db.PlatformAuditEntries.Add(new PlatformAuditEntry { ActorUserId = userId, CompanyId = ticket.CompanyId,
                TargetType = "SupportTicket", TargetId = ticket.Id, Action = "SupportTicketStatusChanged",
                Details = $"Destek durumu {previousStatus} → {ticket.Status} olarak değiştirildi." });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { ticket.Id, ticket.Status, ticket.UpdatedAt });
        });
        return endpoints;
    }

    private static Dictionary<string, string[]> ValidateCreate(CreateSupportTicketRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var subject = request.Subject?.Trim();
        var body = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 160) errors["subject"] = ["Konu 1–160 karakter olmalıdır."];
        if (string.IsNullOrWhiteSpace(body) || body.Length > 8000) errors["message"] = ["Açıklama 1–8.000 karakter olmalıdır."];
        if (!Categories.Contains(request.Category, StringComparer.Ordinal)) errors["category"] = ["Destek kategorisi geçerli değil."];
        return errors;
    }

    private static bool TryCompanyScope(ClaimsPrincipal principal, out Guid companyId, out Guid userId, out string role)
    {
        var hasCompany = Guid.TryParse(principal.FindFirstValue("company_id"), out companyId);
        var hasUser = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        role = principal.FindFirstValue("company_role") ?? string.Empty;
        return hasCompany && hasUser && role.Length != 0;
    }

    private static bool CanManageCompanySupport(string role) => role is "owner" or "admin";

    private static async Task<bool> HasCsrfAsync(HttpContext context)
    {
        try
        {
            await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException) { return false; }
    }

    private static void AddCompanyAudit(AppDbContext db, Guid companyId, Guid userId, Guid ticketId, string action, string details) =>
        db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId, TargetType = "SupportTicket",
            TargetId = ticketId, Action = action, Details = details });
}

public sealed record CreateSupportTicketRequest(string Subject, string Category, string Message);
public sealed record SupportTicketMessageRequest(string Body);
public sealed record SupportTicketStatusRequest(string Status);
