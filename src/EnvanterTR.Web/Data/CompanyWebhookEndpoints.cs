using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

/// <summary>
/// Şirket bildirim webhook'ları (Slack, Microsoft Teams, imzalı genel HTTPS).
/// Yönetim uçları yalnız <c>settings.manage</c> yetkisiyle ve CSRF korumasıyla çalışır; hedef adres ve imza sırrı
/// veri koruma anahtarıyla şifreli saklanır ve hiçbir yanıtta geri dönmez.
/// </summary>
public static class CompanyWebhookEndpoints
{
    public const string HttpClientName = "company-webhooks";
    public const int MaxWebhooksPerCompany = 10;

    /// <summary>Gönderilebilen olay türleri ve kaynakları (<see cref="AssetEvent.EventType"/>).</summary>
    public static readonly IReadOnlyDictionary<string, string> EventSources = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["asset.assigned"] = "Assigned",
        ["asset.returned"] = "Returned",
        ["asset.transferred"] = "Transferred",
        ["asset.created"] = "Created",
        ["asset.trashed"] = "Trashed",
        ["maintenance.started"] = "MaintenanceStarted",
        ["maintenance.completed"] = "MaintenanceCompleted",
        ["audit.unacceptable"] = "AuditUnacceptable"
    };

    public static void MapCompanyWebhookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/integrations/webhooks").RequireAuthorization("settings.manage");

        group.MapGet("", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            var items = await db.CompanyWebhooks.AsNoTracking().Where(x => x.CompanyId == companyId)
                .OrderBy(x => x.CreatedAt).ToListAsync(ct);
            return Results.Ok(new { items = items.Select(ToResponse), events = EventSources.Keys, limit = MaxWebhooksPerCompany });
        });

        group.MapPost("", async (WebhookRequest request, ClaimsPrincipal user, HttpContext context, AppDbContext db,
            IAntiforgery antiforgery, CompanyWebhookProtection protection, CancellationToken ct) =>
        {
            if (!TryScope(user, out var companyId, out var userId)) return Results.Forbid();
            if (!await ValidateCsrfAsync(context, antiforgery)) return Results.StatusCode(StatusCodes.Status400BadRequest);
            var name = (request.Name ?? string.Empty).Trim();
            if (name.Length is < 2 or > 80) return Validation("name", "Ad 2 ile 80 karakter arasında olmalıdır.");
            var kind = (request.Kind ?? string.Empty).Trim().ToLowerInvariant();
            if (kind is not ("slack" or "teams" or "generic")) return Validation("kind", "Tür Slack, Teams veya genel webhook olmalıdır.");
            if (!CompanyWebhookUrlPolicy.TryValidate(kind, request.Url, out var uri, out var urlError)) return Validation("url", urlError);
            if (!TryEvents(request.Events, out var events)) return Validation("events", "En az bir geçerli olay seçin.");
            if (await db.CompanyWebhooks.CountAsync(x => x.CompanyId == companyId, ct) >= MaxWebhooksPerCompany)
                return Results.Problem($"Şirket başına en fazla {MaxWebhooksPerCompany} webhook tanımlanabilir.", statusCode: StatusCodes.Status409Conflict);

            var webhook = new CompanyWebhook
            {
                CompanyId = companyId, Name = name, Kind = kind, ProtectedUrl = "pending", UrlHint = CompanyWebhookUrlPolicy.Hint(uri!),
                EventsJson = JsonSerializer.Serialize(events), CreatedByUserId = userId, IsActive = request.IsActive ?? true
            };
            webhook.ProtectedUrl = protection.Protect(companyId, webhook.Id, uri!.AbsoluteUri);
            string? signingSecret = null;
            if (kind == "generic")
            {
                signingSecret = "whsec_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
                webhook.ProtectedSigningSecret = protection.Protect(companyId, webhook.Id, signingSecret);
            }
            db.CompanyWebhooks.Add(webhook);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId, TargetType = "CompanyWebhook",
                TargetId = webhook.Id, Action = "webhook.created", Details = $"{name}; {kind}; {webhook.UrlHint}" });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/integrations/webhooks/{webhook.Id}", new
            {
                webhook = ToResponse(webhook),
                signingSecret,
                signingSecretNotice = signingSecret is null ? null : "İmza sırrı yalnızca bu yanıtta gösterilir. Alıcı tarafta X-EnvanterTR-Signature başlığını doğrulamak için saklayın."
            });
        });

        group.MapPut("/{id:guid}", async (Guid id, WebhookRequest request, ClaimsPrincipal user, HttpContext context, AppDbContext db,
            IAntiforgery antiforgery, CompanyWebhookProtection protection, CancellationToken ct) =>
        {
            if (!TryScope(user, out var companyId, out var userId)) return Results.Forbid();
            if (!await ValidateCsrfAsync(context, antiforgery)) return Results.StatusCode(StatusCodes.Status400BadRequest);
            var webhook = await db.CompanyWebhooks.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);
            if (webhook is null) return Results.NotFound();
            if (request.Name is not null)
            {
                var name = request.Name.Trim();
                if (name.Length is < 2 or > 80) return Validation("name", "Ad 2 ile 80 karakter arasında olmalıdır.");
                webhook.Name = name;
            }
            if (request.Kind is not null && !string.Equals(request.Kind.Trim(), webhook.Kind, StringComparison.OrdinalIgnoreCase))
                return Validation("kind", "Webhook türü sonradan değiştirilemez; yeni bir webhook oluşturun.");
            if (!string.IsNullOrWhiteSpace(request.Url))
            {
                if (!CompanyWebhookUrlPolicy.TryValidate(webhook.Kind, request.Url, out var uri, out var urlError)) return Validation("url", urlError);
                webhook.ProtectedUrl = protection.Protect(companyId, webhook.Id, uri!.AbsoluteUri);
                webhook.UrlHint = CompanyWebhookUrlPolicy.Hint(uri);
            }
            if (request.Events is not null)
            {
                if (!TryEvents(request.Events, out var events)) return Validation("events", "En az bir geçerli olay seçin.");
                webhook.EventsJson = JsonSerializer.Serialize(events);
            }
            if (request.IsActive is bool active)
            {
                // Yeniden etkinleştirmede birikmiş eski olaylar gönderilmez; imleç şimdiye alınır.
                if (active && !webhook.IsActive) { webhook.CursorOccurredAt = DateTimeOffset.UtcNow; webhook.CursorEventId = Guid.Empty; webhook.ConsecutiveFailures = 0; }
                webhook.IsActive = active;
            }
            webhook.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId, TargetType = "CompanyWebhook",
                TargetId = webhook.Id, Action = "webhook.updated", Details = $"{webhook.Name}; etkin: {(webhook.IsActive ? "evet" : "hayır")}" });
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(webhook));
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, HttpContext context, AppDbContext db,
            IAntiforgery antiforgery, CancellationToken ct) =>
        {
            if (!TryScope(user, out var companyId, out var userId)) return Results.Forbid();
            if (!await ValidateCsrfAsync(context, antiforgery)) return Results.StatusCode(StatusCodes.Status400BadRequest);
            var webhook = await db.CompanyWebhooks.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);
            if (webhook is null) return Results.NotFound();
            db.CompanyWebhooks.Remove(webhook);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId, TargetType = "CompanyWebhook",
                TargetId = webhook.Id, Action = "webhook.deleted", Details = webhook.Name });
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/test", async (Guid id, ClaimsPrincipal user, HttpContext context, AppDbContext db,
            IAntiforgery antiforgery, CompanyWebhookDeliveryService delivery, CancellationToken ct) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            if (!await ValidateCsrfAsync(context, antiforgery)) return Results.StatusCode(StatusCodes.Status400BadRequest);
            var webhook = await db.CompanyWebhooks.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);
            if (webhook is null) return Results.NotFound();
            var result = await delivery.SendTestAsync(webhook, ct);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { delivered = result.Success, statusCode = result.StatusCode, error = result.Error });
        }).RequireRateLimiting("webhook-test");
    }

    private static object ToResponse(CompanyWebhook x) => new
    {
        x.Id, x.Name, x.Kind, x.UrlHint, Events = ParseEvents(x.EventsJson), x.IsActive, x.CreatedAt, x.UpdatedAt,
        x.LastDeliveryAt, x.LastStatusCode, x.LastError, x.ConsecutiveFailures, HasSigningSecret = x.ProtectedSigningSecret is not null
    };

    internal static string[] ParseEvents(string? json)
    {
        try { return (JsonSerializer.Deserialize<string[]>(json ?? "[]") ?? []).Where(EventSources.ContainsKey).ToArray(); }
        catch (JsonException) { return []; }
    }

    private static bool TryEvents(string[]? requested, out string[] events)
    {
        events = (requested ?? []).Select(x => x?.Trim() ?? string.Empty).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        return events.Length > 0 && events.All(EventSources.ContainsKey);
    }

    private static bool TryScope(ClaimsPrincipal user, out Guid companyId, out Guid userId)
    {
        userId = Guid.Empty;
        return Guid.TryParse(user.FindFirstValue("company_id"), out companyId) &&
            Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }

    private static async Task<bool> ValidateCsrfAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try { await antiforgery.ValidateRequestAsync(context); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }

    private static IResult Validation(string key, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [key] = [message] });

    public sealed record WebhookRequest(string? Name, string? Kind, string? Url, string[]? Events, bool? IsActive);
}

