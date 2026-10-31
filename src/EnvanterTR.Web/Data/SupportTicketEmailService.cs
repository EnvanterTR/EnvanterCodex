using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed class SupportTicketEmailService(IConfiguration configuration, IServiceScopeFactory scopeFactory,
    ILogger<SupportTicketEmailService> logger)
{
    private const int BatchSize = 20;
    private static readonly TimeSpan StaleClaimAge = TimeSpan.FromMinutes(10);

    public void QueueSupportMailbox(AppDbContext db, SupportTicket ticket, SupportTicketMessage? message,
        string eventType, string? statusSnapshot = null)
    {
        var recipient = ValidAddress(configuration["Support:Email"]);
        if (recipient is null)
        {
            logger.LogWarning("Support mailbox is not configured; support ticket email was not queued for company {CompanyId}, ticket {TicketId}.",
                ticket.CompanyId, ticket.Id);
            return;
        }

        db.SupportTicketEmailOutbox.Add(new SupportTicketEmailOutbox
        {
            CompanyId = ticket.CompanyId,
            TicketId = ticket.Id,
            MessageId = message?.Id,
            RecipientEmail = recipient,
            IsSupportMailbox = true,
            EventType = eventType,
            StatusSnapshot = statusSnapshot ?? ticket.Status
        });
    }

    public async Task QueueCustomerAsync(AppDbContext db, SupportTicket ticket, SupportTicketMessage? message,
        string eventType, string? statusSnapshot, CancellationToken cancellationToken)
    {
        var recipient = await db.Users.AsNoTracking()
            .Where(user => user.Id == ticket.CreatedByUserId && user.EmailConfirmed && user.Email != null &&
                db.CompanyMemberships.Any(member => member.CompanyId == ticket.CompanyId && member.UserId == user.Id))
            .Select(user => user.Email)
            .SingleOrDefaultAsync(cancellationToken);
        recipient = ValidAddress(recipient);
        if (recipient is null) return;

        db.SupportTicketEmailOutbox.Add(new SupportTicketEmailOutbox
        {
            CompanyId = ticket.CompanyId,
            TicketId = ticket.Id,
            MessageId = message?.Id,
            RecipientEmail = recipient,
            IsSupportMailbox = false,
            EventType = eventType,
            StatusSnapshot = statusSnapshot ?? ticket.Status
        });
    }

    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        if (!TryGetSmtpConfiguration(out var host, out var port, out var fromAddress, out var fromName)) return 0;

        // Do not race startup migrations: this worker may start before the database is ready.
        await using var discoveryScope = scopeFactory.CreateAsyncScope();
        var discoveryDb = discoveryScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var schemaReady = await IsSchemaReadyAsync(discoveryDb, cancellationToken);
        if (!schemaReady) return 0;

        var now = DateTimeOffset.UtcNow;
        var staleBefore = now - StaleClaimAge;
        var candidates = await discoveryDb.SupportTicketEmailOutbox.AsNoTracking()
            .Where(item => (item.Status == "Pending" && item.NextAttemptAt <= now) ||
                           (item.Status == "Sending" && item.LastAttemptAt < staleBefore))
            .OrderBy(item => item.NextAttemptAt).ThenBy(item => item.CreatedAt)
            .Select(item => item.Id).Take(BatchSize).ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var id in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var claimedAt = DateTimeOffset.UtcNow;
            var claimed = await db.SupportTicketEmailOutbox
                .Where(item => item.Id == id &&
                    ((item.Status == "Pending" && item.NextAttemptAt <= claimedAt) ||
                     (item.Status == "Sending" && item.LastAttemptAt < claimedAt - StaleClaimAge)))
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(item => item.Status, "Sending")
                    .SetProperty(item => item.AttemptCount, item => item.AttemptCount + 1)
                    .SetProperty(item => item.LastAttemptAt, claimedAt)
                    .SetProperty(item => item.SentAt, (DateTimeOffset?)null)
                    .SetProperty(item => item.LastErrorType, (string?)null), cancellationToken);
            if (claimed == 0) continue;

            var queued = await db.SupportTicketEmailOutbox.AsNoTracking().SingleAsync(item => item.Id == id, cancellationToken);
            var ticketData = await (from ticket in db.SupportTickets.AsNoTracking()
                                    join company in db.Companies.AsNoTracking() on ticket.CompanyId equals company.Id
                                    join requester in db.Users.AsNoTracking() on ticket.CreatedByUserId equals requester.Id
                                    where ticket.CompanyId == queued.CompanyId && ticket.Id == queued.TicketId
                                    select new
                                    {
                                        ticket.Subject, company.Name, requester.DisplayName,
                                        company.IsActive, company.DeletedAt, requester.Email, requester.EmailConfirmed,
                                        IsMember = db.CompanyMemberships.Any(member =>
                                            member.CompanyId == queued.CompanyId && member.UserId == requester.Id)
                                    })
                .SingleOrDefaultAsync(cancellationToken);

            // Queueing is not a lasting authorization grant. Recheck the destination
            // before loading message content or transmitting it to the SMTP server.
            if (ticketData is not null && (!ticketData.IsActive || ticketData.DeletedAt is not null ||
                (queued.IsSupportMailbox
                    ? !string.Equals(ValidAddress(configuration["Support:Email"]), ValidAddress(queued.RecipientEmail), StringComparison.OrdinalIgnoreCase)
                    : !ticketData.IsMember || !ticketData.EmailConfirmed ||
                      !string.Equals(ValidAddress(ticketData.Email), ValidAddress(queued.RecipientEmail), StringComparison.OrdinalIgnoreCase))))
            {
                await RetryLaterAsync(db, queued, "SupportEmailRecipientNoLongerAuthorized", claimedAt, cancellationToken);
                continue;
            }
            var messageBody = queued.MessageId is null ? null : await db.SupportTicketMessages.AsNoTracking()
                .Where(message => message.CompanyId == queued.CompanyId && message.TicketId == queued.TicketId && message.Id == queued.MessageId)
                .Select(message => message.Body).SingleOrDefaultAsync(cancellationToken);

            if (ticketData is null || (queued.MessageId is not null && messageBody is null))
            {
                await RetryLaterAsync(db, queued, "SupportEmailDataMissing", claimedAt, cancellationToken);
                continue;
            }

            try
            {
                using var mail = BuildMessage(queued, ticketData.Name, ticketData.Subject, ticketData.DisplayName,
                    messageBody, fromAddress, fromName);
                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = !string.Equals(configuration["Email:Smtp:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 15000
                };
                var username = configuration["Email:Smtp:Username"];
                if (!string.IsNullOrWhiteSpace(username))
                    client.Credentials = new NetworkCredential(username, configuration["Email:Smtp:Password"] ?? string.Empty);
                await client.SendMailAsync(mail, cancellationToken);
                await db.SupportTicketEmailOutbox.Where(item => item.Id == id && item.Status == "Sending")
                    .ExecuteUpdateAsync(updates => updates
                        .SetProperty(item => item.Status, "Sent")
                        .SetProperty(item => item.SentAt, DateTimeOffset.UtcNow)
                        .SetProperty(item => item.LastErrorType, (string?)null), cancellationToken);
                sent++;
                logger.LogInformation("Support email delivered for company {CompanyId}, ticket {TicketId}, event {EventType}.",
                    queued.CompanyId, queued.TicketId, queued.EventType);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await RetryLaterAsync(db, queued, exception.GetType().Name, claimedAt, cancellationToken);
                logger.LogWarning("Support email attempt {AttemptCount} failed for company {CompanyId}, ticket {TicketId} ({ErrorType}).",
                    queued.AttemptCount, queued.CompanyId, queued.TicketId, exception.GetType().Name);
            }
        }

        return sent;
    }

    private static async Task<bool> IsSchemaReadyAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        return await db.Database.SqlQueryRaw<bool>("""
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = current_schema() AND table_name = 'SupportTicketEmailOutbox'
            ) AS "Value"
            """).SingleAsync(cancellationToken);
    }

    private async Task RetryLaterAsync(AppDbContext db, SupportTicketEmailOutbox queued, string errorType,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        var delaySeconds = Math.Min(21600, 30 * Math.Pow(2, Math.Min(queued.AttemptCount, 10)));
        await db.SupportTicketEmailOutbox.Where(item => item.Id == queued.Id && item.Status == "Sending")
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(item => item.Status, "Pending")
                .SetProperty(item => item.NextAttemptAt, now.AddSeconds(delaySeconds))
                .SetProperty(item => item.LastErrorType, errorType), cancellationToken);
    }

    private static MailMessage BuildMessage(SupportTicketEmailOutbox queued, string companyName, string subject,
        string requesterName, string? messageBody, string fromAddress, string fromName)
    {
        var requesterIsSupport = queued.IsSupportMailbox;
        var title = queued.EventType switch
        {
            "TicketCreated" => "Yeni destek talebi",
            "CustomerReply" => "Destek talebine yeni müşteri yanıtı",
            "StaffReply" => "Destek talebinize yanıt verildi",
            "StatusChanged" => "Destek talebinizin durumu güncellendi",
            _ => "Destek talebi güncellemesi"
        };
        var ticketReference = queued.TicketId.ToString("N")[..8].ToUpperInvariant();
        var emailSubject = requesterIsSupport
            ? $"[EnvanterTR destek {ticketReference}] {title}"
            : $"EnvanterTR destek {ticketReference}: {title}";
        var body = new StringBuilder()
            .AppendLine(title)
            .AppendLine($"Talep: {ticketReference}")
            .AppendLine($"Şirket: {companyName}")
            .AppendLine($"Talep sahibi: {requesterName}")
            .AppendLine($"Konu: {subject}")
            .AppendLine($"Durum: {StatusLabel(queued.StatusSnapshot)}");
        if (!string.IsNullOrWhiteSpace(messageBody)) body.AppendLine().AppendLine("Mesaj:").AppendLine(messageBody);
        body.AppendLine().AppendLine("Yanıt vermek veya talebi incelemek için EnvanterTR uygulamasındaki Destek bölümünü açın.");

        return new MailMessage(new MailAddress(fromAddress, fromName), new MailAddress(queued.RecipientEmail))
        {
            Subject = emailSubject,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false,
            Body = body.ToString()
        };
    }

    private bool TryGetSmtpConfiguration(out string host, out int port, out string fromAddress, out string fromName)
    {
        host = configuration["Email:Smtp:Host"] ?? "";
        fromAddress = ValidAddress(configuration["Email:FromAddress"]) ?? "";
        fromName = configuration["Email:FromName"] ?? "EnvanterTR";
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress) ||
            !int.TryParse(configuration["Email:Smtp:Port"], out port) || port is < 1 or > 65535)
        {
            port = 0;
            return false;
        }
        return true;
    }

    private static string? ValidAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !MailAddress.TryCreate(value, out var parsed) ||
            !string.Equals(parsed.Address, value.Trim(), StringComparison.OrdinalIgnoreCase)) return null;
        return parsed.Address;
    }

    private static string StatusLabel(string? status) => status switch
    {
        "Open" => "Açık", "InProgress" => "İşlemde", "AwaitingCustomer" => "Müşteri yanıtı bekleniyor",
        "Resolved" => "Çözüldü", "Closed" => "Kapalı", _ => "Güncellendi"
    };
}

public sealed class SupportTicketEmailWorker(IServiceScopeFactory scopeFactory, ILogger<SupportTicketEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<SupportTicketEmailService>().RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception)
            {
                logger.LogWarning("Support email queue cycle failed ({ErrorType}).", exception.GetType().Name);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
