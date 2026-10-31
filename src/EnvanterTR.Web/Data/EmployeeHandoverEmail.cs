using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static partial class EmployeeHandoverEndpoints
{
    public sealed record SendHandoverEmailRequest(Guid IdempotencyKey, string? ExpectedRecipient,
        string? SnapshotSha256, Guid? ResendOfAttemptId = null);

    private static async Task<IResult> GetEmailDeliveryAsync(Guid employeeId, Guid documentId, ClaimsPrincipal user,
        AppDbContext db, IConfiguration configuration, IWebHostEnvironment environment, CancellationToken cancellationToken)
    {
        if (!TryContext(user, out var companyId, out var userId)) return Results.Forbid();
        var record = await db.EmployeeHandoverDocuments.AsNoTracking().SingleOrDefaultAsync(x =>
            x.CompanyId == companyId && x.EmployeeId == employeeId && x.Id == documentId, cancellationToken);
        if (record is null || !await CanAccessDocumentLocationsAsync(db, companyId, userId, [record], cancellationToken))
            return Results.NotFound();
        var recipient = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == employeeId && x.IsActive)
            .Select(x => x.Email).SingleOrDefaultAsync(cancellationToken);
        var latest = await db.EmployeeHandoverEmailAttempts.AsNoTracking().Where(x => x.CompanyId == companyId &&
                x.DocumentId == documentId).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return Results.Ok(new { recipient = HandoverEmailAddress(recipient), record.SnapshotSha256,
            ready = HandoverEmailAddress(recipient) is not null && HandoverSmtpReady(configuration, environment) && TryReadVerifiedSignedSnapshot(record, out _),
            latestAttempt = latest is null ? null : HandoverEmailResponse(latest) });
    }

    private static async Task<IResult> SendEmailDeliveryAsync(Guid employeeId, Guid documentId,
        SendHandoverEmailRequest request, ClaimsPrincipal user, AppDbContext db, IConfiguration configuration,
        IWebHostEnvironment environment, IHostApplicationLifetime lifetime, ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (!TryContext(user, out var companyId, out var userId)) return Results.Forbid();
        if (request.IdempotencyKey == Guid.Empty || HandoverEmailAddress(request.ExpectedRecipient) is not { } expectedRecipient ||
            request.SnapshotSha256 is not { Length: 64 })
            return Results.BadRequest(new { message = "İstek kimliği, kayıtlı alıcı ve belge özeti gereklidir." });
        EmployeeHandoverEmailAttempt attempt;
        EmployeeHandoverDocument record;
        byte[] pdf;
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            record = (await db.EmployeeHandoverDocuments.FromSqlInterpolated($"""
                SELECT * FROM "EmployeeHandoverDocuments"
                WHERE "CompanyId" = {companyId} AND "EmployeeId" = {employeeId} AND "Id" = {documentId} FOR UPDATE
                """).AsNoTracking().SingleOrDefaultAsync(cancellationToken))!;
            if (record is null || !await CanAccessDocumentLocationsAsync(db, companyId, userId, [record], cancellationToken))
                return Results.NotFound();
            var existing = await db.EmployeeHandoverEmailAttempts.AsNoTracking().SingleOrDefaultAsync(x =>
                x.CompanyId == companyId && x.DocumentId == documentId && x.IdempotencyKey == request.IdempotencyKey, cancellationToken);
            if (existing is not null)
            {
                if (existing.RequestedByUserId != userId || !string.Equals(existing.RecipientEmail, expectedRecipient, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(existing.SnapshotSha256, request.SnapshotSha256, StringComparison.OrdinalIgnoreCase))
                    return Results.Conflict(new { message = "Aynı istek kimliği başka bir gönderim için kullanılamaz." });
                return Results.Ok(HandoverEmailResponse(existing));
            }
            var recipient = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == employeeId && x.IsActive)
                .Select(x => x.Email).SingleOrDefaultAsync(cancellationToken);
            if (HandoverEmailAddress(recipient) is not { } currentRecipient ||
                !string.Equals(currentRecipient, expectedRecipient, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(record.SnapshotSha256, request.SnapshotSha256, StringComparison.OrdinalIgnoreCase))
                return Results.Conflict(new { message = "Alıcı veya belge değişti. Kayıtlı alıcıyı ve belgeyi yeniden kontrol edin." });
            if (!TryReadVerifiedSignedSnapshot(record, out var snapshot))
                return Results.Conflict(new { message = "Gönderim için iki imza ve belge bütünlüğü doğrulanmalıdır." });
            if (!HandoverSmtpReady(configuration, environment))
                return Results.Json(new { message = "Güvenli e-posta gönderimi yapılandırılmamış; ileti gönderilmedi." }, statusCode: 503);
            var latest = await db.EmployeeHandoverEmailAttempts.AsNoTracking().Where(x => x.CompanyId == companyId && x.DocumentId == documentId)
                .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).FirstOrDefaultAsync(cancellationToken);
            if (latest is not null)
            {
                if (latest.Status == "Sending" && latest.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1))
                    return Results.Accepted(value: HandoverEmailResponse(latest));
                if (request.ResendOfAttemptId != latest.Id)
                    return Results.Conflict(new { message = "Önceki gönderimi kontrol edip yeniden gönderimi açıkça onaylayın." });
            }
            var companyName = await db.Companies.AsNoTracking().Where(x => x.Id == companyId).Select(x => x.Name).SingleAsync(cancellationToken);
            pdf = CreateHandoverPdf(record, snapshot!, companyName);
            attempt = new EmployeeHandoverEmailAttempt
            {
                CompanyId = companyId, DocumentId = documentId, EmployeeId = employeeId, RequestedByUserId = userId,
                IdempotencyKey = request.IdempotencyKey, RecipientEmail = currentRecipient,
                SnapshotSha256 = record.SnapshotSha256, PdfSha256 = Convert.ToHexStringLower(SHA256.HashData(pdf))
            };
            db.EmployeeHandoverEmailAttempts.Add(attempt);
            AddHandoverEmailAudit(db, attempt, "HandoverEmailRequested");
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        // A committed user action is finished independently of a disconnected browser.
        // No background retry: an ambiguous SMTP result requires an explicit new resend confirmation.
        using var deliveryTimeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.ApplicationStopping);
        deliveryTimeout.CancelAfter(TimeSpan.FromSeconds(25));
        try
        {
            await SendHandoverPdfMailAsync(attempt, record, pdf, configuration, deliveryTimeout.Token);
            attempt.Status = "Accepted";
            attempt.AcceptedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception exception) when (exception is SmtpException or IOException or OperationCanceledException or InvalidOperationException)
        {
            attempt.Status = "Uncertain";
            attempt.ErrorType = exception.GetType().Name;
            loggerFactory.CreateLogger("HandoverEmail").LogWarning("Handover email attempt {AttemptId} outcome is uncertain: {ErrorType}.",
                attempt.Id, attempt.ErrorType);
        }
        finally { CryptographicOperations.ZeroMemory(pdf); }
        attempt.UpdatedAt = DateTimeOffset.UtcNow;
        AddHandoverEmailAudit(db, attempt, attempt.Status == "Accepted" ? "HandoverEmailAccepted" : "HandoverEmailUncertain");
        using var persistenceTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await db.SaveChangesAsync(persistenceTimeout.Token);
        return Results.Ok(HandoverEmailResponse(attempt));
    }

    private static object HandoverEmailResponse(EmployeeHandoverEmailAttempt attempt)
    {
        var status = attempt.Status == "Sending" && attempt.CreatedAt < DateTimeOffset.UtcNow.AddMinutes(-1) ? "Uncertain" : attempt.Status;
        return new { attempt.Id, status, attempt.CreatedAt, attempt.AcceptedAt,
            message = status switch
            {
                "Accepted" => "İmzalı PDF gönderici SMTP sunucusu tarafından kabul edildi. Alıcıya teslimi ayrıca doğrulanmalıdır.",
                "Sending" => "Gönderim sürüyor. Aynı isteği tekrarlamak ikinci bir ileti oluşturmaz.",
                _ => "Gönderim sonucu doğrulanamadı. Yeniden göndermeden önce iletim günlüğünü ve alıcı posta kutusunu kontrol edin."
            } };
    }

    private static bool HandoverSmtpReady(IConfiguration configuration, IWebHostEnvironment environment) =>
        EmailConfigurationReadiness.Evaluate(configuration, requireHttps: !environment.IsDevelopment()).ConfigurationReady &&
        (environment.IsDevelopment() || !string.Equals(configuration["Email:Smtp:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase));

    private static string? HandoverEmailAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 254 || value.IndexOfAny(['\r', '\n', ',', ';']) >= 0 ||
            !MailAddress.TryCreate(value.Trim(), out var address) || !string.Equals(value.Trim(), address.Address, StringComparison.OrdinalIgnoreCase)) return null;
        return address.Address;
    }

    private static void AddHandoverEmailAudit(AppDbContext db, EmployeeHandoverEmailAttempt attempt, string action) =>
        db.AuditEntries.Add(new AuditEntry
        {
            CompanyId = attempt.CompanyId, ActorUserId = attempt.RequestedByUserId,
            TargetType = "EmployeeHandoverDocument", TargetId = attempt.DocumentId, Action = action,
            Details = JsonSerializer.Serialize(new { attempt.Id, attempt.EmployeeId, attempt.SnapshotSha256, attempt.PdfSha256, attempt.Status })
        });

    private static async Task SendHandoverPdfMailAsync(EmployeeHandoverEmailAttempt attempt, EmployeeHandoverDocument record,
        byte[] pdf, IConfiguration configuration, CancellationToken cancellationToken)
    {
        using var message = new MailMessage(new MailAddress(configuration["Email:FromAddress"]!, configuration["Email:FromName"] ?? "EnvanterTR"),
            new MailAddress(attempt.RecipientEmail))
        {
            Subject = $"EnvanterTR imzalı zimmet belgesi - {record.DocumentNumber}", SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8, IsBodyHtml = false,
            Body = $"Merhaba {record.RecipientName},\n\nİki tarafın imzalarını içeren {record.DocumentNumber} numaralı zimmet belgeniz ektedir.\nBelge içerik SHA-256: {record.SnapshotSha256}\n\nBu bildirim EnvanterTR şirket alanındaki yetkili kullanıcının açık gönderme işlemiyle oluşturulmuştur."
        };
        message.Headers.Add("X-EnvanterTR-Delivery-Id", attempt.Id.ToString("D"));
        var safeNumber = new string(record.DocumentNumber.Where(c => char.IsAsciiLetterOrDigit(c) || c == '-').ToArray());
        using var stream = new MemoryStream(pdf, writable: false);
        message.Attachments.Add(new Attachment(stream, safeNumber + ".pdf", "application/pdf"));
        using var client = new SmtpClient(configuration["Email:Smtp:Host"]!, int.Parse(configuration["Email:Smtp:Port"]!, System.Globalization.CultureInfo.InvariantCulture))
        {
            EnableSsl = !string.Equals(configuration["Email:Smtp:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase),
            DeliveryMethod = SmtpDeliveryMethod.Network, Timeout = 25000, UseDefaultCredentials = false
        };
        if (!string.IsNullOrWhiteSpace(configuration["Email:Smtp:Username"]))
            client.Credentials = new NetworkCredential(configuration["Email:Smtp:Username"], configuration["Email:Smtp:Password"]);
        await client.SendMailAsync(message, cancellationToken);
    }
}
