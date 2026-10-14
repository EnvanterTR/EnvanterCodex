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
                .Select(x => new { x.Id, x.Code, x.Name, x.Description, x.DeploymentKind,
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

        endpoints.MapPost("/api/demo-requests", async ([FromBody] PublicDemoRequest request, AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!string.IsNullOrWhiteSpace(request.Website))
                return Results.Accepted(value: new { message = "Talebiniz alındı. Ekibimiz sizinle iletişime geçecek." });
            var companyName = request.CompanyName?.Trim();
            var contactName = request.ContactName?.Trim();
            var emailValue = request.Email?.Trim();
            if (companyName is null || companyName.Length is < 2 or > 200 || contactName is null ||
                contactName.Length is < 2 or > 160 || emailValue is null || emailValue.Length > 256 ||
                !MailAddress.TryCreate(emailValue, out var parsedEmail) ||
                !string.Equals(parsedEmail.Address, emailValue, StringComparison.OrdinalIgnoreCase) || !request.Consent ||
                request.Phone?.Trim().Length > 40 || request.JobTitle?.Trim().Length > 120 ||
                request.TeamSize?.Trim().Length > 40 || request.AssetCount?.Trim().Length > 40 ||
                request.Message?.Trim().Length > 2000)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = ["Şirket, yetkili kişi, geçerli e-posta ve aydınlatma/onay seçimi gereklidir; alan sınırlarını kontrol edin."]
                });

            var now = DateTimeOffset.UtcNow;
            var normalizedEmail = parsedEmail.Address.ToLowerInvariant();
            var duplicate = await db.PlatformDemoRequests.AsNoTracking().AnyAsync(x =>
                x.Email == normalizedEmail && x.CompanyName == companyName && x.CreatedAt >= now.AddDays(-1) && x.Status != "Closed",
                cancellationToken);
            if (!duplicate)
            {
                db.PlatformDemoRequests.Add(new PlatformDemoRequest
                {
                    CompanyName = companyName,
                    ContactName = contactName,
                    Email = normalizedEmail,
                    Phone = Clean(request.Phone),
                    JobTitle = Clean(request.JobTitle),
                    TeamSize = Clean(request.TeamSize),
                    AssetCount = Clean(request.AssetCount),
                    Message = Clean(request.Message),
                    Source = "PublicWebsite",
                    Status = "New",
                    ConsentAt = now,
                    ConsentNoticeVersion = "demo-request-v1",
                    CreatedAt = now,
                    UpdatedAt = now
                });
                await db.SaveChangesAsync(cancellationToken);
            }
            return Results.Accepted(value: new { message = "Talebiniz alındı. Ekibimiz sizinle iletişime geçecek." });
        }).AllowAnonymous().RequireRateLimiting("login");

        var admin = endpoints.MapGroup("/api/platform").RequireAuthorization("platform.admin");
        admin.MapGet("/admins", async (AppDbContext db, CancellationToken cancellationToken) =>
            Results.Ok(await (from platformAdmin in db.PlatformAdministrators.AsNoTracking()
                              join account in db.Users.AsNoTracking() on platformAdmin.UserId equals account.Id
                              orderby account.DisplayName
                              select new { id = account.Id, name = account.DisplayName, email = account.Email })
                .ToListAsync(cancellationToken)));
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

        admin.MapGet("/members", async (AppDbContext db, CancellationToken cancellationToken,
            [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        {
            page = Math.Clamp(page, 1, 1_000_000);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            if (q is { Length: > 120 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama en fazla 120 karakter olabilir."] });
            var users = db.Users.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                users = users.Where(account => account.DisplayName.Contains(term) ||
                    (account.Email != null && account.Email.Contains(term)) ||
                    (account.PhoneNumber != null && account.PhoneNumber.Contains(term)) ||
                    db.CompanyMemberships.Any(membership => membership.UserId == account.Id &&
                        db.Companies.Any(company => company.Id == membership.CompanyId &&
                            (company.Name.Contains(term) || (company.Slug != null && company.Slug.Contains(term))))));
            }
            var total = await users.CountAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var accounts = await users.OrderBy(account => account.DisplayName).ThenBy(account => account.Email)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(account => new
                {
                    account.Id, account.DisplayName, account.Email, account.EmailConfirmed, account.AccountCreatedAt,
                    account.PhoneNumber, account.TwoFactorEnabled, account.LockoutEnd,
                    account.AccessFailedCount,
                    firstMembershipAt = db.CompanyMemberships.Where(membership => membership.UserId == account.Id)
                        .OrderBy(membership => membership.CreatedAt).Select(membership => (DateTimeOffset?)membership.CreatedAt).FirstOrDefault(),
                    lastSeenAt = db.UserSessions.Where(session => session.UserId == account.Id)
                        .OrderByDescending(session => session.LastSeenAt).Select(session => (DateTimeOffset?)session.LastSeenAt).FirstOrDefault(),
                    membershipCount = db.CompanyMemberships.Count(membership => membership.UserId == account.Id)
                }).ToListAsync(cancellationToken);
            var userIds = accounts.Select(account => account.Id).ToArray();
            var memberships = await (from membership in db.CompanyMemberships.AsNoTracking()
                                     join company in db.Companies.AsNoTracking() on membership.CompanyId equals company.Id
                                     where userIds.Contains(membership.UserId)
                                     orderby company.Name, membership.RoleKey
                                     select new { membership.UserId, company.Name, company.Slug, membership.RoleKey, membership.CreatedAt, company.IsActive })
                .ToListAsync(cancellationToken);
            var membershipsByUser = memberships.GroupBy(membership => membership.UserId)
                .ToDictionary(group => group.Key, group => group.Select(membership => new
                {
                    membership.Name, membership.Slug, membership.RoleKey, membership.CreatedAt, membership.IsActive
                }).ToArray());
            var items = accounts.Select(account => new
            {
                account.Id, account.DisplayName, account.Email, account.EmailConfirmed, account.PhoneNumber,
                account.AccountCreatedAt, account.TwoFactorEnabled, isLockedOut = account.LockoutEnd is DateTimeOffset lockoutEnd && lockoutEnd > now,
                account.AccessFailedCount, account.firstMembershipAt, account.lastSeenAt, account.membershipCount,
                memberships = membershipsByUser.GetValueOrDefault(account.Id) ?? []
            });
            return Results.Ok(new { items, page, pageSize, total });
        });

        admin.MapGet("/members/{userId:guid}", async (Guid userId, AppDbContext db, CancellationToken cancellationToken) =>
        {
            var now = DateTimeOffset.UtcNow;
            var account = await db.Users.AsNoTracking().Where(user => user.Id == userId)
                .Select(user => new
                {
                    user.Id, user.DisplayName, user.Email, user.EmailConfirmed, user.PhoneNumber,
                    user.PhoneNumberConfirmed, user.TwoFactorEnabled, user.LockoutEnd, user.AccessFailedCount,
                    user.LockoutEnabled, user.AccountCreatedAt,
                    lastSeenAt = db.UserSessions.Where(session => session.UserId == user.Id)
                        .OrderByDescending(session => session.LastSeenAt).Select(session => (DateTimeOffset?)session.LastSeenAt).FirstOrDefault()
                }).SingleOrDefaultAsync(cancellationToken);
            if (account is null) return Results.NotFound();
            var memberships = await (from membership in db.CompanyMemberships.AsNoTracking()
                                     join company in db.Companies.AsNoTracking() on membership.CompanyId equals company.Id
                                     where membership.UserId == userId
                                     orderby membership.CreatedAt descending
                                     select new
                                     {
                                         company.Id, company.Name, company.Slug, company.IsActive,
                                         membership.RoleKey, membership.CreatedAt,
                                         subscription = (from subscription in db.TenantSubscriptions.AsNoTracking()
                                                         join plan in db.SubscriptionPlans.AsNoTracking() on subscription.PlanId equals plan.Id
                                                         where subscription.CompanyId == company.Id
                                                         orderby subscription.StartsAt descending
                                                         select new { plan.Name, plan.Code, subscription.Status, subscription.IsDemo,
                                                             subscription.StartsAt, subscription.EndsAt, subscription.PaymentReference })
                                             .FirstOrDefault()
                                     }).Take(200).ToListAsync(cancellationToken);
            var membershipCount = await db.CompanyMemberships.CountAsync(membership => membership.UserId == userId, cancellationToken);
            return Results.Ok(new
            {
                account = new
                {
                    account.Id, account.DisplayName, account.Email, account.EmailConfirmed, account.PhoneNumber,
                    account.PhoneNumberConfirmed, account.TwoFactorEnabled, account.LockoutEnabled, account.AccountCreatedAt,
                    isLockedOut = account.LockoutEnd is DateTimeOffset lockoutEnd && lockoutEnd > now,
                    account.AccessFailedCount, account.lastSeenAt,
                    firstMembershipAt = await db.CompanyMemberships.Where(membership => membership.UserId == userId)
                        .OrderBy(membership => membership.CreatedAt).Select(membership => (DateTimeOffset?)membership.CreatedAt).FirstOrDefaultAsync(cancellationToken)
                },
                memberships, membershipCount, membershipsTruncated = membershipCount > memberships.Count
            });
        });

        admin.MapGet("/members/{userId:guid}/timeline", async (Guid userId, AppDbContext db,
            CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        {
            page = Math.Clamp(page, 1, 1_000);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 100);
            var account = await db.Users.AsNoTracking().Where(x => x.Id == userId)
                .Select(x => new { x.Id, x.Email }).SingleOrDefaultAsync(cancellationToken);
            if (account is null) return Results.NotFound();

            const int perSourceLimit = 500;
            var events = new List<PlatformMemberTimelineEvent>();
            var truncated = false;
            var createdAt = await db.Users.AsNoTracking().Where(x => x.Id == userId)
                .Select(x => x.AccountCreatedAt).SingleOrDefaultAsync(cancellationToken);
            if (createdAt is DateTimeOffset accountCreatedAt)
                events.Add(new(accountCreatedAt, "account", "Hesap oluşturuldu", "EnvanterTR kullanıcı hesabı açıldı.", null));
            var memberships = await (from membership in db.CompanyMemberships.AsNoTracking()
                                     join company in db.Companies.AsNoTracking() on membership.CompanyId equals company.Id
                                     where membership.UserId == userId
                                     orderby membership.CreatedAt descending
                                     select new { membership.CompanyId, company.Name, membership.RoleKey, membership.CreatedAt })
                .Take(perSourceLimit + 1).ToListAsync(cancellationToken);
            if (memberships.Count > perSourceLimit) { memberships.RemoveAt(memberships.Count - 1); truncated = true; }
            var companyIds = memberships.Select(x => x.CompanyId).Distinct().ToArray();
            foreach (var membership in memberships)
                events.Add(new(membership.CreatedAt, "membership", "Şirket üyeliği başladı",
                    $"Rol: {membership.RoleKey}", membership.Name));

            var subscriptions = await (from subscription in db.TenantSubscriptions.AsNoTracking()
                                       join plan in db.SubscriptionPlans.AsNoTracking() on subscription.PlanId equals plan.Id
                                       join company in db.Companies.AsNoTracking() on subscription.CompanyId equals company.Id
                                       where companyIds.Contains(subscription.CompanyId)
                                       orderby subscription.CreatedAt descending
                                       select new { subscription.CreatedAt, subscription.Status, subscription.IsDemo,
                                           subscription.StartsAt, subscription.EndsAt, subscription.PaymentReference,
                                           plan.Name, CompanyName = company.Name })
                .Take(perSourceLimit + 1).ToListAsync(cancellationToken);
            if (subscriptions.Count > perSourceLimit) { subscriptions.RemoveAt(subscriptions.Count - 1); truncated = true; }
            foreach (var subscription in subscriptions)
                events.Add(new(subscription.CreatedAt, subscription.IsDemo ? "demo" : "subscription",
                    subscription.IsDemo ? "Demo aboneliği oluşturuldu" : "Abonelik oluşturuldu",
                    $"{subscription.Name} · {subscription.Status} · {subscription.StartsAt:yyyy-MM-dd} – {subscription.EndsAt:yyyy-MM-dd}",
                    subscription.CompanyName, subscription.PaymentReference));

            var orders = await (from order in db.CompanyCheckoutOrders.AsNoTracking()
                                join company in db.Companies.AsNoTracking() on order.CompanyId equals company.Id
                                where order.CompanyId != null && companyIds.Contains(order.CompanyId.Value)
                                orderby order.CreatedAt descending
                                select new { order.CreatedAt, order.PaidAt, order.Status, order.Amount, order.Currency,
                                    order.PaymentReference, order.TermDays, CompanyName = company.Name })
                .Take(perSourceLimit + 1).ToListAsync(cancellationToken);
            if (orders.Count > perSourceLimit) { orders.RemoveAt(orders.Count - 1); truncated = true; }
            foreach (var order in orders)
            {
                events.Add(new(order.CreatedAt, "purchase", "Satın alma kaydı oluşturuldu",
                    $"{(order.TermDays == 365 ? "Yıllık" : "Aylık")} dönem · {order.Status}", order.CompanyName,
                    order.PaymentReference, order.Amount, order.Currency));
                if (order.PaidAt is DateTimeOffset paidAt)
                    events.Add(new(paidAt, "payment", "Ödeme doğrulandı", "Sağlayıcı ödemesi siparişle eşleşti.",
                        order.CompanyName, order.PaymentReference, order.Amount, order.Currency));
            }

            var normalizedEmail = account.Email?.Trim().ToLowerInvariant();
            var demoRequests = await db.PlatformDemoRequests.AsNoTracking()
                .Where(request => (normalizedEmail != null && request.Email.ToLower() == normalizedEmail) ||
                    (request.CompanyId != null && companyIds.Contains(request.CompanyId.Value)))
                .OrderByDescending(request => request.CreatedAt)
                .Select(request => new { request.Id, request.CompanyName, request.Status, request.CreatedAt,
                    request.UpdatedAt, request.DemoGrantedAt, request.CompanyId })
                .Take(perSourceLimit + 1).ToListAsync(cancellationToken);
            if (demoRequests.Count > perSourceLimit) { demoRequests.RemoveAt(demoRequests.Count - 1); truncated = true; }
            foreach (var request in demoRequests)
            {
                events.Add(new(request.CreatedAt, "demo-request", "Demo talebi alındı",
                    $"Talep durumu: {request.Status}", request.CompanyName));
                if (request.UpdatedAt > request.CreatedAt)
                    events.Add(new(request.UpdatedAt, "demo-request", "Demo talebi güncellendi",
                        $"Güncel durum: {request.Status}", request.CompanyName));
                if (request.DemoGrantedAt is DateTimeOffset grantedAt)
                    events.Add(new(grantedAt, "demo", "Demo erişimi tanımlandı", "Demo aboneliği başlatıldı.", request.CompanyName));
            }

            var demoRequestIds = demoRequests.Select(x => x.Id).ToArray();
            var subscriptionIds = await db.TenantSubscriptions.AsNoTracking()
                .Where(x => companyIds.Contains(x.CompanyId)).OrderByDescending(x => x.CreatedAt)
                .Select(x => x.Id).Take(perSourceLimit + 1).ToListAsync(cancellationToken);
            if (subscriptionIds.Count > perSourceLimit) { subscriptionIds = subscriptionIds.Take(perSourceLimit).ToList(); truncated = true; }
            var relevantActions = new[] { "LoginSucceeded", "LoginFailed", "PasswordChanged", "PasswordReset",
                "MemberInvited", "MemberInvitationResent", "MemberInvitationEmailSent", "MemberInvitationEmailFailed",
                "DemoGranted", "SubscriptionExtended", "SubscriptionManuallyAdjusted", "CompanySuspended",
                "CompanyReactivated", "PaidCompanyProvisioned", "DemoRequestUpdated", "DemoRequestStatusChanged" };
            var platformAudits = await (from audit in db.PlatformAuditEntries.AsNoTracking()
                                        join actor in db.Users.AsNoTracking() on audit.ActorUserId equals actor.Id into actors
                                        from actor in actors.DefaultIfEmpty()
                                        where ((audit.TargetType == "Account" || audit.TargetType == "PlatformAccount" || audit.TargetType == "CompanyMembership") && audit.TargetId == userId) ||
                                            (audit.TargetType == "TenantSubscription" && subscriptionIds.Contains(audit.TargetId ?? Guid.Empty)) ||
                                            (audit.TargetType == "PlatformDemoRequest" && demoRequestIds.Contains(audit.TargetId ?? Guid.Empty)) ||
                                            (audit.TargetType == "Company" && companyIds.Contains(audit.TargetId ?? Guid.Empty) && relevantActions.Contains(audit.Action))
                                        orderby audit.OccurredAt descending
                                        select new { audit.Action, audit.TargetType, audit.OccurredAt, audit.CompanyId,
                                            ActorName = actor == null ? null : actor.DisplayName })
                .Take(perSourceLimit + 1).ToListAsync(cancellationToken);
            if (platformAudits.Count > perSourceLimit) { platformAudits.RemoveAt(platformAudits.Count - 1); truncated = true; }
            var auditCompanyIds = platformAudits.Where(x => x.CompanyId.HasValue).Select(x => x.CompanyId!.Value)
                .Concat(companyIds).Distinct().ToArray();
            var companyNames = await db.Companies.AsNoTracking().Where(x => auditCompanyIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name }).ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
            foreach (var audit in platformAudits)
            {
                var title = audit.Action switch
                {
                    "LoginSucceeded" => "Oturum açıldı", "LoginFailed" => "Başarısız giriş denemesi",
                    "PasswordChanged" => "Parola değiştirildi", "PasswordReset" => "Parola kurtarma ile parola yenilendi",
                    "MemberInvited" => "Üyelik daveti oluşturuldu", "MemberInvitationResent" => "Üyelik daveti yeniden gönderildi",
                    "MemberInvitationEmailSent" => "Davet e-postası gönderildi", "MemberInvitationEmailFailed" => "Davet e-postası gönderilemedi",
                    "DemoGranted" => "Demo erişimi tanımlandı", "SubscriptionExtended" => "Abonelik süresi uzatıldı",
                    "SubscriptionManuallyAdjusted" => "Abonelik elle düzenlendi", "CompanySuspended" => "Şirket askıya alındı",
                    "CompanyReactivated" => "Şirket yeniden etkinleştirildi", "PaidCompanyProvisioned" => "Ücretli şirket hesabı oluşturuldu",
                    "DemoRequestUpdated" => "Demo talebi güncellendi", "DemoRequestStatusChanged" => "Demo talebi aşaması değişti",
                    "MemberRoleChanged" => "Şirket üyelik rolü değiştirildi", "MemberRemoved" => "Şirket üyeliği kaldırıldı",
                    _ => audit.Action
                };
                var companyName = audit.CompanyId is Guid id ? companyNames.GetValueOrDefault(id) : null;
                events.Add(new(audit.OccurredAt, "audit", title,
                    audit.ActorName is null ? "Platform işlemi" : $"İşlemi yapan: {audit.ActorName}", companyName));
            }

            var membershipAudits = await (from audit in db.AuditEntries.AsNoTracking()
                                          join company in db.Companies.AsNoTracking() on audit.CompanyId equals company.Id
                                          where audit.TargetType == "CompanyMembership" && audit.TargetId == userId && companyIds.Contains(audit.CompanyId)
                                          orderby audit.OccurredAt descending
                                          select new { audit.Action, audit.OccurredAt, company.Name })
                .Take(perSourceLimit + 1).ToListAsync(cancellationToken);
            if (membershipAudits.Count > perSourceLimit) { membershipAudits.RemoveAt(membershipAudits.Count - 1); truncated = true; }
            foreach (var audit in membershipAudits)
                events.Add(new(audit.OccurredAt, "membership", audit.Action switch
                {
                    "MemberAdded" => "Şirket üyeliği eklendi", "MemberRemoved" => "Şirket üyeliği kaldırıldı",
                    "EmployeeLinkChanged" => "Çalışan profili bağlantısı değiştirildi", _ => "Üyelik kaydı güncellendi"
                }, "Şirket üyeliği işlemi", audit.Name));

            var skip = (page - 1) * pageSize;
            var pageItems = events.OrderByDescending(x => x.OccurredAt).ThenBy(x => x.EventType).ThenBy(x => x.Title)
                .Skip(skip).Take(pageSize).ToArray();
            return Results.Ok(new { items = pageItems, page, pageSize, hasMore = events.Count > skip + pageItems.Length, truncated });
        });

        admin.MapPatch("/members/{userId:guid}/memberships/{companyId:guid}/role", async (Guid userId, Guid companyId,
            [FromBody] UpdatePlatformMemberRoleRequest request, ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var allowedRoles = new[] { "admin", "asset_manager", "asset_operator", "auditor", "viewer", "hr_operator" };
            var roleKey = request?.RoleKey?.Trim();
            var note = Clean(request?.Note);
            if (request is null || roleKey is null || !allowedRoles.Contains(roleKey, StringComparer.Ordinal) ||
                note is null || note.Length > 1000)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["membership"] = ["Desteklenen bir şirket rolü ve en fazla 1000 karakterlik işlem gerekçesi gereklidir. Şirket sahibi rolü bu ekrandan değiştirilemez."]
                });

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var companyExists = await db.Companies.AsNoTracking().AnyAsync(x => x.Id == companyId, cancellationToken);
            if (!companyExists) return Results.NotFound(new { message = "Şirket bulunamadı." });
            var membership = await db.CompanyMemberships.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.UserId == userId, cancellationToken);
            if (membership is null) return Results.NotFound(new { message = "Şirket üyeliği bulunamadı." });
            if (membership.RoleKey == "owner")
                return Results.Conflict(new { message = "Şirket sahibi rolü bu ekrandan değiştirilemez. Önce şirket sahipliğini ayrı bir akışla devredin." });
            if (membership.RoleKey == roleKey)
                return Results.Ok(new { changed = false, membership.RoleKey, membership.CompanyId, membership.UserId });

            var before = membership.RoleKey;
            membership.RoleKey = roleKey;
            var details = JsonSerializer.Serialize(new { before = new { roleKey = before }, after = new { roleKey }, note });
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId, TargetType = "CompanyMembership", TargetId = userId,
                Action = "RoleChanged", Details = details
            });
            AddAudit(db, user, "MemberRoleChanged", "CompanyMembership", userId, companyId, details);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { changed = true, membership.RoleKey, membership.CompanyId, membership.UserId });
        });

        admin.MapDelete("/members/{userId:guid}/memberships/{companyId:guid}", async (Guid userId, Guid companyId,
            [FromBody] RemovePlatformMembershipRequest request, ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var note = Clean(request?.Note);
            if (request is null || note is null || note.Length > 1000)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["membership"] = ["Üyeliği kaldırmak için en fazla 1000 karakterlik işlem gerekçesi gereklidir."]
                });

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var companyExists = await db.Companies.AsNoTracking().AnyAsync(x => x.Id == companyId, cancellationToken);
            if (!companyExists) return Results.NotFound(new { message = "Şirket bulunamadı." });
            var membership = await db.CompanyMemberships.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.UserId == userId, cancellationToken);
            if (membership is null) return Results.NotFound(new { message = "Şirket üyeliği bulunamadı." });
            if (membership.RoleKey == "owner")
                return Results.Conflict(new { message = "Şirket sahibi üyeliği bu ekrandan kaldırılamaz. Önce şirket sahipliğini ayrı bir akışla devredin." });

            var previousRole = membership.RoleKey;
            var now = DateTimeOffset.UtcNow;
            var activeSessions = await db.UserSessions.Where(x => x.UserId == userId && x.CompanyId == companyId &&
                !x.PlatformAdmin && x.RevokedAt == null).ToListAsync(cancellationToken);
            foreach (var session in activeSessions) session.RevokedAt = now;
            db.CompanyMemberships.Remove(membership);
            var details = JsonSerializer.Serialize(new { before = new { roleKey = previousRole }, after = (object?)null,
                note, revokedCompanySessions = activeSessions.Count });
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId, TargetType = "CompanyMembership", TargetId = userId,
                Action = "MemberRemoved", Details = details
            });
            AddAudit(db, user, "MemberRemoved", "CompanyMembership", userId, companyId, details);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { removed = true, companyId, userId, revokedSessions = activeSessions.Count });
        });

        admin.MapPost("/members/invite", async ([FromBody] PlatformInviteMemberRequest request,
            ClaimsPrincipal user, AppDbContext db, UserManager<EnvanterUser> userManager,
            IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            var fullName = request.FullName?.Trim();
            var email = request.Email?.Trim();
            var roleKey = request.RoleKey?.Trim();
            var allowedRoles = new[] { "admin", "asset_manager", "asset_operator", "auditor", "viewer", "hr_operator" };
            if (request.CompanyId == Guid.Empty || string.IsNullOrWhiteSpace(fullName) || fullName.Length > 160 ||
                string.IsNullOrWhiteSpace(email) || email.Length > 256 || !MailAddress.TryCreate(email, out _) ||
                roleKey is null || !allowedRoles.Contains(roleKey, StringComparer.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["invite"] = ["Etkin şirket, ad, geçerli e-posta ve desteklenen şirket rolü gereklidir."]
                });
            if (!Guid.TryParse(user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var actorId))
                return Results.Forbid();

            var emailSent = false;
            var resent = false;
            Guid invitedUserId;
            string companyName;
            string displayName;
            bool emailConfirmed;
            await using (var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
            {
                var company = await db.Companies.FromSqlInterpolated(
                        $"SELECT * FROM \"Companies\" WHERE \"Id\" = {request.CompanyId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken);
                if (company is null) return Results.NotFound(new { message = "Şirket bulunamadı." });
                if (!company.IsActive) return Results.Conflict(new { message = "Pasif şirkete kullanıcı eklenemez." });
                companyName = company.Name;

                var now = DateTimeOffset.UtcNow;
                var subscription = await (from item in db.TenantSubscriptions.AsNoTracking()
                    join plan in db.SubscriptionPlans.AsNoTracking() on item.PlanId equals plan.Id
                    where item.CompanyId == request.CompanyId && (item.Status == "Trial" || item.Status == "Active") &&
                        item.StartsAt <= now && item.EndsAt > now
                    select new { item.Id, plan.DeploymentKind, plan.MaxLoginUsers }).SingleOrDefaultAsync(cancellationToken);
                if (subscription is null)
                    return Results.Conflict(new { message = "Şirketin etkin deneme veya aboneliği bulunamadı. Kullanıcı davetinden önce lisansı etkinleştirin." });

                var account = await userManager.FindByEmailAsync(email);
                var membership = account is null ? null : await db.CompanyMemberships.SingleOrDefaultAsync(
                    item => item.CompanyId == request.CompanyId && item.UserId == account.Id, cancellationToken);
                if (membership is not null)
                {
                    if (account!.EmailConfirmed)
                        return Results.Conflict(new { message = "Bu hesap şirketin zaten üyesi." });
                    if (membership.RoleKey != roleKey)
                        return Results.Conflict(new { message = "Bekleyen davetin rolü farklı. Önce üyelik rolünü güncelleyin." });
                    resent = true;
                    invitedUserId = account.Id;
                    displayName = account.DisplayName;
                    emailConfirmed = false;
                    AddAudit(db, user, "MemberInvitationResent", "CompanyMembership", invitedUserId, request.CompanyId,
                        $"{roleKey} rolündeki bekleyen üye daveti yeniden gönderim için istendi.");
                    await db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                else
                {
                    var memberCount = await db.CompanyMemberships.CountAsync(item => item.CompanyId == request.CompanyId, cancellationToken);
                    if (subscription.DeploymentKind != "cloud" && subscription.MaxLoginUsers is int seatLimit && memberCount >= seatLimit)
                        return Results.Conflict(new { message = $"Bu plan {seatLimit} giriş hesabıyla sınırlı. Kullanıcı eklemek için planı yükseltin." });

                    if (account is null)
                    {
                        account = new EnvanterUser
                        {
                            UserName = email,
                            Email = email,
                            EmailConfirmed = false,
                            DisplayName = fullName
                        };
                        var temporaryPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "aA1!";
                        var createResult = await userManager.CreateAsync(account, temporaryPassword);
                        if (!createResult.Succeeded)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            return Results.Conflict(new { message = "Kullanıcı oluşturulamadı. E-posta daha önce kaydedilmiş olabilir." });
                        }
                    }

                    invitedUserId = account.Id;
                    displayName = account.DisplayName;
                    emailConfirmed = account.EmailConfirmed;
                    db.CompanyMemberships.Add(new CompanyMembership
                    {
                        CompanyId = request.CompanyId, UserId = account.Id, RoleKey = roleKey
                    });
                    db.AuditEntries.Add(new AuditEntry
                    {
                        CompanyId = request.CompanyId, ActorUserId = actorId, TargetType = "CompanyMembership",
                        TargetId = account.Id, Action = "Invited", Details = $"{roleKey} rolüyle platform yöneticisi tarafından eklendi."
                    });
                    AddAudit(db, user, "MemberInvited", "CompanyMembership", account.Id, request.CompanyId,
                        $"{roleKey} rolüyle üyelik oluşturuldu; şirket yöneticisi platform işlemi.");
                    try
                    {
                        await db.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch (DbUpdateException)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Results.Conflict(new { message = "Üyelik oluşturma çakıştı. Listeyi yenileyip tekrar deneyin." });
                    }
                }
            }

            try
            {
                if (emailConfirmed)
                    await AccountEndpoints.SendCompanyAccessAddedEmailAsync(email, displayName, companyName, roleKey,
                        configuration, cancellationToken);
                else
                {
                    var account = await userManager.FindByIdAsync(invitedUserId.ToString());
                    if (account is null) return Results.Problem("Üyelik kaydedildi ancak davet hesabı bulunamadı.", statusCode: StatusCodes.Status500InternalServerError);
                    var token = await userManager.GeneratePasswordResetTokenAsync(account);
                    await AccountEndpoints.SendAccountInvitationEmailAsync(email, displayName, token, configuration, cancellationToken);
                }
                emailSent = true;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Membership is committed; the platform operator can repeat the same pending invitation.
            }

            AddAudit(db, user, emailSent ? "MemberInvitationEmailSent" : "MemberInvitationEmailFailed",
                "CompanyMembership", invitedUserId, request.CompanyId,
                emailConfirmed ? "Mevcut hesaba şirket erişim bildirimi gönderildi." : "Parola belirleme daveti " + (emailSent ? "gönderildi." : "gönderilemedi; yeniden denenebilir."));
            await db.SaveChangesAsync(cancellationToken);
            var message = emailSent
                ? emailConfirmed ? "Mevcut hesap şirkete eklendi ve erişim bildirimi gönderildi." : resent ? "Bekleyen davet yeniden gönderildi." : "Üye eklendi; parola belirleme daveti gönderildi."
                : resent ? "Davet kaydı duruyor ancak e-posta gönderilemedi. Aynı daveti yeniden deneyin." : "Üyelik kaydedildi ancak e-posta gönderilemedi. E-posta ayarlarını kontrol edip aynı daveti yeniden deneyin.";
            return resent
                ? Results.Ok(new { invitationSent = emailSent, resent, userId = invitedUserId, message })
                : Results.Created($"/api/platform/members/{invitedUserId}", new { invitationSent = emailSent, resent, userId = invitedUserId, message });
        }).RequireRateLimiting("login");

        admin.MapGet("/demo-requests", async (AppDbContext db, CancellationToken cancellationToken,
            [FromQuery] string? q, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        {
            page = Math.Clamp(page, 1, 1_000_000);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            if (q is { Length: > 120 }) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama en fazla 120 karakter olabilir."] });
            var allowedStatuses = new[] { "New", "Contacted", "Qualified", "DemoScheduled", "DemoGranted", "Converted", "Closed" };
            if (!string.IsNullOrWhiteSpace(status) && !allowedStatuses.Contains(status, StringComparer.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Durum filtresi desteklenmiyor."] });

            var query = from lead in db.PlatformDemoRequests.AsNoTracking()
                        join adminUser in db.Users.AsNoTracking() on lead.AssignedAdminUserId equals adminUser.Id into adminUsers
                        from assignedUser in adminUsers.DefaultIfEmpty()
                        join company in db.Companies.AsNoTracking() on lead.CompanyId equals company.Id into companies
                        from linkedCompany in companies.DefaultIfEmpty()
                        select new
                        {
                            lead.Id, lead.CompanyName, lead.ContactName, lead.Email, lead.Phone, lead.Source,
                            lead.Status, lead.CreatedAt, lead.UpdatedAt, lead.FollowUpAt, lead.LastContactedAt,
                            lead.DemoGrantedAt, lead.AssignedAdminUserId,
                            AssignedAdminName = assignedUser == null ? null : assignedUser.DisplayName,
                            lead.CompanyId, LinkedCompanyName = linkedCompany == null ? null : linkedCompany.Name
                        };
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.CompanyName.Contains(term) || x.ContactName.Contains(term) ||
                    x.Email.Contains(term) || (x.Phone != null && x.Phone.Contains(term)));
            }
            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderBy(x => x.Status == "Closed" || x.Status == "Converted")
                .ThenBy(x => x.FollowUpAt ?? DateTimeOffset.MaxValue).ThenByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return Results.Ok(new { page, pageSize, total, items });
        });

        admin.MapGet("/demo-requests/{requestId:guid}", async (Guid requestId, AppDbContext db, CancellationToken cancellationToken) =>
        {
            var lead = await db.PlatformDemoRequests.AsNoTracking().Where(x => x.Id == requestId)
                .Select(x => new { x.Id, x.CompanyName, x.ContactName, x.Email, x.Phone, x.JobTitle, x.TeamSize,
                    x.AssetCount, x.Message, x.Source, x.Status, x.ConsentAt, x.ConsentNoticeVersion, x.CreatedAt, x.UpdatedAt,
                    x.AssignedAdminUserId, x.FollowUpAt, x.LastContactedAt, x.CompanyId, x.DemoGrantedAt,
                    LinkedCompanyName = db.Companies.Where(company => company.Id == x.CompanyId).Select(company => company.Name).FirstOrDefault() })
                .SingleOrDefaultAsync(cancellationToken);
            if (lead is null) return Results.NotFound();
            var history = await db.PlatformAuditEntries.AsNoTracking().Where(x => x.TargetType == "PlatformDemoRequest" && x.TargetId == requestId)
                .OrderByDescending(x => x.OccurredAt).Take(100)
                .Select(x => new { x.Action, x.Details, x.OccurredAt, Actor = db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.DisplayName).FirstOrDefault() })
                .ToListAsync(cancellationToken);
            return Results.Ok(new { lead, history });
        });

        admin.MapPatch("/demo-requests/{requestId:guid}", async (Guid requestId, [FromBody] UpdatePlatformDemoRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            var allowedStatuses = new[] { "New", "Contacted", "Qualified", "DemoScheduled", "DemoGranted", "Converted", "Closed" };
            if (!allowedStatuses.Contains(request.Status, StringComparer.Ordinal) || request.Note?.Length > 1000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = ["Desteklenmeyen durum veya fazla uzun yönetici notu."] });
            var lead = await db.PlatformDemoRequests.SingleOrDefaultAsync(x => x.Id == requestId, cancellationToken);
            if (lead is null) return Results.NotFound();
            if (request.AssignedAdminUserId is Guid assignedId && !await db.PlatformAdministrators.AnyAsync(x => x.UserId == assignedId, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assignedAdminUserId"] = ["Atanan kullanıcı platform yöneticisi değil."] });
            if (request.CompanyId is Guid companyId && !await db.Companies.AnyAsync(x => x.Id == companyId, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["companyId"] = ["Bağlanacak şirket bulunamadı."] });
            if ((request.Status is "DemoGranted" or "Converted") && request.CompanyId is null)
                return Results.Conflict(new { message = "Demo/ücretli müşteri aşaması için önce firma hesabını bağlayın." });

            DateTimeOffset? demoGrantedAt = lead.DemoGrantedAt;
            if (request.Status == "DemoGranted")
            {
                var now = DateTimeOffset.UtcNow;
                var demoSubscription = await db.TenantSubscriptions.AsNoTracking()
                    .Where(x => x.CompanyId == request.CompanyId && x.IsDemo && x.Status == "Trial" &&
                        x.StartsAt <= now && x.EndsAt > now)
                    .OrderBy(x => x.StartsAt).Select(x => new { x.StartsAt }).FirstOrDefaultAsync(cancellationToken);
                if (demoSubscription is null)
                    return Results.Conflict(new { message = "Bağlı şirkette süresi devam eden demo bulunamadı. Önce panelden demo açın." });
                demoGrantedAt = demoSubscription.StartsAt;
            }
            if (request.Status == "Converted" && !await db.TenantSubscriptions.AnyAsync(x =>
                x.CompanyId == request.CompanyId && !x.IsDemo && x.Status == "Active" &&
                x.StartsAt <= DateTimeOffset.UtcNow && x.EndsAt > DateTimeOffset.UtcNow, cancellationToken))
                return Results.Conflict(new { message = "Dönüşüm aşaması için bağlı şirkette ücretli abonelik bulunmalıdır." });

            var previousStatus = lead.Status;
            var previousCompanyId = lead.CompanyId;
            var previousAssigneeId = lead.AssignedAdminUserId;
            var previousFollowUpAt = lead.FollowUpAt;
            lead.Status = request.Status;
            lead.AssignedAdminUserId = request.AssignedAdminUserId;
            lead.FollowUpAt = request.FollowUpAt;
            lead.CompanyId = request.CompanyId;
            lead.DemoGrantedAt = demoGrantedAt;
            lead.UpdatedAt = DateTimeOffset.UtcNow;
            if (request.Status != previousStatus && (request.Status is "Contacted" or "Qualified" or "DemoScheduled"))
                lead.LastContactedAt = lead.UpdatedAt;
            var auditDetails = JsonSerializer.Serialize(new
            {
                previousStatus, currentStatus = lead.Status, previousCompanyId, currentCompanyId = lead.CompanyId,
                previousAssigneeId, currentAssigneeId = lead.AssignedAdminUserId,
                previousFollowUpAt, currentFollowUpAt = lead.FollowUpAt,
                note = Clean(request.Note)
            });
            AddAudit(db, user, previousStatus == lead.Status ? "DemoRequestUpdated" : "DemoRequestStatusChanged",
                "PlatformDemoRequest", lead.Id, lead.CompanyId, auditDetails);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { lead.Id, lead.Status, lead.AssignedAdminUserId, lead.FollowUpAt,
                lead.LastContactedAt, lead.CompanyId, lead.DemoGrantedAt, lead.UpdatedAt });
        }).RequireRateLimiting("login");

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
                    lifecycleStage = x.CustomerLifecycleStage,
                    lifecycleOwnerId = x.PlatformOwnerUserId,
                    lifecycleOwnerName = db.Users.Where(account => account.Id == x.PlatformOwnerUserId)
                        .Select(account => account.DisplayName).FirstOrDefault(),
                    nextFollowUpAt = x.PlatformNextFollowUpAt,
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
                    x.BrandLogoPath, x.BrandPrimaryColor, lifecycleStage = x.CustomerLifecycleStage,
                    lifecycleOwnerId = x.PlatformOwnerUserId, nextFollowUpAt = x.PlatformNextFollowUpAt,
                    lifecycleOwnerName = db.Users.Where(account => account.Id == x.PlatformOwnerUserId)
                        .Select(account => account.DisplayName).FirstOrDefault() })
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
            var checkoutOrders = await (from order in db.CompanyCheckoutOrders.AsNoTracking()
                                         join plan in db.SubscriptionPlans.AsNoTracking() on order.PlanId equals plan.Id
                                         where order.CompanyId == companyId
                                         orderby order.CreatedAt descending
                                         select new
                                         {
                                             order.Id, plan.Name, plan.Code, order.TermDays, order.Amount, order.Currency,
                                             order.Status, order.PaymentReference, order.CreatedAt, order.PaidAt
                                         })
                .Take(50).ToListAsync(cancellationToken);
            var lifecycleHistory = await (from audit in db.PlatformAuditEntries.AsNoTracking()
                join account in db.Users.AsNoTracking() on audit.ActorUserId equals account.Id
                where audit.CompanyId == companyId && audit.Action == "CustomerLifecycleUpdated"
                orderby audit.OccurredAt descending
                select new { audit.OccurredAt, audit.Details, actorName = account.DisplayName })
                .Take(20).ToListAsync(cancellationToken);
            return Results.Ok(new { company, memberCount, members, membersTruncated = memberCount > members.Count,
                subscriptions, checkoutOrders, lifecycleHistory });
        });

        admin.MapPatch("/companies/{companyId:guid}/lifecycle", async (Guid companyId,
            [FromBody] UpdatePlatformCustomerLifecycleRequest request, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (request is null) return Results.BadRequest(new { message = "Müşteri aşaması bilgisi gereklidir." });
            var allowedStages = new[] { "Unclassified", "Onboarding", "Active", "RenewalDue", "AtRisk", "Churned" };
            var errors = new Dictionary<string, string[]>();
            if (!allowedStages.Contains(request.Stage, StringComparer.Ordinal))
                errors["stage"] = ["Müşteri yaşam döngüsü aşaması geçersiz."];
            if (request.Note is { Length: > 1000 }) errors["note"] = ["İç not en fazla 1000 karakter olabilir."];
            if (request.NextFollowUpAt is DateTimeOffset followUp && followUp > DateTimeOffset.UtcNow.AddYears(5))
                errors["nextFollowUpAt"] = ["Takip tarihi en fazla beş yıl ileri olabilir."];
            if (request.AssignedAdminUserId is Guid assignedId && !await db.PlatformAdministrators.AsNoTracking()
                .AnyAsync(x => x.UserId == assignedId, cancellationToken))
                errors["assignedAdminUserId"] = ["Atanan kullanıcı platform yöneticisi değil."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            var company = await db.Companies.SingleOrDefaultAsync(x => x.Id == companyId, cancellationToken);
            if (company is null) return Results.NotFound();
            var before = $"{company.CustomerLifecycleStage}|{company.PlatformOwnerUserId}|{company.PlatformNextFollowUpAt:O}";
            company.CustomerLifecycleStage = request.Stage;
            company.PlatformOwnerUserId = request.AssignedAdminUserId;
            company.PlatformNextFollowUpAt = request.NextFollowUpAt?.ToUniversalTime();
            var after = $"{company.CustomerLifecycleStage}|{company.PlatformOwnerUserId}|{company.PlatformNextFollowUpAt:O}";
            var note = Clean(request.Note);
            var details = $"before={before}; after={after}" + (note is null ? "" : $"; note={note}");
            AddAudit(db, user, "CustomerLifecycleUpdated", "Company", company.Id, company.Id, details);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { companyId, stage = company.CustomerLifecycleStage,
                assignedAdminUserId = company.PlatformOwnerUserId, nextFollowUpAt = company.PlatformNextFollowUpAt });
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
