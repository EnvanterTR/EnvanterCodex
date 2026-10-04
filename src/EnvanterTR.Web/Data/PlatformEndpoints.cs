using System.Security.Claims;
using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class PlatformEndpoints
{
    public static IEndpointRouteBuilder MapPlatformEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/plans", async (AppDbContext db, CancellationToken cancellationToken) =>
        {
            var plans = await db.SubscriptionPlans.AsNoTracking()
                .Where(x => x.IsActive && x.IsPublic).OrderBy(x => x.SortOrder)
                .Select(x => new { x.Code, x.Name, x.Description, x.DeploymentKind, x.MaxLoginUsers, x.MaxAssets,
                    x.MonthlyPrice, x.AnnualPrice, x.OneTimePrice, x.Currency, x.SupportsOfflineUse, x.OfflineGraceDays, x.FeatureFlagsJson })
                .ToListAsync(cancellationToken);
            return Results.Ok(plans);
        }).AllowAnonymous();

        endpoints.MapGet("/api/site-content", async (AppDbContext db, CancellationToken cancellationToken) =>
        {
            var blocks = await db.PublicSiteContents.AsNoTracking().Where(x => x.IsPublished)
                .OrderBy(x => x.ContentKey).Select(x => new { x.ContentKey, x.Title, x.Body, x.MediaPath, x.UpdatedAt })
                .ToListAsync(cancellationToken);
            return Results.Ok(blocks);
        }).AllowAnonymous();

        var admin = endpoints.MapGroup("/api/platform").RequireAuthorization("platform.admin");
        admin.AddEndpointFilter(async (invocation, next) =>
        {
            if (HttpMethods.IsGet(invocation.HttpContext.Request.Method) || HttpMethods.IsHead(invocation.HttpContext.Request.Method))
                return await next(invocation);
            var antiforgery = invocation.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            try { await antiforgery.ValidateRequestAsync(invocation.HttpContext); }
            catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
            return await next(invocation);
        });

        admin.MapGet("/overview", async (AppDbContext db, CancellationToken cancellationToken) =>
        {
            var now = DateTimeOffset.UtcNow;
            var next30Days = now.AddDays(30);
            var companies = await db.Companies.AsNoTracking().CountAsync(cancellationToken);
            var activeCompanies = await db.Companies.AsNoTracking().CountAsync(x => x.IsActive, cancellationToken);
            var paidActive = await db.TenantSubscriptions.AsNoTracking()
                .CountAsync(x => !x.IsDemo && x.Status == "Active" && x.EndsAt > now, cancellationToken);
            var demos = await db.TenantSubscriptions.AsNoTracking()
                .CountAsync(x => x.IsDemo && x.Status == "Trial" && x.EndsAt > now, cancellationToken);
            var expired = await db.TenantSubscriptions.AsNoTracking()
                .CountAsync(x => x.EndsAt <= now && x.Status != "Cancelled", cancellationToken);
            var renewalsDue = await db.TenantSubscriptions.AsNoTracking()
                .CountAsync(x => !x.IsDemo && x.Status == "Active" && x.EndsAt > now && x.EndsAt <= next30Days, cancellationToken);
            return Results.Ok(new
            {
                generatedAt = now,
                companies = new { total = companies, active = activeCompanies, suspended = companies - activeCompanies },
                subscriptions = new { paidActive, demos, expired, renewalsDue },
                note = "Gelir metriği ödeme sağlayıcısı bağlanana ve tahsilatlar kaydedilene kadar gösterilmez."
            });
        });

        admin.MapGet("/plans", async (AppDbContext db, CancellationToken cancellationToken) =>
            Results.Ok(await db.SubscriptionPlans.AsNoTracking().OrderBy(x => x.SortOrder)
                .Select(x => new { x.Id, x.Code, x.Name, x.Description, x.DeploymentKind, x.MaxLoginUsers, x.MaxAssets,
                    x.MonthlyPrice, x.AnnualPrice, x.OneTimePrice, x.Currency, x.SupportsOfflineUse, x.OfflineGraceDays,
                    x.FeatureFlagsJson, x.IsPublic, x.IsActive, x.SortOrder }).ToListAsync(cancellationToken)));

        admin.MapPost("/plans", async ([FromBody] SavePlanRequest request, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!ValidatePlan(request, out var error)) return Results.ValidationProblem(error!);
            var code = request.Code.Trim().ToLowerInvariant();
            if (await db.SubscriptionPlans.AnyAsync(x => x.Code == code, cancellationToken))
                return Results.Conflict(new { message = "Paket kodu zaten kullanılıyor." });
            var plan = new SubscriptionPlan
            {
                Code = code,
                Name = request.Name.Trim(),
                DeploymentKind = request.DeploymentKind
            };
            Apply(plan, request, code);
            db.SubscriptionPlans.Add(plan);
            AddAudit(db, user, "PlanCreated", "SubscriptionPlan", plan.Id, null, code);
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Paket kaydı çakıştı." }); }
            return Results.Created($"/api/platform/plans/{plan.Id}", new { plan.Id, plan.Code, plan.Name });
        });

        admin.MapPut("/plans/{id:guid}", async (Guid id, [FromBody] SavePlanRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!ValidatePlan(request, out var error)) return Results.ValidationProblem(error!);
            var plan = await db.SubscriptionPlans.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (plan is null) return Results.NotFound();
            var code = request.Code.Trim().ToLowerInvariant();
            if (await db.SubscriptionPlans.AnyAsync(x => x.Id != id && x.Code == code, cancellationToken))
                return Results.Conflict(new { message = "Paket kodu başka bir pakette kullanılıyor." });
            Apply(plan, request, code);
            AddAudit(db, user, "PlanUpdated", "SubscriptionPlan", plan.Id, null, code);
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Plan güncellemesi başka bir işlemle çakıştı." }); }
            return Results.Ok(new { plan.Id, plan.Code, plan.Name, plan.IsActive });
        });

        admin.MapGet("/companies", async ([FromQuery] string? q, [FromQuery] bool? active,
            [FromQuery] int page, [FromQuery] int pageSize, AppDbContext db, CancellationToken cancellationToken) =>
        {
            page = Math.Clamp(page, 1, 1_000_000);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            var companies = db.Companies.AsNoTracking().AsQueryable();
            if (active is bool isActive) companies = companies.Where(x => x.IsActive == isActive);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama en fazla 120 karakter olabilir."] });
                companies = companies.Where(x => x.Name.Contains(term) || (x.Slug != null && x.Slug.Contains(term)) ||
                    (x.ContactEmail != null && x.ContactEmail.Contains(term)));
            }
            var total = await companies.CountAsync(cancellationToken);
            var rows = await companies.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.Name, x.Slug, x.ContactEmail, x.IsActive, x.CreatedAt,
                    employeeRecords = db.Employees.Count(e => e.CompanyId == x.Id),
                    loginAccounts = db.CompanyMemberships.Count(m => m.CompanyId == x.Id),
                    assetRecords = db.Assets.Count(a => a.CompanyId == x.Id),
                    subscription = db.TenantSubscriptions.Where(s => s.CompanyId == x.Id).OrderByDescending(s => s.StartsAt)
                        .Select(s => new { s.Id, PlanName = db.SubscriptionPlans.Where(p => p.Id == s.PlanId).Select(p => p.Name).FirstOrDefault(), s.Status, s.IsDemo, s.StartsAt, s.EndsAt }).FirstOrDefault()
                }).ToListAsync(cancellationToken);
            return Results.Ok(new { items = rows, page, pageSize, total });
        });

        admin.MapGet("/companies/{companyId:guid}", async (Guid companyId, AppDbContext db, CancellationToken cancellationToken) =>
        {
            var company = await db.Companies.AsNoTracking().Where(x => x.Id == companyId)
                .Select(x => new { x.Id, x.Name, x.Slug, x.ContactEmail, x.IsActive, x.CreatedAt,
                    x.BrandLogoPath, x.BrandPrimaryColor })
                .SingleOrDefaultAsync(cancellationToken);
            if (company is null) return Results.NotFound();
            var memberCount = await db.CompanyMemberships.CountAsync(x => x.CompanyId == companyId, cancellationToken);
            var members = await (from membership in db.CompanyMemberships.AsNoTracking()
                                 join account in db.Users.AsNoTracking() on membership.UserId equals account.Id
                                 where membership.CompanyId == companyId
                                 orderby membership.RoleKey, account.DisplayName
                                 select new { account.Id, account.DisplayName, account.Email, membership.RoleKey, membership.CreatedAt })
                .Take(100).ToListAsync(cancellationToken);
            var subscriptions = await (from subscription in db.TenantSubscriptions.AsNoTracking()
                                       join plan in db.SubscriptionPlans.AsNoTracking() on subscription.PlanId equals plan.Id
                                       where subscription.CompanyId == companyId
                                       orderby subscription.StartsAt descending
                                       select new { subscription.Id, plan.Name, plan.Code, subscription.Status,
                                           subscription.IsDemo, subscription.StartsAt, subscription.EndsAt,
                                           subscription.PaymentReference, subscription.CreatedAt })
                .Take(50).ToListAsync(cancellationToken);
            return Results.Ok(new { company, memberCount, members, membersTruncated = memberCount > members.Count, subscriptions });
        });

        admin.MapGet("/companies/renewals", async ([FromQuery] int days, [FromQuery] int page,
            [FromQuery] int pageSize, AppDbContext db, CancellationToken cancellationToken) =>
        {
            days = Math.Clamp(days == 0 ? 30 : days, 1, 90);
            page = Math.Clamp(page, 1, 1_000_000);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            var now = DateTimeOffset.UtcNow;
            var cutoff = now.AddDays(days);
            var query = from subscription in db.TenantSubscriptions.AsNoTracking()
                        join company in db.Companies.AsNoTracking() on subscription.CompanyId equals company.Id
                        join plan in db.SubscriptionPlans.AsNoTracking() on subscription.PlanId equals plan.Id
                        where company.IsActive && !subscription.IsDemo && subscription.Status == "Active"
                            && subscription.EndsAt > now && subscription.EndsAt <= cutoff
                        orderby subscription.EndsAt
                        select new { company.Id, company.Name, company.Slug, company.ContactEmail,
                            subscriptionId = subscription.Id, subscription.EndsAt, planName = plan.Name,
                            planCode = plan.Code, plan.AnnualPrice, plan.Currency };
            var total = await query.CountAsync(cancellationToken);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return Results.Ok(new { windowDays = days, page, pageSize, total, items });
        });

        admin.MapPost("/companies/{companyId:guid}/demo", async (Guid companyId,
            [FromBody] GrantDemoRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (request is null) return Results.BadRequest(new { message = "Demo bilgileri gereklidir." });
            if (request.Days is < 1 or > 60 || request.Note is { Length: > 1000 } || request.PlanId == Guid.Empty)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["demo"] = ["Demo 1-60 gün arasında olmalı; açıklama en fazla 1000 karakter olabilir."] });
            var plan = await db.SubscriptionPlans.SingleOrDefaultAsync(x => x.Id == request.PlanId && x.IsActive, cancellationToken);
            if (plan is null) return Results.NotFound();
            var now = DateTimeOffset.UtcNow;
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            if (!await db.Companies.AnyAsync(x => x.Id == companyId && x.IsActive, cancellationToken)) return Results.NotFound();
            var pastSubscriptions = await db.TenantSubscriptions.Where(x => x.CompanyId == companyId &&
                (x.Status == "Active" || x.Status == "Trial") && x.EndsAt <= now).ToListAsync(cancellationToken);
            foreach (var past in pastSubscriptions) past.Status = "Expired";
            var active = await db.TenantSubscriptions.AnyAsync(x => x.CompanyId == companyId &&
                (x.Status == "Active" || x.Status == "Trial") && x.EndsAt > now, cancellationToken);
            if (active) return Results.Conflict(new { message = "Şirketin zaten süren bir aboneliği var." });
            var subscription = new TenantSubscription
            {
                CompanyId = companyId, PlanId = plan.Id, Status = "Trial", IsDemo = true,
                StartsAt = now, EndsAt = now.AddDays(request.Days)
            };
            db.TenantSubscriptions.Add(subscription);
            AddAudit(db, user, "DemoGranted", "TenantSubscription", subscription.Id, companyId, request.Note);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "Bu firma için eşzamanlı başka bir aktif abonelik oluşturuldu. Listeyi yenileyip tekrar deneyin." });
            }
            return Results.Created($"/api/platform/companies/{companyId}/subscriptions/{subscription.Id}",
                new { subscription.Id, subscription.Status, subscription.IsDemo, subscription.StartsAt, subscription.EndsAt, plan = plan.Name });
        });

        admin.MapPost("/companies/{companyId:guid}/subscription/extend", async (Guid companyId,
            [FromBody] ExtendSubscriptionRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (request is null) return Results.BadRequest(new { message = "Uzatma bilgileri gereklidir." });
            if (request.Days is < 1 or > 1095 || request.Note is { Length: > 1000 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["extension"] = ["Uzatma 1-1095 gün olmalı; açıklama en fazla 1000 karakter olabilir."] });
            var subscription = await db.TenantSubscriptions.Where(x => x.CompanyId == companyId)
                .OrderByDescending(x => x.StartsAt).FirstOrDefaultAsync(cancellationToken);
            if (subscription is null) return Results.NotFound();
            if (subscription.Status == "Cancelled")
                return Results.Conflict(new { message = "İptal edilmiş abonelik uzatılamaz. Önce yeni bir sipariş/abonelik oluşturulmalıdır." });
            var now = DateTimeOffset.UtcNow;
            subscription.StartsAt = subscription.StartsAt > now ? subscription.StartsAt : now;
            subscription.EndsAt = (subscription.EndsAt > now ? subscription.EndsAt : now).AddDays(request.Days);
            // Extending a demo must not silently turn it into a paid contract.
            subscription.Status = subscription.IsDemo ? "Trial" : "Active";
            AddAudit(db, user, "SubscriptionExtended", "TenantSubscription", subscription.Id, companyId,
                $"{request.Days} gün. {Clean(request.Note)}");
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { subscription.Id, subscription.Status, subscription.EndsAt, subscription.IsDemo });
        });

        admin.MapPatch("/companies/{companyId:guid}/active", async (Guid companyId,
            [FromBody] SetCompanyActiveRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (request is null) return Results.BadRequest(new { message = "Firma erişim durumu gereklidir." });
            if (request.Note is { Length: > 1000 }) return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Açıklama 1000 karakteri aşamaz."] });
            var company = await db.Companies.SingleOrDefaultAsync(x => x.Id == companyId, cancellationToken);
            if (company is null) return Results.NotFound();
            company.IsActive = request.IsActive;
            AddAudit(db, user, request.IsActive ? "CompanyReactivated" : "CompanySuspended", "Company", company.Id, company.Id, request.Note);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { company.Id, company.IsActive });
        });

        admin.MapGet("/site-content", async (AppDbContext db, CancellationToken cancellationToken) =>
            Results.Ok(await db.PublicSiteContents.AsNoTracking().OrderBy(x => x.ContentKey)
                .Select(x => new { x.Id, x.ContentKey, x.Title, x.Body, x.MediaPath, x.IsPublished, x.UpdatedAt })
                .ToListAsync(cancellationToken)));

        admin.MapPut("/site-content/{key}", async (string key, [FromBody] SaveSiteContentRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (request is null) return Results.BadRequest(new { message = "İçerik verisi gereklidir." });
            key = key.Trim().ToLowerInvariant();
            if (key.Length is 0 or > 100 || string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200 ||
                request.Body is { Length: > 12000 } || request.MediaPath is { Length: > 500 } ||
                (request.MediaPath is not null && (!request.MediaPath.StartsWith("/media/", StringComparison.Ordinal) ||
                 request.MediaPath.Contains("..", StringComparison.Ordinal) || request.MediaPath.Contains('\\') ||
                 !Regex.IsMatch(request.MediaPath, "^/media/[A-Za-z0-9._/-]+$"))))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["content"] = ["İçerik alanı sınırı veya medya yolu geçersiz. Medya yolu /media/ ile başlamalıdır."] });
            var block = await db.PublicSiteContents.SingleOrDefaultAsync(x => x.ContentKey == key, cancellationToken);
            if (block is null)
            {
                block = new PublicSiteContent { ContentKey = key, Title = request.Title.Trim() };
                db.PublicSiteContents.Add(block);
            }
            block.Title = request.Title.Trim();
            block.Body = request.Body?.Trim();
            block.MediaPath = request.MediaPath;
            block.IsPublished = request.IsPublished;
            block.UpdatedAt = DateTimeOffset.UtcNow;
            AddAudit(db, user, "SiteContentUpdated", "PublicSiteContent", block.Id, null, key);
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Bu içerik anahtarı başka bir yönetici tarafından eşzamanlı kaydedildi. İçerikleri yenileyin." }); }
            return Results.Ok(new { block.Id, block.ContentKey, block.IsPublished, block.UpdatedAt });
        });

        admin.MapGet("/audit", async ([FromQuery] int page, [FromQuery] int pageSize,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            page = Math.Clamp(page, 1, 1_000_000);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            var query = db.PlatformAuditEntries.AsNoTracking();
            var total = await query.CountAsync(cancellationToken);
            var entries = await query.OrderByDescending(x => x.OccurredAt).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new { x.Id, x.ActorUserId, x.CompanyId, x.Action, x.TargetType, x.TargetId, x.Details, x.OccurredAt })
                .ToListAsync(cancellationToken);
            return Results.Ok(new { entries, total, page, pageSize });
        });

        return endpoints;
    }

    private static bool ValidatePlan(SavePlanRequest request, out Dictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Trim().Length > 40 ||
            !Regex.IsMatch(request.Code.Trim(), "^[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$"))
            errors["code"] = ["Paket kodu harf/rakam ve tek tirelerden oluşmalı, 40 karakteri aşmamalıdır."];
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 120) errors["name"] = ["Paket adı zorunludur (120 karaktere kadar)."];
        if (request.Description is { Length: > 2000 }) errors["description"] = ["Açıklama en fazla 2000 karakter olabilir."];
        if (request.DeploymentKind is not ("cloud" or "local-windows" or "windows-server" or "linux")) errors["deploymentKind"] = ["Dağıtım türü geçersiz."];
        if (request.MaxLoginUsers is <= 0 || request.MaxAssets is <= 0) errors["limits"] = ["Kullanıcı ve varlık sınırı pozitif sayı veya sınırsız için boş olmalıdır."];
        const decimal maximumPrice = 9999999999999999.99m;
        if (request.MonthlyPrice < 0 || request.AnnualPrice < 0 || request.OneTimePrice < 0 ||
            request.MonthlyPrice > maximumPrice || request.AnnualPrice > maximumPrice || request.OneTimePrice > maximumPrice)
            errors["prices"] = ["Fiyatlar negatif olamaz ve veritabanının tutabileceği sınırı aşamaz."];
        if (string.IsNullOrWhiteSpace(request.Currency) || !Regex.IsMatch(request.Currency.Trim(), "^[A-Za-z]{3}$"))
            errors["currency"] = ["Para birimi üç harfli kod olmalıdır (ör. TRY, EUR, USD)."];
        if (request.OfflineGraceDays is < 0 or > 365) errors["offlineGraceDays"] = ["Çevrimdışı tolerans 0-365 gün arasında olmalıdır."];
        if (request.SortOrder is < 0 or > 10000) errors["sortOrder"] = ["Sıralama 0-10000 arasında olmalıdır."];
        if (request.FeatureFlagsJson is { Length: > 8000 }) errors["features"] = ["Özellik ayarları 8000 karakteri aşamaz."];
        else if (!string.IsNullOrWhiteSpace(request.FeatureFlagsJson))
        {
            try
            {
                using var parsed = JsonDocument.Parse(request.FeatureFlagsJson);
                if (parsed.RootElement.ValueKind != JsonValueKind.Object || parsed.RootElement.EnumerateObject()
                    .Any(x => !Regex.IsMatch(x.Name, "^[A-Za-z][A-Za-z0-9_.-]{0,79}$") ||
                        x.Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)))
                    errors["features"] = ["Özellikler JSON nesnesi olmalı; anahtarlar adlandırılmış ve değerler true/false olmalıdır."];
            }
            catch (JsonException) { errors["features"] = ["Özellik ayarları geçerli JSON olmalıdır."]; }
        }
        return errors.Count == 0;
    }

    private static void Apply(SubscriptionPlan plan, SavePlanRequest request, string code)
    {
        plan.Code = code;
        plan.Name = request.Name.Trim();
        plan.Description = Clean(request.Description);
        plan.DeploymentKind = request.DeploymentKind;
        plan.MaxLoginUsers = request.MaxLoginUsers;
        plan.MaxAssets = request.MaxAssets;
        plan.MonthlyPrice = request.MonthlyPrice;
        plan.AnnualPrice = request.AnnualPrice;
        plan.OneTimePrice = request.OneTimePrice;
        plan.Currency = request.Currency.Trim().ToUpperInvariant();
        plan.SupportsOfflineUse = request.SupportsOfflineUse;
        plan.OfflineGraceDays = request.OfflineGraceDays;
        plan.FeatureFlagsJson = Clean(request.FeatureFlagsJson);
        plan.IsPublic = request.IsPublic;
        plan.IsActive = request.IsActive;
        plan.SortOrder = request.SortOrder;
    }

    private static void AddAudit(AppDbContext db, ClaimsPrincipal user, string action,
        string targetType, Guid? targetId, Guid? companyId, string? details)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            throw new InvalidOperationException("Platform audit requires an authenticated user id.");
        db.PlatformAuditEntries.Add(new PlatformAuditEntry
        {
            ActorUserId = actorId, Action = action, TargetType = targetType,
            TargetId = targetId, CompanyId = companyId, Details = Clean(details)
        });
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