/// <summary>Webhook hedef adresi kuralları: yalnız HTTPS, kimlik bilgisi içermeyen, türüne uygun ana makine.</summary>
public static class CompanyWebhookUrlPolicy
{
    public const string PrivateTargetMessage = "Webhook hedefi genel internete açık bir adrese çözülmedi.";
    private static readonly string[] TeamsHostSuffixes = [".webhook.office.com", ".logic.azure.com", ".powerplatform.com", ".api.powerplatform.com"];

    public static bool TryValidate(string kind, string? value, out Uri? uri, out string error)
    {
        uri = null; error = string.Empty;
        var text = (value ?? string.Empty).Trim();
        if (text.Length is < 12 or > 2000 || !Uri.TryCreate(text, UriKind.Absolute, out var parsed))
        { error = "Geçerli bir webhook adresi girin."; return false; }
        if (parsed.Scheme != Uri.UriSchemeHttps) { error = "Webhook adresi https:// ile başlamalıdır."; return false; }
        if (!string.IsNullOrEmpty(parsed.UserInfo)) { error = "Webhook adresi kullanıcı adı veya parola içeremez."; return false; }
        if (!parsed.IsDefaultPort && parsed.Port != 443) { error = "Webhook adresi yalnız 443 numaralı bağlantı noktasını kullanabilir."; return false; }
        var host = parsed.IdnHost.ToLowerInvariant();
        if (parsed.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6) { error = "IP adresi yerine alan adı kullanın."; return false; }
        if (host is "localhost" || host.EndsWith(".localhost", StringComparison.Ordinal) || host.EndsWith(".local", StringComparison.Ordinal) ||
            host.EndsWith(".internal", StringComparison.Ordinal) || !host.Contains('.'))
        { error = "İç ağ adresleri webhook hedefi olamaz."; return false; }
        if (kind == "slack" && host != "hooks.slack.com") { error = "Slack webhook adresi https://hooks.slack.com/ ile başlamalıdır."; return false; }
        if (kind == "teams" && !TeamsHostSuffixes.Any(suffix => host.EndsWith(suffix, StringComparison.Ordinal)))
        { error = "Teams webhook adresi Microsoft Teams iş akışı (Workflows) veya gelen webhook adresi olmalıdır."; return false; }
        uri = parsed; return true;
    }

