using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed class ScheduledReportEmailService(AppDbContext db, IConfiguration configuration,
    ILogger<ScheduledReportEmailService> logger)
{
    public static bool IsConfigured(IConfiguration configuration) => MailSettings.TryCreate(configuration) is not null;

    public async Task RunOnceAsync(CancellationToken ct)
    {
        var settings = MailSettings.TryCreate(configuration);
        if (settings is null) return;
        var ready = await db.Database.SqlQueryRaw<bool>("""
            SELECT EXISTS (SELECT 1 FROM information_schema.tables
                WHERE table_schema = current_schema() AND table_name = 'ScheduledReports') AS "Value"
            """).SingleAsync(ct);
        if (!ready) return;
        var selectionTime = DateTimeOffset.UtcNow;
        var stale = selectionTime.AddMinutes(-30);
        var ids = await db.ScheduledReports.AsNoTracking().Where(x => x.IsEnabled && x.NextDueAt <= selectionTime &&
            (x.RetryAfter == null || x.RetryAfter <= selectionTime) && (x.ClaimedAt == null || x.ClaimedAt < stale))
            .OrderBy(x => x.NextDueAt).ThenBy(x => x.Id).Select(x => x.Id).Take(100).ToListAsync(ct);
        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            var now = DateTimeOffset.UtcNow;
            var staleClaim = now.AddMinutes(-30);
            var token = Guid.NewGuid();
            var version = Guid.NewGuid();
            var claimed = await db.ScheduledReports.Where(x => x.Id == id && x.IsEnabled && x.NextDueAt <= now &&
                (x.RetryAfter == null || x.RetryAfter <= now) && (x.ClaimedAt == null || x.ClaimedAt < staleClaim))
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.ClaimToken, token).SetProperty(x => x.ClaimedAt, now)
                    .SetProperty(x => x.LastAttemptAt, now).SetProperty(x => x.LastStatus, "Sending")
                    .SetProperty(x => x.LastErrorCode, (string?)null).SetProperty(x => x.Version, version)
                    .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1), ct);
            if (claimed != 1) continue;
            var report = await db.ScheduledReports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.ClaimToken == token, ct);
            if (report is null) continue;
            try
            {
                // A time zone may disappear after an OS time-zone database update.
                // Reject an unusable calendar before sending any message.
                _ = ReportScheduleClock.NextDue(report, DateTimeOffset.UtcNow);
                // Resolve the current recipient and permissions on every delivery.
                // Neither an old address nor an old role is stored in the schedule.
                var recipient = await (from membership in db.CompanyMemberships.AsNoTracking()
                    join user in db.Users.AsNoTracking() on membership.UserId equals user.Id
                    join company in db.Companies.AsNoTracking() on membership.CompanyId equals company.Id
                    where membership.CompanyId == report.CompanyId && membership.UserId == report.UserId && company.IsActive &&
                        user.EmailConfirmed && user.Email != null && (user.LockoutEnd == null || user.LockoutEnd <= now)
                    select new { user.Email, CompanyName = company.Name, membership.RoleKey }).SingleOrDefaultAsync(ct);
                var permissions = recipient is null ? (Valid: false, PermissionKeys: Array.Empty<string>()) :
                    await CompanyRoleAuthorization.ResolveAsync(db, report.CompanyId, recipient.RoleKey, ct);
                if (recipient is null || !permissions.Valid || !permissions.PermissionKeys.Contains(CompanyRoleAuthorization.AssetsRead) ||
                    !MailAddress.TryCreate(recipient.Email, out var address) || !string.Equals(address.Address, recipient.Email, StringComparison.OrdinalIgnoreCase) ||
                    !await TenantPlanFeatures.IsEnabledAsync(report.CompanyId, TenantPlanFeatures.Reports, db, ct) ||
                    report.ReportKind == "maintenance" && !await TenantPlanFeatures.IsEnabledAsync(report.CompanyId, TenantPlanFeatures.Maintenance, db, ct))
                {
                    await SkipAsync(report, token, "RecipientOrPermissionUnavailable", ct);
                    continue;
                }
                var document = await ScheduledReportData.BuildAsync(db, report, ct);
                using var message = BuildMessage(settings, report, document, address, recipient.CompanyName);
                // An edit, pause, deletion, or membership deletion invalidates a queued claim.
                if (!await db.ScheduledReports.AnyAsync(x => x.Id == id && x.IsEnabled && x.ClaimToken == token && x.Version == version, ct)) continue;
                using var client = new SmtpClient(settings.Host, settings.Port)
                {
                    EnableSsl = !string.Equals(configuration["Email:Smtp:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase),
                    DeliveryMethod = SmtpDeliveryMethod.Network, Timeout = 15000
                };
                if (!string.IsNullOrWhiteSpace(configuration["Email:Smtp:Username"]))
                    client.Credentials = new NetworkCredential(configuration["Email:Smtp:Username"], configuration["Email:Smtp:Password"] ?? "");
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(30));
                await client.SendMailAsync(message, timeout.Token);
                var sentAt = DateTimeOffset.UtcNow;
                var next = ReportScheduleClock.NextDue(report, sentAt);
                await db.ScheduledReports.Where(x => x.Id == id && x.ClaimToken == token).ExecuteUpdateAsync(set => set
                    .SetProperty(x => x.LastSentAt, sentAt).SetProperty(x => x.LastStatus, "Sent").SetProperty(x => x.LastErrorCode, (string?)null)
                    .SetProperty(x => x.NextDueAt, next).SetProperty(x => x.RetryAfter, (DateTimeOffset?)null)
                    .SetProperty(x => x.ClaimedAt, (DateTimeOffset?)null).SetProperty(x => x.ClaimToken, (Guid?)null)
                    .SetProperty(x => x.AttemptCount, 0).SetProperty(x => x.Version, Guid.NewGuid()), ct);
                logger.LogInformation("Scheduled report {ReportId} delivered for company {CompanyId}.", id, report.CompanyId);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (ReportScheduleException)
            {
                await db.ScheduledReports.Where(x => x.Id == id && x.ClaimToken == token).ExecuteUpdateAsync(set => set
                    .SetProperty(x => x.IsEnabled, false).SetProperty(x => x.LastStatus, "Failed")
                    .SetProperty(x => x.LastErrorCode, "InvalidSchedule")
                    .SetProperty(x => x.RetryAfter, (DateTimeOffset?)null).SetProperty(x => x.ClaimedAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.ClaimToken, (Guid?)null).SetProperty(x => x.Version, Guid.NewGuid()), ct);
                logger.LogWarning("Scheduled report {ReportId} paused because its calendar is unavailable.", id);
            }
            catch (Exception exception)
            {
                var exhausted = report.AttemptCount >= 3;
                var retry = DateTimeOffset.UtcNow.AddHours(1);
                var next = exhausted ? ReportScheduleClock.NextDue(report, DateTimeOffset.UtcNow) : report.NextDueAt;
                var code = exception is OperationCanceledException ? "DeliveryTimeout" : exception is SmtpException ? "SmtpDeliveryFailed" : "ReportDeliveryFailed";
                await db.ScheduledReports.Where(x => x.Id == id && x.ClaimToken == token).ExecuteUpdateAsync(set => set
                    .SetProperty(x => x.LastStatus, "Failed").SetProperty(x => x.LastErrorCode, code)
                    .SetProperty(x => x.NextDueAt, next).SetProperty(x => x.RetryAfter, exhausted ? (DateTimeOffset?)null : retry)
                    .SetProperty(x => x.ClaimedAt, (DateTimeOffset?)null).SetProperty(x => x.ClaimToken, (Guid?)null)
                    .SetProperty(x => x.AttemptCount, exhausted ? 0 : report.AttemptCount).SetProperty(x => x.Version, Guid.NewGuid()), ct);
                logger.LogWarning("Scheduled report {ReportId} failed ({ErrorType}).", id, exception.GetType().Name);
            }
        }
    }

    private Task<int> SkipAsync(ScheduledReport report, Guid token, string code, CancellationToken ct)
    {
        var next = ReportScheduleClock.NextDue(report, DateTimeOffset.UtcNow);
        return db.ScheduledReports.Where(x => x.Id == report.Id && x.ClaimToken == token).ExecuteUpdateAsync(set => set
            .SetProperty(x => x.NextDueAt, next).SetProperty(x => x.LastStatus, "Skipped").SetProperty(x => x.LastErrorCode, code)
            .SetProperty(x => x.RetryAfter, (DateTimeOffset?)null).SetProperty(x => x.ClaimedAt, (DateTimeOffset?)null)
            .SetProperty(x => x.ClaimToken, (Guid?)null).SetProperty(x => x.AttemptCount, 0).SetProperty(x => x.Version, Guid.NewGuid()), ct);
    }

    private static MailMessage BuildMessage(MailSettings settings, ScheduledReport report, ScheduledReportDocument document,
        MailAddress recipient, string companyName)
    {
        string T(string key) => ScheduledReportText.Get(key, report.Language);
        static string Html(object? value) => WebUtility.HtmlEncode(Convert.ToString(value, CultureInfo.InvariantCulture) ?? "");
        static string Csv(object? value)
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
            var trimmed = text.TrimStart();
            if (trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@') text = "'" + text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
        var csv = new StringBuilder("\uFEFF").AppendJoin(';', document.Headers.Select(Csv)).Append("\r\n");
        foreach (var row in document.Rows) csv.AppendJoin(';', row.Select(Csv)).Append("\r\n");
        var attachment = Encoding.UTF8.GetBytes(csv.ToString());
        if (attachment.Length > 10 * 1024 * 1024) throw new InvalidOperationException("Scheduled report attachment is too large.");
        var body = new StringBuilder("<!doctype html><html><body style=\"font-family:Arial,sans-serif;color:#172033\">")
            .Append("<h2>").Append(Html(companyName)).Append(" · ").Append(Html(report.Name)).Append("</h2>")
            .Append("<p>").Append(Html(DateTimeOffset.UtcNow.ToString("u"))).Append("</p><table style=\"border-collapse:collapse\"><thead><tr>");
        foreach (var header in document.Headers) body.Append("<th style=\"padding:8px;border:1px solid #ccd5e0;background:#edf2f8\">").Append(Html(header)).Append("</th>");
        body.Append("</tr></thead><tbody>");
        foreach (var row in document.Rows.Take(50))
        {
            body.Append("<tr>");
            foreach (var value in row) body.Append("<td style=\"padding:8px;border:1px solid #ccd5e0\">").Append(Html(value)).Append("</td>");
            body.Append("</tr>");
        }
        body.Append("</tbody></table><p>").Append(Html(T("csvHint"))).Append("</p><p><a href=\"")
            .Append(Html(settings.BaseUri.ToString())).Append("\">EnvanterTR</a></p><p>").Append(Html(T("footer"))).Append("</p></body></html>");
        var message = new MailMessage { From = new MailAddress(settings.From, settings.FromName),
            Subject = $"{T("subject")} · {report.Name}", SubjectEncoding = Encoding.UTF8, BodyEncoding = Encoding.UTF8,
            Body = body.ToString(), IsBodyHtml = true };
        message.To.Add(recipient);
        message.Headers.Add("Message-ID", $"<report-{report.Id:N}-{report.NextDueAt.ToUnixTimeSeconds()}@{new MailAddress(settings.From).Host}>");
        message.Attachments.Add(new Attachment(new MemoryStream(attachment, writable: false), $"report-{report.Id:N}-{report.NextDueAt:yyyyMMdd}.csv", "text/csv"));
        return message;
    }

    private sealed record MailSettings(string Host, int Port, string From, string FromName, Uri BaseUri)
    {
        public static MailSettings? TryCreate(IConfiguration config)
        {
            var host = config["Email:Smtp:Host"];
            var from = config["Email:FromAddress"];
            if (string.IsNullOrWhiteSpace(host) || host.Any(char.IsControl) || !MailAddress.TryCreate(from, out var address) ||
                !string.Equals(address.Address, from, StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(config["Email:Smtp:Port"], out var port) || port is < 1 or > 65535 ||
                !Uri.TryCreate(config["Email:PublicBaseUrl"], UriKind.Absolute, out var uri) ||
                uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo) ||
                !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
                uri.Scheme != "https" && !string.Equals(config["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase)) return null;
            return new(host, port, address.Address, config["Email:FromName"] ?? "EnvanterTR", uri);
        }
    }
}

public sealed class ScheduledReportEmailWorker(IServiceScopeFactory scopeFactory, ILogger<ScheduledReportEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ScheduledReportEmailService>().RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception) { logger.LogWarning("Scheduled report cycle failed ({ErrorType}).", exception.GetType().Name); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
