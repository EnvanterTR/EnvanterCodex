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
    private static readonly string[] AllowedKinds = ["maintenance", "audit", "license", "warranty"];
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

            var items = await GetUpcomingItemsAsync(preference.CompanyId, kinds, from, to, cancellationToken);
            if (items.Count == 0) continue;

            var digest = await TryClaimDigestAsync(preference.CompanyId, preference.UserId, today, now, cancellationToken);
            if (digest is null) continue;

            try
            {
                using var message = BuildMessage(parsedAddress, fromAddress, fromName, baseUri, items);
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

    private async Task<List<OperationalReminderItem>> GetUpcomingItemsAsync(Guid companyId, string[] kinds,
        DateOnly rangeStart, DateOnly rangeEnd, CancellationToken cancellationToken)
    {
        var items = new List<OperationalReminderItem>();
        if (kinds.Contains("maintenance", StringComparer.Ordinal))
        {
            var rows = await (from maintenance in db.AssetMaintenances.AsNoTracking()
                              join asset in db.Assets.AsNoTracking() on new { maintenance.CompanyId, maintenance.AssetId } equals new { asset.CompanyId, AssetId = asset.Id }
                              where maintenance.CompanyId == companyId && maintenance.Status == "Scheduled" &&
                                    maintenance.PlannedOn >= rangeStart && maintenance.PlannedOn <= rangeEnd
                              orderby maintenance.PlannedOn, asset.AssetTag
                              select new OperationalReminderItem("maintenance", maintenance.PlannedOn!.Value,
                                  asset.AssetTag + " · " + asset.Name, maintenance.MaintenanceType + (asset.Location == null ? "" : " · " + asset.Location)))
                .Take(200).ToListAsync(cancellationToken);
            items.AddRange(rows);
        }
        if (kinds.Contains("audit", StringComparer.Ordinal))
        {
            var rows = await db.Assets.AsNoTracking()
                .Where(asset => asset.CompanyId == companyId && asset.Status != AssetStatuses.Archived && asset.DeletedAt == null &&
                    asset.NextAuditOn >= rangeStart && asset.NextAuditOn <= rangeEnd)
                .OrderBy(asset => asset.NextAuditOn).ThenBy(asset => asset.AssetTag)
                .Select(asset => new OperationalReminderItem("audit", asset.NextAuditOn!.Value,
                    asset.AssetTag + " · " + asset.Name, asset.Location))
                .Take(200).ToListAsync(cancellationToken);
            items.AddRange(rows);
        }
        if (kinds.Contains("license", StringComparer.Ordinal))
        {
            var rows = await db.SoftwareLicenses.AsNoTracking()
                .Where(license => license.CompanyId == companyId && license.ExpirationDate >= rangeStart && license.ExpirationDate <= rangeEnd)
                .OrderBy(license => license.ExpirationDate).ThenBy(license => license.Name)
                .Select(license => new OperationalReminderItem("license", license.ExpirationDate!.Value,
                    license.Name, license.Publisher))
                .Take(200).ToListAsync(cancellationToken);
            items.AddRange(rows);
        }
        if (kinds.Contains("warranty", StringComparer.Ordinal))
        {
            var rows = await db.Assets.AsNoTracking()
                .Where(asset => asset.CompanyId == companyId && asset.DeletedAt == null &&
                    asset.WarrantyExpiresAt >= rangeStart && asset.WarrantyExpiresAt <= rangeEnd)
                .OrderBy(asset => asset.WarrantyExpiresAt).ThenBy(asset => asset.AssetTag)
                .Select(asset => new OperationalReminderItem("warranty", asset.WarrantyExpiresAt!.Value,
                    asset.AssetTag + " · " + asset.Name, asset.Location))
                .Take(200).ToListAsync(cancellationToken);
            items.AddRange(rows);
        }
        return items.OrderBy(x => x.Date).ThenBy(x => x.Kind, StringComparer.Ordinal).Take(500).ToList();
    }

    private MailMessage BuildMessage(MailAddress recipient, string fromAddress, string fromName,
        Uri publicBaseUri, IReadOnlyList<OperationalReminderItem> items)
    {
        var rows = new StringBuilder();
        foreach (var item in items)
        {
            rows.Append("<tr><td style=\"padding:8px;border-bottom:1px solid #e7edf5\">")
                .Append(WebUtility.HtmlEncode(KindLabel(item.Kind))).Append("</td><td style=\"padding:8px;border-bottom:1px solid #e7edf5\">")
                .Append(WebUtility.HtmlEncode(item.Date.ToString("yyyy-MM-dd"))).Append("</td><td style=\"padding:8px;border-bottom:1px solid #e7edf5\">")
                .Append(WebUtility.HtmlEncode(item.Title)).Append("</td><td style=\"padding:8px;border-bottom:1px solid #e7edf5\">")
                .Append(WebUtility.HtmlEncode(item.Detail ?? "")).Append("</td></tr>");
        }
        var link = WebUtility.HtmlEncode(new Uri(publicBaseUri, "/").AbsoluteUri);
        var body = $"<div style=\"font-family:Arial,sans-serif;color:#25334a;max-width:760px;margin:auto\"><h2>Yaklaşan EnvanterTR işleri</h2><p>Son 7 gün ve gelecek 30 gün içindeki seçtiğiniz bakım, denetim, lisans ve garanti kayıtları:</p><table style=\"width:100%;border-collapse:collapse;text-align:left\"><thead><tr><th style=\"padding:8px\">Tür</th><th style=\"padding:8px\">Tarih</th><th style=\"padding:8px\">Kayıt</th><th style=\"padding:8px\">Açıklama</th></tr></thead><tbody>{rows}</tbody></table><p><a href=\"{link}\">EnvanterTR uygulamasını aç</a></p><p style=\"color:#738197;font-size:12px\">Bu özeti bildirim tercihlerinde e-posta hatırlatmalarını seçtiğiniz için aldınız.</p></div>";
        var message = new MailMessage(new MailAddress(fromAddress, fromName), recipient)
        {
            Subject = "EnvanterTR · yaklaşan işler özeti",
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

    private static string KindLabel(string kind) => kind switch
    {
        "maintenance" => "Bakım planı",
        "audit" => "Fiziksel denetim",
        "license" => "Lisans bitişi",
        "warranty" => "Garanti bitişi",
        _ => "İş"
    };

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
