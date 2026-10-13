using System.Security.Claims;
using System.Data;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
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
                .Select(x => new { x.Code, x.Name, x.Description, x.DeploymentKind,
                    MaxLoginUsers = x.DeploymentKind == "cloud" ? (int?)null : x.MaxLoginUsers, x.MaxAssets,
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
            var customerTrials = await db.TenantSubscriptions.AsNoTracking()
                .CountAsync(x => !x.IsDemo && x.Status == "Trial" && x.EndsAt > now, cancellationToken);
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
                subscriptions = new { paidActive, customerTrials, demos, expired, renewalsDue },
                note = "Gelir metriği ödeme sağlayıcısı bağlanana ve tahsilatlar kaydedilene kadar gösterilmez."
            });
        });

        admin.MapGet("/checkout-orders", async (AppDbContext db, CancellationToken cancellationToken,
            [FromQuery] string? q, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        {
            page = Math.Clamp(page, 1, 1_000_000);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            if (q is { Length: > 120 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama en fazla 120 karakter olabilir."] });
            var validStatuses = new[] { "Pending", "PaymentStarted", "PaymentReview", "PaymentFailed", "Expired", "Provisioned", "ProvisioningFailed" };
            if (!string.IsNullOrWhiteSpace(status) && !validStatuses.Contains(status, StringComparer.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Satın alma durumu geçersiz."] });

            var query = from order in db.CompanyCheckoutOrders.AsNoTracking()
                        join plan in db.SubscriptionPlans.AsNoTracking() on order.PlanId equals plan.Id
                        select new { order, plan };
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.order.Status == status);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.order.CompanyName.Contains(term) || x.order.Slug.Contains(term) ||
                    x.order.ContactEmail.Contains(term) || x.order.OwnerName.Contains(term) || x.order.OwnerEmail.Contains(term) ||
                    (x.order.PaymentReference != null && x.order.PaymentReference.Contains(term)));
            }
            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(x => x.order.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.order.Id, x.order.CompanyName, x.order.Slug, x.order.ContactEmail,
                    x.order.OwnerName, x.order.OwnerEmail, x.order.Status, x.order.TermDays,
                    x.order.Amount, x.order.Currency, x.order.PaymentReference, x.order.CompanyId,
                    x.order.CreatedAt, x.order.ExpiresAt, x.order.PaidAt,
                    planName = x.plan.Name, planCode = x.plan.Code
                }).ToListAsync(cancellationToken);
            return Results.Ok(new { items, page, pageSize, total });
        });

        admin.MapGet("/plans", async (AppDbContext db, CancellationToken cancellationToken) =>
            Results.Ok(await db.SubscriptionPlans.AsNoTracking().OrderBy(x => x.SortOrder)
                .Select(x => new { x.Id, x.Code, x.Name, x.Description, x.DeploymentKind,
                    MaxLoginUsers = x.DeploymentKind == "cloud" ? (int?)null : x.MaxLoginUsers, x.MaxAssets,
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

        admin.MapPost("/companies", async ([FromBody] ProvisionCompanyRequest request, ClaimsPrincipal user,
            AppDbContext db, UserManager<EnvanterUser> userManager, IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            var companyName = request.CompanyName?.Trim();
            var slug = request.Slug?.Trim().ToLowerInvariant();
            var contactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim();
            var ownerName = request.OwnerName?.Trim();
            var ownerEmail = request.OwnerEmail?.Trim();
            var paymentReference = request.PaymentReference?.Trim();
            if (string.IsNullOrWhiteSpace(companyName) || companyName.Length > 200 ||
                !TenantHostResolver.IsValidSlug(slug) ||
                (contactEmail is { Length: > 256 } || (contactEmail is not null && !MailAddress.TryCreate(contactEmail, out _))) ||
                string.IsNullOrWhiteSpace(ownerName) || ownerName.Length > 160 ||
                string.IsNullOrWhiteSpace(ownerEmail) || ownerEmail.Length > 256 || !MailAddress.TryCreate(ownerEmail, out _) ||
                request.TermDays is < 1 or > 1095 || string.IsNullOrWhiteSpace(paymentReference) || paymentReference.Length > 180 ||
                !request.PaymentVerified)
                return Results.ValidationProblem(new Dictionary<string, string[]> {
                    ["company"] = ["Firma, geçerli alt alan adı, yetkili kişi ve doğrulanmış ödeme referansı gereklidir; abonelik süresi 1-1095 gün olmalıdır."]
                });

            var plan = await db.SubscriptionPlans.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.PlanId &&
                x.IsActive && x.DeploymentKind == "cloud", cancellationToken);
            if (plan is null) return Results.ValidationProblem(new Dictionary<string, string[]> {
                ["planId"] = ["Etkin bir Cloud planı seçilmelidir."]
            });

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            if (await db.Companies.AnyAsync(x => x.Slug == slug, cancellationToken))
                return Results.Conflict(new { message = "Bu alt alan adı zaten kullanılıyor." });
            var checkoutReservedStatuses = new[] { "Pending", "PaymentStarted", "PaymentReview", "ProvisioningFailed" };
            if (await db.CompanyCheckoutOrders.AnyAsync(x => x.Slug == slug && checkoutReservedStatuses.Contains(x.Status), cancellationToken))
                return Results.Conflict(new { message = "Bu alt alan adı tamamlanmamış bir Cloud satın alma işlemi için ayrılmış." });

            var owner = await userManager.FindByEmailAsync(ownerEmail);
            var wasConfirmed = owner?.EmailConfirmed == true;
            if (owner is null)
            {
                var temporaryPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "aA1!";
                owner = new EnvanterUser { UserName = ownerEmail, Email = ownerEmail, EmailConfirmed = false, DisplayName = ownerName };
                var created = await userManager.CreateAsync(owner, temporaryPassword);
                if (!created.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Results.Conflict(new { message = "Yetkili hesap oluşturulamadı; e-posta adresini ve hesap durumunu kontrol edin." });
                }
            }

            var company = new Company { Name = companyName, Slug = slug, ContactEmail = contactEmail ?? ownerEmail };
            var now = DateTimeOffset.UtcNow;
            var subscription = new TenantSubscription {
                CompanyId = company.Id, PlanId = plan.Id, Status = "Active", IsDemo = false,
                StartsAt = now, EndsAt = now.AddDays(request.TermDays), PaymentReference = paymentReference
            };
            db.Companies.Add(company);
            db.CompanyMemberships.Add(new CompanyMembership { CompanyId = company.Id, UserId = owner.Id, RoleKey = "owner" });
            db.TenantSubscriptions.Add(subscription);
            AddAudit(db, user, "PaidCompanyProvisioned", "Company", company.Id, company.Id,
                $"{plan.Code} · {request.TermDays} gün · ödeme platform yöneticisi tarafından doğrulandı.");
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "Şirket oluşturma çakıştı; şirket ve alt alan adı listesini yenileyin." });
            }

            var invitationSent = false;
            try
            {
                if (wasConfirmed)
                    await AccountEndpoints.SendCompanyAccessAddedEmailAsync(ownerEmail, owner.DisplayName ?? ownerName,
                        companyName, "owner", configuration, cancellationToken);
                else
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(owner);
                    await AccountEndpoints.SendAccountInvitationEmailAsync(ownerEmail, ownerName, token, configuration, cancellationToken);
                }
                invitationSent = true;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Company provisioning is committed; platform administrators can retry the owner notice.
            }

            var rootDomain = configuration["TenantRouting:RootDomain"]?.Trim().Trim('.');
            var tenantUrl = string.IsNullOrWhiteSpace(rootDomain) ? null : $"https://{slug}.{rootDomain}";
            return Results.Created($"/api/platform/companies/{company.Id}", new {
                company.Id, company.Name, company.Slug, company.ContactEmail, ownerId = owner.Id,
                plan = plan.Name, subscription.Status, subscription.StartsAt, subscription.EndsAt,
                tenantUrl, invitationSent,
                message = invitationSent ? "Şirket ve ücretli Cloud aboneliği oluşturuldu; yetkiliye erişim e-postası gönderildi." :
                    "Şirket ve abonelik oluşturuldu; e-posta gönderilemedi. Yetkili davetini firma ayrıntılarından yeniden gönderebilirsiniz."
            });
        }).RequireRateLimiting("login");

        admin.MapPost("/companies/{companyId:guid}/owner-invitation/resend", async (Guid companyId,
            UserManager<EnvanterUser> userManager, AppDbContext db, IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            var owner = await (from membership in db.CompanyMemberships.AsNoTracking()
                join ownerAccount in db.Users.AsNoTracking() on membership.UserId equals ownerAccount.Id
                join company in db.Companies.AsNoTracking() on membership.CompanyId equals company.Id
                where membership.CompanyId == companyId && membership.RoleKey == "owner"
                select new { AccountId = ownerAccount.Id, ownerAccount.Email, ownerAccount.EmailConfirmed, ownerAccount.DisplayName, CompanyName = company.Name })
                .SingleOrDefaultAsync(cancellationToken);
            if (owner?.Email is null) return Results.NotFound();
            var account = await userManager.FindByIdAsync(owner.AccountId.ToString());
            if (account is null) return Results.NotFound();
            try
            {
                if (owner.EmailConfirmed)
                    await AccountEndpoints.SendCompanyAccessAddedEmailAsync(owner.Email, owner.DisplayName ?? owner.Email,
                        owner.CompanyName, "owner", configuration, cancellationToken);
                else
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(account);
                    await AccountEndpoints.SendAccountInvitationEmailAsync(owner.Email, owner.DisplayName ?? owner.Email,
                        token, configuration, cancellationToken);
                }
                return Results.Ok(new { invitationSent = true, message = "Yetkili erişim e-postası yeniden gönderildi." });
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return Results.Problem("E-posta gönderilemedi. E-posta sunucusu ayarlarını kontrol edip yeniden deneyin.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).RequireRateLimiting("login");

        admin.MapGet("/companies", async ([FromQuery] string? q, [FromQuery] bool? active,
            AppDbContext db, CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
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
                                       select new { subscription.Id, subscription.PlanId, plan.Name, plan.Code, subscription.Status,
                                           subscription.IsDemo, subscription.StartsAt, subscription.EndsAt,
                                           subscription.PaymentReference, subscription.CreatedAt })
                .Take(50).ToListAsync(cancellationToken);
            return Results.Ok(new { company, memberCount, members, membersTruncated = memberCount > members.Count, subscriptions });
        });

        admin.MapGet("/companies/renewals", async (AppDbContext db, CancellationToken cancellationToken,
            [FromQuery] int days = 30, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
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

        admin.MapPut("/companies/{companyId:guid}/subscriptions/{subscriptionId:guid}", async (Guid companyId, Guid subscriptionId,
            [FromBody] EditSubscriptionRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (request is null) return Results.BadRequest(new { message = "Abonelik düzenleme bilgileri gereklidir." });
            if (request.PlanId == Guid.Empty || request.StartsAt < new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero) ||
                request.EndsAt <= request.StartsAt || request.EndsAt > DateTimeOffset.UtcNow.AddYears(50) || request.Note is { Length: > 1000 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["subscription"] = ["Etkin plan seçin; başlangıç/bitiş tarihleri geçerli olmalı ve not 1000 karakteri aşmamalıdır."] });

            var subscription = await db.TenantSubscriptions.SingleOrDefaultAsync(x => x.Id == subscriptionId && x.CompanyId == companyId, cancellationToken);
            if (subscription is null) return Results.NotFound();
            var plan = await db.SubscriptionPlans.SingleOrDefaultAsync(x => x.Id == request.PlanId && x.IsActive && x.DeploymentKind == "cloud", cancellationToken);
            if (plan is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["planId"] = ["Etkin bir Cloud paketi seçilmelidir."] });

            var previous = new { subscription.PlanId, subscription.StartsAt, subscription.EndsAt, subscription.Status, subscription.IsDemo };
            subscription.PlanId = plan.Id;
            subscription.StartsAt = request.StartsAt.ToUniversalTime();
            subscription.EndsAt = request.EndsAt.ToUniversalTime();
            var current = new { subscription.PlanId, subscription.StartsAt, subscription.EndsAt, subscription.Status, subscription.IsDemo };
            var audit = JsonSerializer.Serialize(new { before = previous, after = current, note = Clean(request.Note) });
            AddAudit(db, user, "SubscriptionManuallyAdjusted", "TenantSubscription", subscription.Id, companyId, audit);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { subscription.Id, subscription.PlanId, subscription.StartsAt, subscription.EndsAt, subscription.Status, subscription.IsDemo });
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

        admin.MapGet("/audit", async (AppDbContext db, CancellationToken cancellationToken,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
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
        if (request.DeploymentKind == "cloud" && request.MaxLoginUsers is not null)
            errors["maxLoginUsers"] = ["Bulut planlarında giriş kullanıcısı sınırı yoktur; tüm şirket kullanıcıları dahildir."];
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
        plan.MaxLoginUsers = request.DeploymentKind == "cloud" ? null : request.MaxLoginUsers;
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