    public static string Hint(Uri uri)
    {
        var tail = uri.AbsolutePath.Length > 4 ? uri.AbsolutePath[^4..] : string.Empty;
        return $"{uri.Host}/…{tail}";
    }

    /// <summary>Yalnız genel internete açık adreslere bağlanılır (SSRF koruması; DNS yeniden bağlama dahil her bağlantıda denetlenir).</summary>
    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.IPv6None)) return false;
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return !(b[0] == 10 || b[0] == 127 || b[0] == 0 || (b[0] == 169 && b[1] == 254) || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) ||
                (b[0] == 192 && b[1] == 168) || (b[0] == 100 && b[1] >= 64 && b[1] <= 127) || (b[0] == 192 && b[1] == 0 && b[2] == 0) ||
                (b[0] == 198 && (b[1] == 18 || b[1] == 19)) || b[0] >= 224);
        }
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast) return false;
            var b = address.GetAddressBytes();
            if ((b[0] & 0xfe) == 0xfc) return false; // fc00::/7 benzersiz yerel
            return true;
        }
        return false;
    }

    public static SocketsHttpHandler CreateHandler(bool allowPrivateTargets) => new()
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        // Vekil sunucu kullanılmaz: aksi halde bağlantı vekile gider ve aşağıdaki IP denetimi atlanmış olur.
        UseProxy = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        ConnectTimeout = TimeSpan.FromSeconds(5),
        ConnectCallback = async (context, cancellationToken) =>
        {
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
            var allowed = addresses.Where(address => allowPrivateTargets || IsPublic(address)).ToArray();
            if (allowed.Length == 0) throw new HttpRequestException(PrivateTargetMessage);
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try { await socket.ConnectAsync(allowed, context.DnsEndPoint.Port, cancellationToken); return new NetworkStream(socket, ownsSocket: true); }
            catch { socket.Dispose(); throw; }
        }
    };
}

