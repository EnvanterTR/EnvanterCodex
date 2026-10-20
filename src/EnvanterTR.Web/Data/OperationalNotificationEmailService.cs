using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed class OperationalNotificationEmailService(
    AppDbContext db,
    IConfiguration configuration,
    ILogger<OperationalNotificationEmailService> logger)
{
    private static readonly string[] AllowedKinds = ["maintenance", "audit", "license", "warranty", "return", "reservation", "assignment", "asset-life", "subscription"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan StaleClaimAge = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromHours(1);

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        if (!TryGetMailConfiguration(out var host, out var port, out var fromAddress, out var fromName, out var baseUri))
            return;

        // The hosted worker can start while a fresh database is still applying migrations.
        // Wait for both the preference column and digest table before querying the EF model.
        var schemaReady = await db.Database.SqlQueryRaw<bool>("""
            SELECT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = current_schema()
                  AND table_name = 'OperationalNotificationPreferences'
                  AND column_name = 'EmailEnabledKindsJson'
            ) AND EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = current_schema()
                  AND table_name = 'OperationalNotificationEmailDigests'
            ) AS "Value"
            """).SingleAsync(cancellationToken);
        if (!schemaReady) return;

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var from = today.AddDays(-7);
        var to = today.AddDays(30);
        var preferences = await db.OperationalNotificationPreferences.AsNoTracking()
            .Where(x => x.EmailEnabledKindsJson != "[]")
            .OrderBy(x => x.CompanyId).ThenBy(x => x.UserId)
            .ToListAsync(cancellationToken);

        foreach (var preference in preferences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var kinds = ParseKinds(preference.EmailEnabledKindsJson);
            if (kinds.Length == 0) continue;

            var recipient = await (from membership in db.CompanyMemberships.AsNoTracking()
                                   join user in db.Users.AsNoTracking() on membership.UserId equals user.Id
                                   join company in db.Companies.AsNoTracking() on membership.CompanyId equals company.Id
                                   where membership.CompanyId == preference.CompanyId && membership.UserId == preference.UserId &&
                                         company.IsActive && user.EmailConfirmed && user.Email != null
                                   select new { user.Email, user.DisplayName }).SingleOrDefaultAsync(cancellationToken);
            if (recipient is null || !MailAddress.TryCreate(recipient.Email, out var parsedAddress) ||
                !string.Equals(parsedAddress.Address, recipient.Email, StringComparison.OrdinalIgnoreCase)) continue;

            var items = await GetUpcomingItemsAsync(preference.CompanyId, preference.UserId, kinds, from, to, cancellationToken);
            if (items.Count == 0) continue;

            var digest = await TryClaimDigestAsync(preference.CompanyId, preference.UserId, today, now, cancellationToken);
            if (digest is null) continue;

            try
            {
                var language = await GetPreferredLanguageAsync(preference.CompanyId, preference.UserId, cancellationToken);
                using var message = BuildMessage(parsedAddress, fromAddress, fromName, baseUri, items, language);
                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = !string.Equals(configuration["Email:Smtp:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 15000
                };
                var username = configuration["Email:Smtp:Username"];
                var password = configuration["Email:Smtp:Password"];
                if (!string.IsNullOrWhiteSpace(username)) client.Credentials = new NetworkCredential(username, password ?? string.Empty);
                await client.SendMailAsync(message, cancellationToken);
                digest.Status = "Sent";
                digest.SentAt = DateTimeOffset.UtcNow;
                digest.LastError = null;
                digest.ConcurrencyToken = Guid.NewGuid();
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Operational email digest delivered for company {CompanyId} and user {UserId} with {ItemCount} items.",
                    preference.CompanyId, preference.UserId, items.Count);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                digest.Status = "Failed";
                digest.LastError = exception.GetType().Name;
                digest.ConcurrencyToken = Guid.NewGuid();
                try { await db.SaveChangesAsync(cancellationToken); }
                catch (DbUpdateException) { db.Entry(digest).State = EntityState.Detached; }
                logger.LogWarning("Operational email digest failed for company {CompanyId} and user {UserId} ({ErrorType}).",
                    preference.CompanyId, preference.UserId, exception.GetType().Name);
            }
        }
    }

    private async Task<OperationalNotificationEmailDigest?> TryClaimDigestAsync(Guid companyId, Guid userId,
        DateOnly deliveryDate, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var digest = await db.OperationalNotificationEmailDigests.SingleOrDefaultAsync(
            x => x.CompanyId == companyId && x.UserId == userId && x.DeliveryDate == deliveryDate, cancellationToken);
        if (digest is null)
        {
            digest = new OperationalNotificationEmailDigest
            {
                CompanyId = companyId, UserId = userId, DeliveryDate = deliveryDate,
                Status = "Sending", StartedAt = now, AttemptCount = 1
            };
            db.OperationalNotificationEmailDigests.Add(digest);
            try { await db.SaveChangesAsync(cancellationToken); return digest; }
            catch (DbUpdateException)
            {
                db.Entry(digest).State = EntityState.Detached;
                return null;
            }
        }

        if (digest.Status == "Sent" ||
            (digest.Status == "Sending" && now - digest.StartedAt < StaleClaimAge) ||
            (digest.Status == "Failed" && now - digest.StartedAt < RetryDelay)) return null;

        digest.Status = "Sending";
        digest.StartedAt = now;
        digest.SentAt = null;
        digest.AttemptCount++;
        digest.LastError = null;
        digest.ConcurrencyToken = Guid.NewGuid();
        try { await db.SaveChangesAsync(cancellationToken); return digest; }
        catch (DbUpdateConcurrencyException)
        {
            db.Entry(digest).State = EntityState.Detached;
            return null;
        }
    }

    private async Task<List<OperationalReminderItem>> GetUpcomingItemsAsync(Guid companyId, Guid userId, string[] kinds,
        DateOnly rangeStart, DateOnly rangeEnd, CancellationToken cancellationToken)
    {
        var items = new List<OperationalReminderItem>();
        var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, userId, cancellationToken);
        var allowedLocations = employeeScope.AllowedLocations;
        var visibleEmployees = employeeScope.Apply(db.Employees.AsNoTracking().Where(employee => employee.CompanyId == companyId));
        var visibleEmployeeIds = visibleEmployees.Select(employee => employee.Id);
        if (kinds.Contains("maintenance", StringComparer.Ordinal))
        {
            var maintenanceQuery = from maintenance in db.AssetMaintenances.AsNoTracking()
                              join asset in db.Assets.AsNoTracking() on new { maintenance.CompanyId, maintenance.AssetId } equals new { asset.CompanyId, AssetId = asset.Id }
                              where maintenance.CompanyId == companyId && maintenance.Status == "Scheduled" &&
                                    maintenance.PlannedOn >= rangeStart && maintenance.PlannedOn <= rangeEnd
                              select new { maintenance, asset };
            if (allowedLocations is not null)
                maintenanceQuery = maintenanceQuery.Where(item => item.asset.LocationCatalogId.HasValue && allowedLocations.Contains(item.asset.LocationCatalogId.Value));
            var rows = await (from item in maintenanceQuery
                              orderby item.maintenance.PlannedOn, item.asset.AssetTag
                              select new OperationalReminderItem("maintenance", item.maintenance.PlannedOn!.Value,
                                  item.asset.AssetTag + " · " + item.asset.Name, item.maintenance.MaintenanceType + (item.asset.Location == null ? "" : " · " + item.asset.Location)))
                .Take(200).ToListAsync(cancellationToken);
            items.AddRange(rows);
        }
        if (kinds.Contains("audit", StringComparer.Ordinal))
        {
            var auditQuery = db.Assets.AsNoTracking()
                .Where(asset => asset.CompanyId == companyId && asset.Status != AssetStatuses.Archived && asset.DeletedAt == null &&
                    asset.NextAuditOn >= rangeStart && asset.NextAuditOn <= rangeEnd);
            if (allowedLocations is not null)
                auditQuery = auditQuery.Where(asset => asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value));
            var rows = await auditQuery
                .OrderBy(asset => asset.NextAuditOn).ThenBy(asset => asset.AssetTag)
                .Select(asset => new OperationalReminderItem("audit", asset.NextAuditOn!.Value,
                    asset.AssetTag + " · " + asset.Name, asset.Location))
                .Take(200).ToListAsync(cancellationToken);
            items.AddRange(rows);
        }
        if (kinds.Contains("license", StringComparer.Ordinal))
        {
            var licenseQuery = db.SoftwareLicenses.AsNoTracking()
                .Where(license => license.CompanyId == companyId && license.ExpirationDate >= rangeStart && license.ExpirationDate <= rangeEnd)
                .LicensesInEmployeeScope(db, companyId, employeeScope);
            var rows = await licenseQuery
                .OrderBy(license => license.ExpirationDate).ThenBy(license => license.Name)
                .Select(license => new OperationalReminderItem("license", license.ExpirationDate!.Value,
                    license.Name, license.Publisher))
                .Take(200).ToListAsync(cancellationToken);
            items.AddRange(rows);
        }
        if (kinds.Contains("warranty", StringComparer.Ordinal))
        {
            var warrantyQuery = db.Assets.AsNoTracking()
                .Where(asset => asset.CompanyId == companyId && asset.DeletedAt == null &&
                    asset.WarrantyExpiresAt >= rangeStart && asset.WarrantyExpiresAt <= rangeEnd);
            if (allowedLocations is not null)
                warrantyQuery = warrantyQuery.Where(asset => asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value));
            var rows = await warrantyQuery
                .OrderBy(asset => asset.WarrantyExpiresAt).ThenBy(asset => asset.AssetTag)
                .Select(asset => new OperationalReminderItem("warranty", asset.WarrantyExpiresAt!.Value,
                    asset.AssetTag + " · " + asset.Name, asset.Location))
                .Take(200).ToListAsync(cancellationToken);
            items.AddRange(rows);
        }
        if (kinds.Contains("return", StringComparer.Ordinal))
        {
            var overdueQuery = db.Assets.AsNoTracking()
                .Where(asset => asset.CompanyId == companyId && asset.DeletedAt == null && asset.Status == AssetStatuses.Deployed &&
                    asset.AssignedEmployeeId != null && asset.ExpectedReturnOn < DateOnly.FromDateTime(DateTime.UtcNow));
            if (allowedLocations is not null)
                overdueQuery = overdueQuery.Where(asset => asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value));
            if (employeeScope.IsRestricted)
                overdueQuery = overdueQuery.Where(asset => visibleEmployeeIds.Contains(asset.AssignedEmployeeId!.Value));
            var overdue = await overdueQuery
                .OrderByDescending(asset => asset.ExpectedReturnOn).ThenBy(asset => asset.AssetTag)
                .Select(asset => new OperationalReminderItem("return", asset.ExpectedReturnOn!.Value,
                    asset.AssetTag + " · " + asset.Name,
                    "Gecikti · " + (db.Employees.Where(employee => employee.CompanyId == companyId && employee.Id == asset.AssignedEmployeeId)
                        .Select(employee => employee.DisplayName).FirstOrDefault() ?? "Çalışan belirtilmemiş")))
                .Take(100).ToListAsync(cancellationToken);
            items.AddRange(overdue);
            var upcomingReturnQuery = db.Assets.AsNoTracking()
                .Where(asset => asset.CompanyId == companyId && asset.DeletedAt == null && asset.Status == AssetStatuses.Deployed &&
                    asset.AssignedEmployeeId != null && asset.ExpectedReturnOn >= DateOnly.FromDateTime(DateTime.UtcNow) && asset.ExpectedReturnOn <= rangeEnd);
            if (allowedLocations is not null)
                upcomingReturnQuery = upcomingReturnQuery.Where(asset => asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value));
            if (employeeScope.IsRestricted)
                upcomingReturnQuery = upcomingReturnQuery.Where(asset => visibleEmployeeIds.Contains(asset.AssignedEmployeeId!.Value));
            var upcomingReturns = await upcomingReturnQuery
                .OrderBy(asset => asset.ExpectedReturnOn).ThenBy(asset => asset.AssetTag)
                .Select(asset => new OperationalReminderItem("return", asset.ExpectedReturnOn!.Value,
                    asset.AssetTag + " · " + asset.Name,
                    "Beklenen iade · " + (db.Employees.Where(employee => employee.CompanyId == companyId && employee.Id == asset.AssignedEmployeeId)
                        .Select(employee => employee.DisplayName).FirstOrDefault() ?? "Çalışan belirtilmemiş")))
                .Take(100).ToListAsync(cancellationToken);
            items.AddRange(upcomingReturns);
        }
        if (kinds.Contains("reservation", StringComparer.Ordinal))
        {
            var membership = await db.CompanyMemberships.AsNoTracking()
                .Where(item => item.CompanyId == companyId && item.UserId == userId)
                .Select(item => new { item.RoleKey, item.EmployeeId }).SingleOrDefaultAsync(cancellationToken);
            if (membership is not null)
            {
                var reviewer = membership.RoleKey is "owner" or "admin" or "asset_manager" or "asset_operator";
                var reservationQuery = from reservation in db.AssetReservations.AsNoTracking()
                                       join asset in db.Assets.AsNoTracking() on new { reservation.CompanyId, AssetId = reservation.AssetId } equals new { asset.CompanyId, AssetId = asset.Id }
                                       join employee in visibleEmployees on new { reservation.CompanyId, reservation.EmployeeId } equals new { employee.CompanyId, EmployeeId = employee.Id }
                                       where reservation.CompanyId == companyId && reservation.Status == "Approved" &&
                                             reservation.StartsOn >= rangeStart && reservation.StartsOn <= rangeEnd && asset.DeletedAt == null
                                       select new { reservation, asset, employee };
                if (!reviewer)
                    reservationQuery = reservationQuery.Where(item => item.reservation.RequestedByUserId == userId ||
                        (membership.EmployeeId != null && item.reservation.EmployeeId == membership.EmployeeId));
                if (allowedLocations is not null)
                    reservationQuery = reservationQuery.Where(item => item.asset.LocationCatalogId.HasValue && allowedLocations.Contains(item.asset.LocationCatalogId.Value));
                var rows = await reservationQuery.OrderBy(item => item.reservation.StartsOn).ThenBy(item => item.asset.AssetTag)
                    .Select(item => new OperationalReminderItem("reservation", item.reservation.StartsOn,
                        item.asset.AssetTag + " · " + item.asset.Name,
                        item.employee.DisplayName + " · " + item.reservation.StartsOn.ToString("yyyy-MM-dd") + " – " + item.reservation.EndsOn.ToString("yyyy-MM-dd")))
                    .Take(200).ToListAsync(cancellationToken);
                items.AddRange(rows);
            }
        }
        if (kinds.Contains("assignment", StringComparer.Ordinal))
        {
            var rangeStartUtc = new DateTimeOffset(rangeStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            var rangeEndExclusiveUtc = new DateTimeOffset(rangeEnd.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            var assignmentQuery = from assetEvent in db.AssetEvents.AsNoTracking()
                              join asset in db.Assets.AsNoTracking() on new { assetEvent.CompanyId, AssetId = assetEvent.AssetId } equals new { asset.CompanyId, AssetId = asset.Id }
                              join employee in visibleEmployees on new { assetEvent.CompanyId, EmployeeId = assetEvent.EmployeeId!.Value } equals new { employee.CompanyId, EmployeeId = employee.Id }
                              where assetEvent.CompanyId == companyId && assetEvent.EventType == "Assigned" && assetEvent.EmployeeId != null &&
                                    assetEvent.OccurredAt >= rangeStartUtc && assetEvent.OccurredAt < rangeEndExclusiveUtc &&
                                    asset.DeletedAt == null
                              select new { assetEvent, asset, employee };
            if (allowedLocations is not null)
                assignmentQuery = assignmentQuery.Where(item => item.asset.LocationCatalogId.HasValue && allowedLocations.Contains(item.asset.LocationCatalogId.Value));
            var rows = await (from item in assignmentQuery
                              orderby item.assetEvent.OccurredAt descending, item.asset.AssetTag
                              select new { item.assetEvent.OccurredAt, item.asset.AssetTag, item.asset.Name, EmployeeName = item.employee.DisplayName })
                .Take(200).ToListAsync(cancellationToken);
            items.AddRange(rows.Select(row => new OperationalReminderItem("assignment",
                DateOnly.FromDateTime(row.OccurredAt.UtcDateTime), row.AssetTag + " · " + row.Name,
                "Zimmetlenen çalışan · " + row.EmployeeName)));
        }
        if (kinds.Contains("asset-life", StringComparer.Ordinal))
        {
            // Depreciation months are the configured service-life estimate from the
            // recorded purchase date. Calculate month arithmetic in .NET to preserve
            // end-of-month behavior consistently with the depreciation report.
            var earliestPurchaseDate = rangeStart.AddMonths(-3600);
            var earliestLifetimeItems = new SortedSet<OperationalReminderItem>(Comparer<OperationalReminderItem>.Create((left, right) =>
            {
                var comparison = left.Date.CompareTo(right.Date);
                if (comparison == 0) comparison = StringComparer.Ordinal.Compare(left.Title, right.Title);
                if (comparison == 0) comparison = StringComparer.Ordinal.Compare(left.Detail, right.Detail);
                return comparison;
            }));
            var rows = from asset in db.Assets.AsNoTracking()
                       join model in db.AssetModels.AsNoTracking() on new { asset.CompanyId, Id = asset.ModelId } equals new { model.CompanyId, Id = (Guid?)model.Id }
                       where asset.CompanyId == companyId && asset.DeletedAt == null && asset.Status != AssetStatuses.Archived &&
                             asset.PurchaseDate >= earliestPurchaseDate && asset.PurchaseDate <= rangeEnd && model.DepreciationMonths != null
                       select new { asset.PurchaseDate, asset.AssetTag, asset.Name, ModelName = model.Name, Months = model.DepreciationMonths!.Value };
            if (allowedLocations is not null)
                rows = rows.Where(item => db.Assets.Any(asset => asset.CompanyId == companyId && asset.AssetTag == item.AssetTag &&
                    asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value)));
            await foreach (var row in rows.OrderBy(item => item.PurchaseDate).ThenBy(item => item.AssetTag)
                               .AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                var date = row.PurchaseDate!.Value.AddMonths(row.Months);
                if (date >= rangeStart && date <= rangeEnd)
                {
                    earliestLifetimeItems.Add(new OperationalReminderItem("asset-life", date,
                        row.AssetTag + " · " + row.Name, "Model: " + row.ModelName));
                    if (earliestLifetimeItems.Count > 500) earliestLifetimeItems.Remove(earliestLifetimeItems.Max!);
                }
            }
            items.AddRange(earliestLifetimeItems);
        }
        if (kinds.Contains("subscription", StringComparer.Ordinal))
        {
            var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
            var milestoneWindowsUtc = new[] { 30, 7, 1 }
                .Select(days =>
                {
                    var start = todayUtc.AddDays(days).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                    return (Start: new DateTimeOffset(start), EndExclusive: new DateTimeOffset(start.AddDays(1)));
                })
                .ToArray();
            var rows = await (from subscription in db.TenantSubscriptions.AsNoTracking()
                              join plan in db.SubscriptionPlans.AsNoTracking() on subscription.PlanId equals plan.Id
                              where subscription.CompanyId == companyId && !subscription.IsDemo && subscription.Status == "Active" &&
                                    ((subscription.EndsAt >= milestoneWindowsUtc[0].Start && subscription.EndsAt < milestoneWindowsUtc[0].EndExclusive) ||
                                     (subscription.EndsAt >= milestoneWindowsUtc[1].Start && subscription.EndsAt < milestoneWindowsUtc[1].EndExclusive) ||
                                     (subscription.EndsAt >= milestoneWindowsUtc[2].Start && subscription.EndsAt < milestoneWindowsUtc[2].EndExclusive))
                              orderby subscription.EndsAt
                              select new { subscription.EndsAt, PlanName = plan.Name })
                .Take(20).ToListAsync(cancellationToken);
            items.AddRange(rows.Select(row => new
                {
                    EndsOn = DateOnly.FromDateTime(row.EndsAt.UtcDateTime),
                    row.PlanName
                })
                .Select(row => new OperationalReminderItem("subscription", row.EndsOn, "EnvanterTR Cloud", row.PlanName)));
        }
        // Reserve digest space for every selected kind before filling the remaining
        // capacity by date, so busy categories cannot starve less frequent reminders.
        var selected = items.GroupBy(item => item.Kind, StringComparer.Ordinal)
            .SelectMany(group => group.OrderBy(item => item.Date).ThenBy(item => item.Title, StringComparer.Ordinal).Take(70))
            .Concat(items.OrderBy(item => item.Date).ThenBy(item => item.Kind, StringComparer.Ordinal).ThenBy(item => item.Title, StringComparer.Ordinal))
            .Distinct()
            .Take(500)
            .OrderBy(item => item.Date).ThenBy(item => item.Kind, StringComparer.Ordinal).ThenBy(item => item.Title, StringComparer.Ordinal)
            .ToList();
        return selected;
    }

    private async Task<string> GetPreferredLanguageAsync(Guid companyId, Guid userId, CancellationToken cancellationToken)
    {
        var json = await db.DashboardPreferences.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.UserId == userId)
            .Select(x => x.VisibleWidgetsJson).SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json)) return "tr";
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("language", out var value) && value.ValueKind == JsonValueKind.String)
            {
                var language = value.GetString();
                if (language is "tr" or "en" or "ru" or "uk" or "mk") return language;
            }
        }
        catch (JsonException) { }
        return "tr";
    }

    private MailMessage BuildMessage(MailAddress recipient, string fromAddress, string fromName,
        Uri publicBaseUri, IReadOnlyList<OperationalReminderItem> items, string language)
    {
        var copy = EmailCopy.For(language);
        var culture = CultureInfo.GetCultureInfo(copy.CultureName);
        var rows = new StringBuilder();
        foreach (var item in items)
        {
            rows.Append("<tr><td style=\"padding:8px;border-bottom:1px solid #e7edf5\">")
                .Append(WebUtility.HtmlEncode(copy.KindLabel(item.Kind))).Append("</td><td style=\"padding:8px;border-bottom:1px solid #e7edf5\">")
                .Append(WebUtility.HtmlEncode(item.Date.ToDateTime(TimeOnly.MinValue).ToString("d", culture))).Append("</td><td style=\"padding:8px;border-bottom:1px solid #e7edf5\">")
                .Append(WebUtility.HtmlEncode(item.Title)).Append("</td><td style=\"padding:8px;border-bottom:1px solid #e7edf5\">")
                .Append(WebUtility.HtmlEncode(copy.TranslateDetail(item.Detail, culture))).Append("</td></tr>");
        }
        var link = WebUtility.HtmlEncode(new Uri(publicBaseUri, "/").AbsoluteUri);
        var body = $"<div style=\"font-family:Arial,sans-serif;color:#25334a;max-width:760px;margin:auto\"><h2>{WebUtility.HtmlEncode(copy.Title)}</h2><p>{WebUtility.HtmlEncode(copy.Intro)}</p><table style=\"width:100%;border-collapse:collapse;text-align:left\"><thead><tr><th style=\"padding:8px\">{WebUtility.HtmlEncode(copy.Type)}</th><th style=\"padding:8px\">{WebUtility.HtmlEncode(copy.Date)}</th><th style=\"padding:8px\">{WebUtility.HtmlEncode(copy.Record)}</th><th style=\"padding:8px\">{WebUtility.HtmlEncode(copy.Description)}</th></tr></thead><tbody>{rows}</tbody></table><p><a href=\"{link}\">{WebUtility.HtmlEncode(copy.OpenApp)}</a></p><p style=\"color:#738197;font-size:12px\">{WebUtility.HtmlEncode(copy.Footer)}</p></div>";
        var message = new MailMessage(new MailAddress(fromAddress, fromName), recipient)
        {
            Subject = copy.Subject,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true,
            Body = body
        };
        return message;
    }

    private bool TryGetMailConfiguration(out string host, out int port, out string fromAddress, out string fromName, out Uri baseUri)
    {
        host = configuration["Email:Smtp:Host"] ?? "";
        fromAddress = configuration["Email:FromAddress"] ?? "";
        fromName = configuration["Email:FromName"] ?? "EnvanterTR";
        Uri? parsedBaseUri = null;
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress) ||
            !MailAddress.TryCreate(fromAddress, out _) || !Uri.TryCreate(configuration["Email:PublicBaseUrl"], UriKind.Absolute, out parsedBaseUri) ||
            parsedBaseUri is null || parsedBaseUri.Scheme is not ("https" or "http") ||
            (parsedBaseUri.Scheme != Uri.UriSchemeHttps && !string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase)) ||
            !int.TryParse(configuration["Email:Smtp:Port"], out port) || port is < 1 or > 65535)
        {
            port = 0;
            baseUri = null!;
            return false;
        }
        baseUri = parsedBaseUri;
        return true;
    }

    private static string[] ParseKinds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var kinds = JsonSerializer.Deserialize<string[]>(json, JsonOptions);
            if (kinds is null || kinds.Length > AllowedKinds.Length || kinds.Any(x => !AllowedKinds.Contains(x, StringComparer.Ordinal)) ||
                kinds.Distinct(StringComparer.Ordinal).Count() != kinds.Length) return [];
            return AllowedKinds.Where(kinds.Contains).ToArray();
        }
        catch (JsonException) { return []; }
    }

    private sealed record EmailCopy(string CultureName, string Title, string Subject, string Intro,
        string Type, string Date, string Record, string Description, string OpenApp, string Footer,
        string Maintenance, string Audit, string License, string Warranty, string Return,
        string Reservation, string Assignment, string AssetLife, string Subscription, string Work,
        string Late, string ExpectedReturn, string AssignedEmployee, string Model)
    {
        public string KindLabel(string kind) => kind switch
        {
            "maintenance" => Maintenance, "audit" => Audit, "license" => License,
            "warranty" => Warranty, "return" => Return, "reservation" => Reservation,
            "assignment" => Assignment, "asset-life" => AssetLife, "subscription" => Subscription, _ => Work
        };

        public string? TranslateDetail(string? detail, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(detail)) return detail;
            var separator = detail.LastIndexOf(" · ", StringComparison.Ordinal);
            if (separator >= 0)
            {
                var dates = detail[(separator + 3)..].Split(" – ", 2, StringSplitOptions.None);
                if (dates.Length == 2 &&
                    DateOnly.TryParseExact(dates[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) &&
                    DateOnly.TryParseExact(dates[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
                    return detail[..(separator + 3)] + start.ToDateTime(TimeOnly.MinValue).ToString("d", culture) + " – " +
                           end.ToDateTime(TimeOnly.MinValue).ToString("d", culture);
            }
            foreach (var (source, translated) in new[]
                     {
                         ("Gecikti · ", Late), ("Beklenen iade · ", ExpectedReturn),
                         ("Zimmetlenen çalışan · ", AssignedEmployee), ("Model: ", Model)
                     })
                if (detail.StartsWith(source, StringComparison.Ordinal)) return translated + detail[source.Length..];
            return detail;
        }

        public static EmailCopy For(string language) => language switch
        {
            "en" => new("en-US", "EnvanterTR operations summary", "EnvanterTR · upcoming operations summary",
                "Selected maintenance, audit, license, warranty, return, reservation, new assignment, end-of-life, and subscription expiry records from the past 7 days and next 30 days:",
                "Type", "Date", "Record", "Details", "Open EnvanterTR", "You received this summary because email reminders are enabled in your notification preferences.",
                "Maintenance", "Physical audit", "License expiry", "Warranty expiry", "Asset return", "Asset reservation", "New assignment", "End of service life", "Subscription expiry", "Task",
                "Overdue · ", "Expected return · ", "Assigned employee · ", "Model: "),
            "ru" => new("ru-RU", "Сводка операций EnvanterTR", "EnvanterTR · сводка предстоящих задач",
                "Выбранные записи о техобслуживании, проверках, лицензиях, гарантии, возвратах, резервациях, новых назначениях, окончании срока службы и подписки за последние 7 и следующие 30 дней:",
                "Тип", "Дата", "Запись", "Описание", "Открыть EnvanterTR", "Вы получили эту сводку, потому что включили напоминания по электронной почте в настройках уведомлений.",
                "Техобслуживание", "Физическая проверка", "Истечение лицензии", "Истечение гарантии", "Возврат актива", "Резервация актива", "Новое назначение", "Окончание срока службы", "Окончание подписки", "Задача",
                "Просрочено · ", "Ожидаемый возврат · ", "Назначенный сотрудник · ", "Модель: "),
            "uk" => new("uk-UA", "Зведення операцій EnvanterTR", "EnvanterTR · зведення майбутніх справ",
                "Вибрані записи про обслуговування, аудит, ліцензії, гарантії, повернення, резервування, нові призначення, завершення строку служби та підписки за останні 7 і наступні 30 днів:",
                "Тип", "Дата", "Запис", "Опис", "Відкрити EnvanterTR", "Ви отримали це зведення, оскільки ввімкнули нагадування електронною поштою в налаштуваннях сповіщень.",
                "Обслуговування", "Фізичний аудит", "Закінчення ліцензії", "Закінчення гарантії", "Повернення активу", "Резервування активу", "Нове призначення", "Завершення строку служби", "Завершення підписки", "Завдання",
                "Прострочено · ", "Очікуване повернення · ", "Призначений працівник · ", "Модель: "),
            "mk" => new("mk-MK", "Преглед на операциите во EnvanterTR", "EnvanterTR · преглед на претстојни задачи",
                "Избрани записи за одржување, ревизија, лиценци, гаранција, враќања, резервации, нови задолжувања, крај на работниот век и истекување на претплатата во изминатите 7 и следните 30 дена:",
                "Тип", "Датум", "Запис", "Опис", "Отвори EnvanterTR", "Овој преглед го добивте бидејќи сте вклучиле потсетници по е-пошта во поставките за известувања.",
                "Одржување", "Физичка ревизија", "Истекување на лиценца", "Истекување на гаранција", "Враќање на опрема", "Резервација на опрема", "Ново задолжување", "Крај на работниот век", "Истекување на претплатата", "Задача",
                "Задоцнето · ", "Очекувано враќање · ", "Задолжен вработен · ", "Модел: "),
            _ => new("tr-TR", "EnvanterTR operasyon özeti", "EnvanterTR · yaklaşan işler özeti",
                "Son 7 gün ve gelecek 30 gün içindeki seçtiğiniz bakım, denetim, lisans, garanti, iade, rezervasyon, yeni zimmet, kullanım ömrü ve abonelik bitişleri:",
                "Tür", "Tarih", "Kayıt", "Açıklama", "EnvanterTR uygulamasını aç", "Bu özeti bildirim tercihlerinde e-posta hatırlatmalarını seçtiğiniz için aldınız.",
                "Bakım planı", "Fiziksel denetim", "Lisans bitişi", "Garanti bitişi", "Varlık iadesi", "Varlık rezervasyonu", "Yeni zimmet", "Kullanım ömrü sonu", "Abonelik bitişi", "İş",
                "Gecikti · ", "Beklenen iade · ", "Zimmetlenen çalışan · ", "Model: ")
        };
    }

    private sealed record OperationalReminderItem(string Kind, DateOnly Date, string Title, string? Detail);
}

public sealed class OperationalNotificationEmailWorker(IServiceScopeFactory scopeFactory,
    ILogger<OperationalNotificationEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<OperationalNotificationEmailService>().RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception)
            {
        logger.LogError("Operational email reminder cycle failed ({ErrorType}).", exception.GetType().Name);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
