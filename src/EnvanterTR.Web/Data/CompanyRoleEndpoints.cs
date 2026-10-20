using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed record SaveCompanyRoleRequest(string? Name, string[]? PermissionKeys);
public sealed record SetCompanyRoleActiveRequest(bool IsActive);

public static class CompanyRoleEndpoints
{
    public static IEndpointRouteBuilder MapCompanyRoleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/company-roles").RequireAuthorization("company.member");
        api.AddEndpointFilter(async (invocation, next) =>
        {
            var context = invocation.HttpContext;
            var role = context.User.FindFirstValue("company_role");
            if (!Guid.TryParse(context.User.FindFirstValue("company_id"), out var companyId) || string.IsNullOrWhiteSpace(role))
                return Results.Forbid();
            var db = context.RequestServices.GetRequiredService<AppDbContext>();
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var actorId) ||
                !await db.Companies.AsNoTracking().AnyAsync(company => company.Id == companyId && company.IsActive &&
                    db.CompanyMemberships.Any(member => member.CompanyId == companyId && member.UserId == actorId && member.RoleKey == role),
                    context.RequestAborted)) return Results.Forbid();
            if (await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, context.RequestAborted) is not null)
                return Results.Forbid();
            if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)) return await next(invocation);
            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
            try { await antiforgery.ValidateRequestAsync(context); }
            catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
            return await next(invocation);
        });

        api.MapGet("", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            var customRoles = await db.CompanyCustomRoles.AsNoTracking()
                .Where(role => role.CompanyId == companyId)
                .OrderBy(role => role.Name).ToListAsync(cancellationToken);
            return Results.Ok(new
            {
                permissions = CompanyRoleAuthorization.PermissionNames,
                roles = CompanyRoleAuthorization.BuiltIns.Concat(customRoles.Select(role =>
                    new CompanyRoleDefinitionDto(role.RoleKey, role.Name,
                        CompanyRoleAuthorization.ParsePermissionKeys(role.PermissionsJson), BuiltIn: false, role.IsActive)))
                    .ToArray()
            });
        });

        api.MapPost("", async ([FromBody] SaveCompanyRoleRequest request, ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length is < 2 or > 80)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Rol adı 2–80 karakter olmalıdır."] });
            if (!CompanyRoleAuthorization.ArePermissionKeysValid(request.PermissionKeys))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["permissionKeys"] = ["Rolde yinelenen veya desteklenmeyen izin var."] });
            if (request.PermissionKeys!.Contains(CompanyRoleAuthorization.CompanySettings, StringComparer.Ordinal) &&
                user.FindFirstValue("company_role") != "owner") return Results.Forbid();

            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            var lockedCompany = await db.Companies.FromSqlInterpolated($"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (lockedCompany is null || !lockedCompany.IsActive) return Results.Forbid();
            var count = await db.CompanyCustomRoles.CountAsync(role => role.CompanyId == companyId, cancellationToken);
            if (count >= 30) return Results.Conflict(new { message = "Bir şirkette en fazla 30 özel rol tanımlanabilir." });
            if (await db.CompanyCustomRoles.AnyAsync(role => role.CompanyId == companyId && role.NormalizedName == name.ToUpperInvariant(), cancellationToken))
                return Results.Conflict(new { message = "Bu adla bir şirket rolü zaten var." });

            var roleKey = $"custom_{Guid.NewGuid():N}";
            var now = DateTimeOffset.UtcNow;
            var role = new CompanyCustomRole
            {
                CompanyId = companyId, RoleKey = roleKey, Name = name, NormalizedName = name.ToUpperInvariant(),
                PermissionsJson = CompanyRoleAuthorization.SerializePermissionKeys(request.PermissionKeys!),
                CreatedByUserId = actorId, CreatedAt = now, UpdatedAt = now
            };
            db.CompanyCustomRoles.Add(role);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "CompanyCustomRole", TargetId = role.Id, Action = "company-role.created",
                Details = $"{name}; izinler: {role.PermissionsJson}" });
            try { await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Rol kaydı çakıştı; listeyi yenileyip tekrar deneyin." }); }
            return Results.Created($"/api/company-roles/{roleKey}", new CompanyRoleDefinitionDto(role.RoleKey, role.Name,
                CompanyRoleAuthorization.ParsePermissionKeys(role.PermissionsJson), BuiltIn: false, role.IsActive));
        }).RequireAuthorization("settings.manage");

        api.MapPut("/{roleKey}", async (string roleKey, [FromBody] SaveCompanyRoleRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (CompanyRoleAuthorization.IsBuiltIn(roleKey)) return Results.Conflict(new { message = "Yerleşik roller değiştirilemez." });
            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length is < 2 or > 80)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Rol adı 2–80 karakter olmalıdır."] });
            if (!CompanyRoleAuthorization.ArePermissionKeysValid(request.PermissionKeys))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["permissionKeys"] = ["Rolde yinelenen veya desteklenmeyen izin var."] });
            if (request.PermissionKeys!.Contains(CompanyRoleAuthorization.CompanySettings, StringComparer.Ordinal) &&
                user.FindFirstValue("company_role") != "owner") return Results.Forbid();
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            var lockedCompany = await db.Companies.FromSqlInterpolated($"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (lockedCompany is null || !lockedCompany.IsActive) return Results.Forbid();
            var role = await db.CompanyCustomRoles.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.RoleKey == roleKey, cancellationToken);
            if (role is null) return Results.NotFound();
            if (await db.CompanyCustomRoles.AnyAsync(item => item.CompanyId == companyId && item.Id != role.Id &&
                    item.NormalizedName == name.ToUpperInvariant(), cancellationToken))
                return Results.Conflict(new { message = "Bu adla bir şirket rolü zaten var." });
            var previousName = role.Name;
            var previousPermissions = role.PermissionsJson;
            role.Name = name;
            role.NormalizedName = name.ToUpperInvariant();
            role.PermissionsJson = CompanyRoleAuthorization.SerializePermissionKeys(request.PermissionKeys!);
            role.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "CompanyCustomRole", TargetId = role.Id, Action = "company-role.updated",
                Details = $"{previousName} → {role.Name}; izinler {previousPermissions} → {role.PermissionsJson}" });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new CompanyRoleDefinitionDto(role.RoleKey, role.Name,
                CompanyRoleAuthorization.ParsePermissionKeys(role.PermissionsJson), BuiltIn: false, role.IsActive));
        }).RequireAuthorization("settings.manage");

        api.MapPatch("/{roleKey}/active", async (string roleKey, [FromBody] SetCompanyRoleActiveRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (CompanyRoleAuthorization.IsBuiltIn(roleKey)) return Results.Conflict(new { message = "Yerleşik roller devre dışı bırakılamaz." });
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            var lockedCompany = await db.Companies.FromSqlInterpolated($"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (lockedCompany is null || !lockedCompany.IsActive) return Results.Forbid();
            var role = await db.CompanyCustomRoles.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.RoleKey == roleKey, cancellationToken);
            if (role is null) return Results.NotFound();
            if (!request.IsActive && await db.CompanyMemberships.AnyAsync(member => member.CompanyId == companyId && member.RoleKey == roleKey, cancellationToken))
                return Results.Conflict(new { message = "Bu rol üyelere atanmış. Devre dışı bırakmadan önce üyeleri başka role alın." });
            if (role.IsActive == request.IsActive)
            {
                await transaction.CommitAsync(cancellationToken);
                return Results.Ok(new { role.RoleKey, role.IsActive });
            }
            role.IsActive = request.IsActive;
            role.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "CompanyCustomRole", TargetId = role.Id, Action = request.IsActive ? "company-role.activated" : "company-role.deactivated",
                Details = role.Name });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { role.RoleKey, role.IsActive });
        }).RequireAuthorization("settings.manage");

        return endpoints;
    }

    private static bool TryScope(ClaimsPrincipal user, out Guid companyId, out Guid actorId)
    {
        companyId = Guid.Empty;
        actorId = Guid.Empty;
        return Guid.TryParse(user.FindFirstValue("company_id"), out companyId) &&
            Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out actorId);
    }
}