public sealed class CompanyWebhookProtection(IDataProtectionProvider provider)
{
    private const string Prefix = "enc:v1:";
    public string Protect(Guid companyId, Guid webhookId, string value) => Prefix + Protector(companyId, webhookId).Protect(value);
    public string Unprotect(Guid companyId, Guid webhookId, string protectedValue)
    {
        if (!protectedValue.StartsWith(Prefix, StringComparison.Ordinal)) throw new CryptographicException("Webhook değeri beklenen biçimde değil.");
        return Protector(companyId, webhookId).Unprotect(protectedValue[Prefix.Length..]);
    }
    private IDataProtector Protector(Guid companyId, Guid webhookId) =>
        provider.CreateProtector("EnvanterTR.CompanyWebhook.v1", companyId.ToString("N"), webhookId.ToString("N"));
}

public sealed record WebhookDeliveryResult(bool Success, int? StatusCode, string? Error);

public sealed class CompanyWebhookDeliveryService(AppDbContext db, IHttpClientFactory httpClientFactory, CompanyWebhookProtection protection,
    ILogger<CompanyWebhookDeliveryService> logger)
{
    private const int BatchSize = 20;
    private const int DisableAfterFailures = 20;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task RunOnceAsync(CancellationToken ct)
    {
        var tableReady = await db.Database.SqlQueryRaw<bool>("""
            SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = 'CompanyWebhooks') AS "Value"
            """).SingleAsync(ct);
        if (!tableReady) return;

        var webhooks = await db.CompanyWebhooks
            .Where(x => x.IsActive && db.Companies.Any(company => company.Id == x.CompanyId && company.IsActive))
            .OrderBy(x => x.CompanyId).ThenBy(x => x.CreatedAt).Take(200).ToListAsync(ct);
        foreach (var webhook in webhooks)
        {
            ct.ThrowIfCancellationRequested();
            // Art arda hatada geri çekil: 1, 2, 4 … en fazla 30 döngü atla.
            if (webhook.ConsecutiveFailures > 0 && webhook.LastDeliveryAt is DateTimeOffset last &&
                DateTimeOffset.UtcNow - last < TimeSpan.FromSeconds(15 * Math.Min(30, 1 << Math.Min(5, webhook.ConsecutiveFailures))))
                continue;
            var sourceTypes = CompanyWebhookEndpoints.ParseEvents(webhook.EventsJson)
                .Select(key => CompanyWebhookEndpoints.EventSources[key]).ToArray();
            if (sourceTypes.Length == 0) continue;
            var cursorAt = webhook.CursorOccurredAt; var cursorId = webhook.CursorEventId;
            var events = await db.AssetEvents.AsNoTracking()
                .Where(x => x.CompanyId == webhook.CompanyId && sourceTypes.Contains(x.EventType) &&
                    (x.OccurredAt > cursorAt || (x.OccurredAt == cursorAt && x.Id.CompareTo(cursorId) > 0)))
                .OrderBy(x => x.OccurredAt).ThenBy(x => x.Id).Take(BatchSize)
                .Select(x => new WebhookEventRow(x.Id, x.EventType, x.OccurredAt, x.Details, x.AssetId,
                    db.Assets.Where(asset => asset.Id == x.AssetId && asset.CompanyId == x.CompanyId).Select(asset => asset.AssetTag).FirstOrDefault(),
                    db.Assets.Where(asset => asset.Id == x.AssetId && asset.CompanyId == x.CompanyId).Select(asset => asset.Name).FirstOrDefault(),
                    db.Employees.Where(employee => employee.Id == x.EmployeeId && employee.CompanyId == x.CompanyId).Select(employee => employee.DisplayName).FirstOrDefault(),
                    db.Users.Where(actor => actor.Id == x.ActorUserId).Select(actor => actor.DisplayName).FirstOrDefault()))
                .ToListAsync(ct);
            if (events.Count == 0) continue;
            var companyName = await db.Companies.AsNoTracking().Where(x => x.Id == webhook.CompanyId).Select(x => x.Name).SingleAsync(ct);
            foreach (var item in events)
            {
                var result = await SendAsync(webhook, BuildMessage(companyName, item), item.Id.ToString("N"), ct);
                if (!result.Success) break;
                webhook.CursorOccurredAt = item.OccurredAt; webhook.CursorEventId = item.Id;
            }
            if (webhook.ConsecutiveFailures >= DisableAfterFailures)
            {
                webhook.IsActive = false;
                webhook.LastError = "Art arda başarısız teslim nedeniyle duraklatıldı; adresi kontrol edip yeniden etkinleştirin.";
            }
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<WebhookDeliveryResult> SendTestAsync(CompanyWebhook webhook, CancellationToken ct)
    {
        var companyName = await db.Companies.AsNoTracking().Where(x => x.Id == webhook.CompanyId).Select(x => x.Name).SingleAsync(ct);
        var message = new WebhookMessage("test", DateTimeOffset.UtcNow, companyName,
            $"EnvanterTR test bildirimi: {companyName} şirketinin \"{webhook.Name}\" webhook'u çalışıyor.", null, null, null, null, null, null);
        return await SendAsync(webhook, message, "test-" + Guid.NewGuid().ToString("N"), ct);
    }

    private async Task<WebhookDeliveryResult> SendAsync(CompanyWebhook webhook, WebhookMessage message, string deliveryId, CancellationToken ct)
    {
        WebhookDeliveryResult result;
        try
        {
            var url = protection.Unprotect(webhook.CompanyId, webhook.Id, webhook.ProtectedUrl);
            if (!CompanyWebhookUrlPolicy.TryValidate(webhook.Kind, url, out var uri, out var error))
                result = new(false, null, error);
            else
            {
                var body = webhook.Kind switch
                {
                    "slack" => JsonSerializer.Serialize(new { text = message.Text }, Json),
                    "teams" => JsonSerializer.Serialize(new
                    {
                        type = "message",
                        attachments = new[]
                        {
                            new
                            {
                                contentType = "application/vnd.microsoft.card.adaptive",
                                content = new Dictionary<string, object>
                                {
                                    ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json", ["type"] = "AdaptiveCard", ["version"] = "1.4",
                                    ["body"] = new object[] { new { type = "TextBlock", text = message.Text, wrap = true } }
                                }
                            }
                        }
                    }, Json),
                    _ => JsonSerializer.Serialize(new
                    {
                        id = deliveryId, @event = message.Event, occurredAt = message.OccurredAt, company = message.Company, text = message.Text,
                        asset = message.AssetId is null ? null : new { id = message.AssetId, tag = message.AssetTag, name = message.AssetName },
                        employee = message.Employee, actor = message.Actor, details = message.Details
                    }, Json)
                };
                using var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
                request.Headers.UserAgent.ParseAdd("EnvanterTR-Webhooks/1.0");
                if (webhook.Kind == "generic" && webhook.ProtectedSigningSecret is not null)
                {
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
                    var secret = protection.Unprotect(webhook.CompanyId, webhook.Id, webhook.ProtectedSigningSecret);
                    var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{body}"))).ToLowerInvariant();
                    request.Headers.Add("X-EnvanterTR-Event", message.Event);
                    request.Headers.Add("X-EnvanterTR-Delivery", deliveryId);
                    request.Headers.Add("X-EnvanterTR-Timestamp", timestamp);
                    request.Headers.Add("X-EnvanterTR-Signature", "sha256=" + signature);
                }
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(10));
                using var response = await httpClientFactory.CreateClient(CompanyWebhookEndpoints.HttpClientName)
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                var status = (int)response.StatusCode;
                result = response.IsSuccessStatusCode ? new(true, status, null) : new(false, status, $"Hedef {status} yanıtı verdi.");
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { result = new(false, null, "Hedef 10 saniye içinde yanıt vermedi."); }
        catch (HttpRequestException exception)
        {
            // Ham istisna metni (iç adresler, vekil bilgisi) kullanıcıya gösterilmez.
            var reason = exception.Message.Contains(CompanyWebhookUrlPolicy.PrivateTargetMessage, StringComparison.Ordinal)
                ? CompanyWebhookUrlPolicy.PrivateTargetMessage
                : exception.InnerException is System.Net.Sockets.SocketException { SocketErrorCode: System.Net.Sockets.SocketError.HostNotFound }
                    ? "Webhook adresinin alan adı bulunamadı."
                    : "Webhook hedefine bağlanılamadı.";
            result = new(false, null, reason);
        }
        catch (CryptographicException) { result = new(false, null, "Webhook adresi çözülemedi; adresi yeniden kaydedin."); }

        webhook.LastDeliveryAt = DateTimeOffset.UtcNow;
        webhook.LastStatusCode = result.StatusCode;
        webhook.LastError = result.Error;
        webhook.ConsecutiveFailures = result.Success ? 0 : webhook.ConsecutiveFailures + 1;
        if (!result.Success) logger.LogInformation("Webhook delivery failed for {WebhookId} ({Status}).", webhook.Id, result.StatusCode);
        return result;
    }

    private static string Truncate(string value) => value.Length <= 280 ? value : value[..280];

    internal static WebhookMessage BuildMessage(string company, WebhookEventRow item)
    {
        var key = CompanyWebhookEndpoints.EventSources.First(pair => pair.Value == item.EventType).Key;
        var asset = item.AssetTag is null ? "Varlık" : $"{item.AssetTag} · {item.AssetName}";
        var who = item.Employee is null ? string.Empty : $" → {item.Employee}";
        var by = item.Actor is null ? string.Empty : $" (işlemi yapan: {item.Actor})";
        var text = key switch
        {
            "asset.assigned" => $"📦 {company}: {asset} zimmetlendi{who}{by}",
            "asset.returned" => $"↩️ {company}: {asset} iade alındı{by}",
            "asset.transferred" => $"🔁 {company}: {asset} transfer edildi{who}{by}",
            "asset.created" => $"🆕 {company}: {asset} envantere eklendi{by}",
            "asset.trashed" => $"🗑️ {company}: {asset} çöp kutusuna taşındı{by}",
            "maintenance.started" => $"🛠️ {company}: {asset} bakıma alındı{by}",
            "maintenance.completed" => $"✅ {company}: {asset} bakımı tamamlandı{by}",
            "audit.unacceptable" => $"⚠️ {company}: {asset} fiziksel denetimde uygunsuz bulundu{by}",
            _ => $"{company}: {asset} · {item.EventType}"
        };
        return new WebhookMessage(key, item.OccurredAt, company, text, item.AssetId, item.AssetTag, item.AssetName, item.Employee, item.Actor,
            item.Details is null ? null : Truncate(item.Details));
    }
}

public sealed record WebhookEventRow(Guid Id, string EventType, DateTimeOffset OccurredAt, string? Details, Guid AssetId,
    string? AssetTag, string? AssetName, string? Employee, string? Actor);

public sealed record WebhookMessage(string Event, DateTimeOffset OccurredAt, string Company, string Text, Guid? AssetId,
    string? AssetTag, string? AssetName, string? Employee, string? Actor, string? Details);

public sealed class CompanyWebhookWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<CompanyWebhookWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Webhooks:WorkerEnabled", true)) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<CompanyWebhookDeliveryService>().RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception)
            {
                logger.LogWarning("Company webhook delivery cycle failed ({ErrorType}).", exception.GetType().Name);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
