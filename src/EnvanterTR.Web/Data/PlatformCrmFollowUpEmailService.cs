using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

/// <summary>Sends one privacy-minimized daily reminder to each assigned platform administrator.</summary>
public sealed class PlatformCrmFollowUpEmailService(
    AppDbContext db,
    IConfiguration configuration,
    ILogger<PlatformCrmFollowUpEmailService> logger)
{
    private static readonly TimeSpan StaleClaimAge = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromHours(1);

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        // Intentionally opt-in. Production does not send CRM mail until the operator enables it.
        if (!bool.TryParse(configuration["Platform:CrmFollowUpEmailRemindersEnabled"], out var enabled) || !enabled)
            return;
        if (!TryGetMailConfiguration(out var host, out var port, out var fromAddress, out var fromName, out var baseUri))
            return;

        // The worker may start while migrations are being applied. Avoid querying a model
        // table that does not exist yet on older production schemas.
        var schemaReady = await db.Database.SqlQueryRaw<bool>("""
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = current_schema()
                  AND table_name = 'PlatformFollowUpEmailDigests'
            ) AS "Value"
            """).SingleAsync(cancellationToken);
        if (!schemaReady) return;

        var now = DateTimeOffset.UtcNow;
        var deliveryDate = DateOnly.FromDateTime(now.UtcDateTime);
        var admins = await (from administrator in db.PlatformAdministrators.AsNoTracking()
                            join user in db.Users.AsNoTracking() on administrator.UserId equals user.Id
                            where user.EmailConfirmed && user.Email != null
                            select new { administrator.UserId, user.Email, user.DisplayName })
            .OrderBy(x => x.UserId).ToListAsync(cancellationToken);

        foreach (var admin in admins)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!MailAddress.TryCreate(admin.Email, out var recipient) ||
                !string.Equals(recipient.Address, admin.Email, StringComparison.OrdinalIgnoreCase)) continue;

            var items = new List<FollowUpItem>();
            var companyItems = await db.Companies.AsNoTracking()
                .Where(company => company.IsActive && company.PlatformOwnerUserId == admin.UserId &&
                    company.CustomerLifecycleStage != "Churned" && company.PlatformNextFollowUpAt != null &&
                    company.PlatformNextFollowUpAt <= now)
                .OrderBy(company => company.PlatformNextFollowUpAt).ThenBy(company => company.Name)
                .Take(100)
                .Select(company => new FollowUpItem("Şirket takibi", company.Name, company.PlatformNextFollowUpAt!.Value))
                .ToListAsync(cancellationToken);
            items.AddRange(companyItems);

            var demoItems = await db.PlatformDemoRequests.AsNoTracking()
                .Where(request => request.AssignedAdminUserId == admin.UserId && request.FollowUpAt != null &&
                    request.FollowUpAt <= now && request.Status != "Converted" && request.Status != "Closed")
                .OrderBy(request => request.FollowUpAt).ThenBy(request => request.CompanyName)
                .Take(100)
                // Do not include lead contact details or free-text messages in reminder email.
                .Select(request => new FollowUpItem("Demo talebi", request.CompanyName, request.FollowUpAt!.Value))
                .ToListAsync(cancellationToken);
            items.AddRange(demoItems);
            if (items.Count == 0) continue;
            items = items.OrderBy(item => item.DueAt).ThenBy(item => item.Title, StringComparer.Ordinal)
                .Take(200).ToList();

            var digest = await TryClaimDigestAsync(admin.UserId, deliveryDate, now, cancellationToken);
            if (digest is null) continue;

            try
            {
                using var message = BuildMessage(recipient, fromAddress, fromName, baseUri, items);
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
                logger.LogInformation("Platform CRM follow-up digest delivered to administrator {UserId} with {ItemCount} items.",
                    admin.UserId, items.Count);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                digest.Status = "Failed";
                digest.LastError = exception.GetType().Name;
                digest.ConcurrencyToken = Guid.NewGuid();
                try { await db.SaveChangesAsync(cancellationToken); }
                catch (DbUpdateException) { db.Entry(digest).State = EntityState.Detached; }
                logger.LogWarning("Platform CRM follow-up digest failed for administrator {UserId} ({ErrorType}).",
                    admin.UserId, exception.GetType().Name);
            }
        }
    }

    private async Task<PlatformFollowUpEmailDigest?> TryClaimDigestAsync(Guid userId, DateOnly deliveryDate,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        var digest = await db.PlatformFollowUpEmailDigests.SingleOrDefaultAsync(
            row => row.UserId == userId && row.DeliveryDate == deliveryDate, cancellationToken);
        if (digest is null)
        {
            digest = new PlatformFollowUpEmailDigest
            {
                UserId = userId, DeliveryDate = deliveryDate, Status = "Sending", StartedAt = now, AttemptCount = 1
            };
            db.PlatformFollowUpEmailDigests.Add(digest);
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

    private static MailMessage BuildMessage(MailAddress recipient, string fromAddress, string fromName,
        Uri baseUri, IReadOnlyCollection<FollowUpItem> items)
    {
        var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = $"EnvanterTR · {items.Count} müşteri takibi bekliyor",
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true
        };
        message.To.Add(recipient);
        var rows = string.Join("", items.Select(item =>
            $"<li><strong>{WebUtility.HtmlEncode(item.Kind)}</strong>: {WebUtility.HtmlEncode(item.Title)} " +
            $"· {WebUtility.HtmlEncode(item.DueAt.ToString("dd.MM.yyyy HH:mm 'UTC'"))}</li>"));
        var link = new Uri(baseUri, "/platform-admin.html").AbsoluteUri;
        message.Body = $"<p>Atanmış, zamanı gelmiş CRM takipleri:</p><ul>{rows}</ul>" +
            $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Platform yönetim panelini aç</a></p>" +
            "<p>Bu ileti yalnızca platform yöneticisine gönderilmiştir; demo talep sahibinin iletişim bilgileri içermez.</p>";
        return message;
    }

    private bool TryGetMailConfiguration(out string host, out int port, out string fromAddress,
        out string fromName, out Uri baseUri)
    {
        host = configuration["Email:Smtp:Host"] ?? "";
        fromAddress = configuration["Email:FromAddress"] ?? "";
        fromName = configuration["Email:FromName"] ?? "EnvanterTR";
        Uri? parsedBaseUri = null;
        if (string.IsNullOrWhiteSpace(host) || !MailAddress.TryCreate(fromAddress, out _) ||
            !Uri.TryCreate(configuration["Email:PublicBaseUrl"], UriKind.Absolute, out parsedBaseUri) || parsedBaseUri is null ||
            parsedBaseUri.Scheme is not ("https" or "http") ||
            (parsedBaseUri.Scheme != Uri.UriSchemeHttps &&
                !string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase)) ||
            !int.TryParse(configuration["Email:Smtp:Port"], out port) || port is < 1 or > 65535)
        {
            port = 0;
            baseUri = null!;
            return false;
        }
        baseUri = parsedBaseUri;
        return true;
    }

    private sealed record FollowUpItem(string Kind, string Title, DateTimeOffset DueAt);
}

public sealed class PlatformCrmFollowUpEmailWorker(IServiceScopeFactory scopeFactory,
    ILogger<PlatformCrmFollowUpEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<PlatformCrmFollowUpEmailService>().RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception)
            {
                logger.LogError("Platform CRM follow-up email cycle failed ({ErrorType}).", exception.GetType().Name);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
