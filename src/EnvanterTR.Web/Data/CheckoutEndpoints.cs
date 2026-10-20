using System.Data;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnvanterTR.Web.Data;

public sealed record InitializeCompanyCheckoutRequest(
    Guid CheckoutId, string CompanyName, string Slug, string? ContactEmail,
    string OwnerFirstName, string OwnerLastName, string OwnerEmail, string Phone,
    string IdentityNumber, string BillingAddress, string City, string Country, string? ZipCode,
    Guid PlanId, string Term, bool DataProcessingAcknowledged, string? DataProcessingNoticeVersion);

public static class CheckoutEndpoints
{
    private const string CheckoutStatusStarted = "PaymentStarted";
    private const string CheckoutNoticeVersion = "cloud-checkout-data-v1";
    private static readonly Regex PhonePattern = new("^\\+[1-9][0-9]{7,14}$", RegexOptions.CultureInvariant);
    private static readonly Regex IdentityPattern = new("^[0-9]{11}$", RegexOptions.CultureInvariant);

    public static IEndpointRouteBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/checkout/status", (IOptions<IyzipayOptions> configuredOptions) =>
        {
            var options = configuredOptions.Value;
            var available = options.IsConfigured && IsTrustedCallbackUrl(options.CallbackUrl) && IsTrustedApiBase(options.BaseUrl);
            return Results.Ok(new
            {
                available,
                provider = available ? "iyzico" : null,
                message = available ? null : "Çevrimiçi ödeme şu anda kullanılamıyor. Bilgileriniz gönderilmeden önce ödeme yapılandırmasının tamamlanması gerekiyor."
            });
        }).AllowAnonymous();
        endpoints.MapPost("/api/checkout/initialize", InitializeAsync)
            .AllowAnonymous().RequireRateLimiting("checkout");
        endpoints.MapPost("/api/checkout/callback", CallbackAsync)
            .AllowAnonymous().RequireRateLimiting("customer-api")
            .WithMetadata(new RequestSizeLimitAttribute(8192));
        endpoints.MapPost("/api/checkout/webhook", WebhookAsync)
            .AllowAnonymous().RequireRateLimiting("customer-api")
            .WithMetadata(new RequestSizeLimitAttribute(8192));
        return endpoints;
    }

    private static async Task<IResult> InitializeAsync(
        [FromBody] InitializeCompanyCheckoutRequest request,
        HttpContext context, IAntiforgery antiforgery, AppDbContext db,
        IyzipayCheckoutClient iyzipay, IOptions<IyzipayOptions> configuredOptions,
        IDataProtectionProvider dataProtectionProvider, CancellationToken cancellationToken)
    {
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest(new { message = "Güvenlik doğrulaması yenilenemedi. Sayfayı yenileyip tekrar deneyin." });
        }

        var options = configuredOptions.Value;
        if (!options.IsConfigured || !IsTrustedCallbackUrl(options.CallbackUrl) || !IsTrustedApiBase(options.BaseUrl))
            return Results.Problem("Çevrimiçi ödeme şu anda yapılandırılmamış. Şirket kaydı başlatılmadı.",
                statusCode: StatusCodes.Status503ServiceUnavailable);

        var companyName = request.CompanyName?.Trim();
        var slug = request.Slug?.Trim().ToLowerInvariant();
        var firstName = request.OwnerFirstName?.Trim();
        var lastName = request.OwnerLastName?.Trim();
        var ownerEmail = request.OwnerEmail?.Trim();
        var contactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? ownerEmail : request.ContactEmail.Trim();
        var phone = request.Phone?.Trim();
        var identityNumber = request.IdentityNumber?.Trim();
        var billingAddress = request.BillingAddress?.Trim();
        var city = request.City?.Trim();
        var country = request.Country?.Trim();
        var zipCode = string.IsNullOrWhiteSpace(request.ZipCode) ? null : request.ZipCode.Trim();

        if (request.CheckoutId == Guid.Empty || string.IsNullOrWhiteSpace(companyName) || companyName.Length > 200 ||
            !TenantHostResolver.IsValidSlug(slug) || string.IsNullOrWhiteSpace(firstName) || firstName.Length > 80 ||
            string.IsNullOrWhiteSpace(lastName) || lastName.Length > 80 || string.IsNullOrWhiteSpace(ownerEmail) ||
            ownerEmail.Length > 256 || !MailAddress.TryCreate(ownerEmail, out _) || contactEmail is null ||
            contactEmail.Length > 256 || !MailAddress.TryCreate(contactEmail, out _) || phone is null || !PhonePattern.IsMatch(phone) ||
            identityNumber is null || !IdentityPattern.IsMatch(identityNumber) || string.IsNullOrWhiteSpace(billingAddress) ||
            billingAddress.Length > 500 || string.IsNullOrWhiteSpace(city) || city.Length > 100 ||
            string.IsNullOrWhiteSpace(country) || country.Length > 100 || (zipCode?.Length > 24) ||
            request.Term is not ("monthly" or "annual") || !request.DataProcessingAcknowledged ||
            request.DataProcessingNoticeVersion != CheckoutNoticeVersion)
            return Results.ValidationProblem(new Dictionary<string, string[]> {
                ["checkout"] = ["Firma, alt alan adı, yetkili, fatura bilgileri ve veri işleme bilgilendirmesi onayını kontrol edin."]
            });

        var plan = await db.SubscriptionPlans.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.PlanId &&
            x.IsActive && x.IsPublic && x.DeploymentKind == "cloud", cancellationToken);
        if (plan is null)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["planId"] = ["Satın alınabilir Cloud paketi bulunamadı."] });
        var termDays = request.Term == "annual" ? 365 : 30;
        var amount = request.Term == "annual" ? plan.AnnualPrice : plan.MonthlyPrice;
        var currency = plan.Currency.ToUpperInvariant();
        if (amount <= 0 || currency is not ("TRY" or "USD" or "EUR" or "GBP" or "NOK" or "CHF"))
            return Results.Problem("Seçilen Cloud paketi için ödeme fiyatı veya para birimi kullanılamıyor.",
                statusCode: StatusCodes.Status503ServiceUnavailable);

        var ownerName = $"{firstName} {lastName}";
        var existingOrder = await db.CompanyCheckoutOrders.SingleOrDefaultAsync(x => x.Id == request.CheckoutId, cancellationToken);
        CompanyCheckoutOrder pending;
        if (existingOrder is not null)
        {
            if (!SameRequest(existingOrder, companyName, slug!, contactEmail, ownerName, ownerEmail!, plan.Id, termDays,
                    amount, currency, CheckoutNoticeVersion))
                return Results.Conflict(new { message = "Bu satın alma kimliği başka bilgilerle kullanılmış." });
            if (existingOrder.Status is CheckoutStatusStarted or "PaymentReview" &&
                !string.IsNullOrWhiteSpace(existingOrder.CheckoutPageUrlProtected))
            {
                try
                {
                    var url = dataProtectionProvider.CreateProtector("EnvanterTR.Iyzico.CheckoutPage.v1")
                        .Unprotect(existingOrder.CheckoutPageUrlProtected);
                    return Results.Ok(new { checkoutUrl = url, orderId = existingOrder.Id });
                }
                catch (CryptographicException)
                {
                    return Results.Problem("Ödeme oturumu güvenli biçimde açılamadı. Yeni bir satın alma başlatın.",
                        statusCode: StatusCodes.Status409Conflict);
                }
            }
            if (existingOrder.Status != "Pending")
                return Results.Conflict(new { message = "Bu satın alma tamamlanmış veya yeniden başlatılamaz durumda. Yeni bir ödeme denemesi oluşturun." });

            var now = DateTimeOffset.UtcNow;
            var leaseAcquired = await db.CompanyCheckoutOrders
                .Where(x => x.Id == request.CheckoutId && x.Status == "Pending" && x.ExpiresAt > now &&
                    (x.InitializationLeaseUntil == null || x.InitializationLeaseUntil <= now))
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.InitializationLeaseUntil, now.AddMinutes(2)), cancellationToken);
            if (leaseAcquired != 1)
            {
                context.Response.Headers.RetryAfter = "10";
                return Results.Conflict(new { message = "Ödeme oturumu hazırlanıyor. Aynı sayfadan birkaç saniye sonra tekrar deneyin." });
            }
            pending = existingOrder;
        }
        else
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var reservedStatuses = new[] { "Pending", CheckoutStatusStarted, "PaymentReview", "ProvisioningFailed" };
            var now = DateTimeOffset.UtcNow;
            var abandoned = await db.CompanyCheckoutOrders.Where(x => x.Slug == slug &&
                (x.Status == "Pending" || x.Status == CheckoutStatusStarted) && x.ExpiresAt <= now).ToListAsync(cancellationToken);
            foreach (var expired in abandoned) expired.Status = "Expired";
            if (abandoned.Count > 0) await db.SaveChangesAsync(cancellationToken);
            if (await db.Companies.AnyAsync(x => x.Slug == slug, cancellationToken) ||
                await db.CompanyCheckoutOrders.AnyAsync(x => x.Slug == slug && reservedStatuses.Contains(x.Status), cancellationToken))
                return Results.Conflict(new { message = "Bu şirket adresi zaten kullanılıyor veya başka bir ödeme işlemi için ayrılmış." });

            pending = new CompanyCheckoutOrder
            {
                Id = request.CheckoutId, CompanyName = companyName, Slug = slug!, ContactEmail = contactEmail,
                OwnerName = ownerName, OwnerEmail = ownerEmail!, PlanId = plan.Id, TermDays = termDays,
                Amount = amount, Currency = currency, Status = "Pending",
                DataProcessingNoticeVersion = CheckoutNoticeVersion,
                DataProcessingAcknowledgedAt = now,
                InitializationLeaseUntil = now.AddMinutes(2),
                CreatedAt = now, ExpiresAt = now.AddDays(3)
            };
            db.CompanyCheckoutOrders.Add(pending);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "Satın alma veya şirket adresi başka bir işlemle çakıştı. Bilgileri yenileyip tekrar deneyin." });
            }
        }

        var buyerId = $"ET-{request.CheckoutId:N}";
        var requestBody = new
        {
            locale = "tr", conversationId = request.CheckoutId.ToString("N"),
            price = amount, paidPrice = amount, currency, basketId = request.CheckoutId.ToString("N"),
            paymentGroup = "SUBSCRIPTION", callbackUrl = options.CallbackUrl,
            enabledInstallments = new[] { 1 },
            buyer = new
            {
                id = buyerId, name = firstName, surname = lastName, identityNumber,
                email = ownerEmail, gsmNumber = phone, registrationAddress = billingAddress,
                city, country, zipCode, ip = GetClientIp(context)
            },
            billingAddress = new { address = billingAddress, contactName = ownerName, city, country, zipCode },
            basketItems = new[] { new { id = request.CheckoutId.ToString("N"), price = amount,
                name = plan.Name.Length > 100 ? plan.Name[..100] : plan.Name, category1 = "EnvanterTR Cloud", itemType = "VIRTUAL" } }
        };

        try
        {
            using var response = await iyzipay.InitializeAsync(requestBody, cancellationToken);
            var root = response.RootElement;
            var token = ReadString(root, "token");
            var paymentPageUrl = ReadString(root, "paymentPageUrl");
            var responseConversation = ReadString(root, "conversationId");
            if (!string.Equals(ReadString(root, "status"), "success", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(token) || !Uri.TryCreate(paymentPageUrl, UriKind.Absolute, out var paymentUri) ||
                paymentUri.Scheme != Uri.UriSchemeHttps || paymentUri.Host != new Uri(options.BaseUrl).Host ||
                !string.Equals(responseConversation, request.CheckoutId.ToString("N"), StringComparison.Ordinal))
            {
                await SetStatusAsync(db, pending.Id, "PaymentFailed", cancellationToken);
                return Results.Problem("Ödeme oturumu sağlayıcıdan doğrulanmış bir bağlantı alamadı.",
                    statusCode: StatusCodes.Status502BadGateway);
            }

            var order = await db.CompanyCheckoutOrders.SingleAsync(x => x.Id == pending.Id, cancellationToken);
            order.CheckoutTokenHash = HashToken(token);
            order.CheckoutPageUrlProtected = dataProtectionProvider.CreateProtector("EnvanterTR.Iyzico.CheckoutPage.v1")
                .Protect(paymentUri.AbsoluteUri);
            order.Status = CheckoutStatusStarted;
            order.InitializationLeaseUntil = null;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { checkoutUrl = paymentUri.AbsoluteUri, orderId = order.Id });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            await SetStatusAsync(db, pending.Id, "PaymentFailed", cancellationToken);
            return Results.Problem("Ödeme sağlayıcısına şu anda ulaşılamıyor. Bir süre sonra tekrar deneyin.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<IResult> CallbackAsync(
        HttpContext context, AppDbContext db, IyzipayCheckoutClient iyzipay,
        UserManager<EnvanterUser> userManager, IConfiguration configuration, ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (!context.Request.HasFormContentType)
            return Results.BadRequest(new { message = "Ödeme bildirimi biçimi geçersiz." });
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var token = form["token"].ToString();
        if (string.IsNullOrWhiteSpace(token) || token.Length > 256)
            return Results.BadRequest(new { message = "Ödeme bildirimi doğrulanamadı." });
        return await ProcessCheckoutTokenAsync(token, null, null, context, db, iyzipay, userManager,
            configuration, logger, cancellationToken);
    }

    private static async Task<IResult> WebhookAsync(
        HttpContext context, AppDbContext db, IyzipayCheckoutClient iyzipay,
        UserManager<EnvanterUser> userManager, IConfiguration configuration, IOptions<IyzipayOptions> configuredOptions,
        ILogger<Program> logger, CancellationToken cancellationToken)
    {
        var options = configuredOptions.Value;
        if (!options.IsConfigured) return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!context.Request.HasJsonContentType())
            return Results.BadRequest(new { message = "Ödeme bildirimi biçimi geçersiz." });
        if (context.Request.ContentLength is > 8192)
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        if (!context.Request.Headers.TryGetValue("X-IYZ-SIGNATURE-V3", out var signatureValues) ||
            signatureValues.Count != 1 || string.IsNullOrWhiteSpace(signatureValues[0]))
            return Results.Unauthorized();

        byte[] body;
        try { body = await ReadLimitedRequestBodyAsync(context.Request.Body, 8192, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (InvalidDataException) { return Results.StatusCode(StatusCodes.Status413PayloadTooLarge); }

        using var payloadBytes = new SensitiveBuffer(body);
        JsonDocument payload;
        try { payload = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 8 }); }
        catch (JsonException) { return Results.BadRequest(new { message = "Ödeme bildirimi JSON biçimi geçersiz." }); }
        using (payload)
        {
            var root = payload.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return Results.BadRequest();
            var eventType = ReadString(root, "iyziEventType");
            var paymentId = ReadString(root, "iyziPaymentId");
            var token = ReadString(root, "token");
            var conversationId = ReadString(root, "paymentConversationId");
            var status = ReadString(root, "status");
            if (string.IsNullOrWhiteSpace(eventType) || eventType.Length > 64 ||
                string.IsNullOrWhiteSpace(paymentId) || paymentId.Length > 180 ||
                string.IsNullOrWhiteSpace(token) || token.Length > 256 ||
                string.IsNullOrWhiteSpace(conversationId) || conversationId.Length > 64 ||
                string.IsNullOrWhiteSpace(status) || status.Length > 32)
                return Results.BadRequest(new { message = "Ödeme bildirimi alanları geçersiz." });
            if (!IyzipayWebhookSignature.Verify(options.SecretKey, eventType, paymentId, token,
                    conversationId, status, signatureValues[0]!))
                return Results.Unauthorized();

            // Signed events that this integration does not handle are acknowledged without changing an order.
            if (!string.Equals(eventType, "CHECKOUT_FORM_AUTH", StringComparison.Ordinal) ||
                !(string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(status, "FAILURE", StringComparison.OrdinalIgnoreCase)))
                return Results.Ok();

            var order = await db.CompanyCheckoutOrders.AsNoTracking()
                .SingleOrDefaultAsync(x => x.CheckoutTokenHash == HashToken(token), cancellationToken);
            if (order is null || !string.Equals(conversationId, order.Id.ToString("N"), StringComparison.Ordinal))
            {
                logger.LogWarning("Signed iyzico checkout webhook could not be correlated to an order.");
                return Results.Ok();
            }

            var processingResult = await ProcessCheckoutTokenAsync(token, paymentId, status.ToUpperInvariant(), context, db,
                iyzipay, userManager, configuration, logger, cancellationToken);
            if (processingResult is IStatusCodeHttpResult httpResult && (httpResult.StatusCode ?? StatusCodes.Status200OK) is >= 200 and < 300)
                return Results.Ok(new { received = true });
            return processingResult;
        }
    }

    private static async Task<IResult> ProcessCheckoutTokenAsync(string token, string? expectedPaymentId,
        string? expectedPaymentStatus, HttpContext context, AppDbContext db, IyzipayCheckoutClient iyzipay,
        UserManager<EnvanterUser> userManager, IConfiguration configuration, ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(token);
        var order = await db.CompanyCheckoutOrders.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CheckoutTokenHash == tokenHash, cancellationToken);
        if (order is null)
            return Results.BadRequest(new { message = "Ödeme işlemi bulunamadı." });
        if (order.Status is "Provisioned" or "PaymentFailed")
        {
            var terminalPaymentStatus = order.Status == "Provisioned" ? "SUCCESS" : "FAILURE";
            if (expectedPaymentId is not null && !string.Equals(order.PaymentReference, expectedPaymentId, StringComparison.Ordinal) ||
                expectedPaymentStatus is not null && !string.Equals(terminalPaymentStatus, expectedPaymentStatus, StringComparison.Ordinal))
                return Results.Conflict(new { message = "Ödeme bildirimi kayıtlı sağlayıcı sonucu ile eşleşmedi." });
            return CheckoutResult(order.Status == "Provisioned" ? "success" : "failed",
                order.Status == "Provisioned" ? order.Slug : null, configuration);
        }
        if (order.Status is not (CheckoutStatusStarted or "PaymentReview" or "ProvisioningFailed" or "Expired"))
            return Results.Conflict(new { message = "Ödeme henüz doğrulanabilir durumda değil." });

        JsonDocument result;
        try
        {
            result = await iyzipay.RetrieveAsync(new
            {
                locale = "tr", conversationId = order.Id.ToString("N"), token
            }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogWarning("iyzico checkout result could not be retrieved. Error type: {ErrorType}", exception.GetType().Name);
            return Results.Problem("Ödeme sonucu sağlayıcıdan doğrulanamadı. Bu sayfayı kapatmadan kısa süre sonra tekrar deneyin.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        using (result)
        {
            var root = result.RootElement;
            if (!string.Equals(ReadString(root, "status"), "success", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(ReadString(root, "token"), token, StringComparison.Ordinal) ||
                !string.Equals(ReadString(root, "conversationId"), order.Id.ToString("N"), StringComparison.Ordinal) ||
                !string.Equals(ReadString(root, "basketId"), order.Id.ToString("N"), StringComparison.Ordinal) ||
                !string.Equals(ReadString(root, "currency"), order.Currency, StringComparison.Ordinal) ||
                !TryReadDecimal(root, "price", out var price) || price != order.Amount ||
                !TryReadDecimal(root, "paidPrice", out var paidPrice) || paidPrice != order.Amount ||
                !int.TryParse(ReadString(root, "fraudStatus"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var fraudStatus))
                return Results.BadRequest(new { message = "Ödeme bilgileri siparişle eşleşmedi; şirket hesabı açılmadı." });

            var paymentStatus = ReadString(root, "paymentStatus");
            var paymentReference = ReadString(root, "paymentId");
            if (expectedPaymentId is not null && !string.Equals(paymentReference, expectedPaymentId, StringComparison.Ordinal) ||
                expectedPaymentStatus is not null && !string.Equals(paymentStatus, expectedPaymentStatus, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Signed iyzico checkout webhook did not match the retrieved payment result. Checkout {CheckoutId}", order.Id);
                return Results.Conflict(new { message = "Ödeme bildirimi sağlayıcıdan doğrulanan sonuçla eşleşmedi." });
            }
            if (!string.Equals(paymentStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase) || fraudStatus == -1)
            {
                await SetStatusAsync(db, order.Id, "PaymentFailed", cancellationToken, paymentReference);
                return CheckoutResult("failed");
            }
            if (fraudStatus == 0)
            {
                await SetStatusAsync(db, order.Id, "PaymentReview", cancellationToken, paymentReference);
                return CheckoutResult("review");
            }
            if (fraudStatus != 1 || string.IsNullOrWhiteSpace(paymentReference) || paymentReference.Length > 180)
                return Results.BadRequest(new { message = "Ödeme henüz onaylanmadı; şirket hesabı açılmadı." });

            string? invitationEmail = null;
            string? invitationName = null;
            string? companyName = null;
            var invitationEmailSent = false;
            bool invitationNeeded = false;
            try
            {
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                var lockedOrder = await db.CompanyCheckoutOrders.FromSqlInterpolated(
                    $"SELECT * FROM \"CompanyCheckoutOrders\" WHERE \"Id\" = {order.Id} FOR UPDATE")
                    .SingleAsync(cancellationToken);
                if (lockedOrder.Status == "Provisioned")
                {
                    await transaction.CommitAsync(cancellationToken);
                    return CheckoutResult("success", lockedOrder.Slug, configuration);
                }
                if (lockedOrder.Status == "PaymentFailed")
                {
                    await transaction.CommitAsync(cancellationToken);
                    return CheckoutResult("failed");
                }
                if (await db.Companies.AnyAsync(x => x.Slug == lockedOrder.Slug, cancellationToken))
                {
                    lockedOrder.Status = "ProvisioningFailed";
                    lockedOrder.PaymentReference = paymentReference;
                    lockedOrder.PaidAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    logger.LogError("Verified checkout payment could not provision because its company slug is occupied. Checkout {CheckoutId}", lockedOrder.Id);
                    return CheckoutResult("support");
                }

                var plan = await db.SubscriptionPlans.SingleOrDefaultAsync(x => x.Id == lockedOrder.PlanId &&
                    x.IsActive && x.DeploymentKind == "cloud", cancellationToken);
                if (plan is null || plan.Currency != lockedOrder.Currency)
                {
                    lockedOrder.Status = "ProvisioningFailed";
                    lockedOrder.PaymentReference = paymentReference;
                    lockedOrder.PaidAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return CheckoutResult("support");
                }

                var owner = await userManager.FindByEmailAsync(lockedOrder.OwnerEmail);
                var wasConfirmed = owner?.EmailConfirmed == true;
                if (owner is null)
                {
                    var temporaryPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "aA1!";
                    owner = new EnvanterUser { UserName = lockedOrder.OwnerEmail, Email = lockedOrder.OwnerEmail,
                        EmailConfirmed = false, DisplayName = lockedOrder.OwnerName };
                    var created = await userManager.CreateAsync(owner, temporaryPassword);
                    if (!created.Succeeded)
                        throw new InvalidOperationException("Owner account creation failed.");
                }

                var now = DateTimeOffset.UtcNow;
                var company = new Company { Name = lockedOrder.CompanyName, Slug = lockedOrder.Slug,
                    ContactEmail = lockedOrder.ContactEmail, SecurityRequireMfa = true };
                db.Companies.Add(company);
                db.CompanyMemberships.Add(new CompanyMembership { CompanyId = company.Id, UserId = owner.Id, RoleKey = "owner" });
                db.TenantSubscriptions.Add(new TenantSubscription { CompanyId = company.Id, PlanId = plan.Id,
                    Status = "Active", IsDemo = false, StartsAt = now, EndsAt = now.AddDays(lockedOrder.TermDays),
                    PaymentReference = paymentReference });
                lockedOrder.CompanyId = company.Id;
                lockedOrder.PaymentReference = paymentReference;
                lockedOrder.PaidAt = now;
                lockedOrder.Status = "Provisioned";
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                invitationEmail = owner.Email;
                invitationName = owner.DisplayName;
                companyName = company.Name;
                invitationNeeded = !wasConfirmed;
                if (wasConfirmed) invitationNeeded = true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                logger.LogError("Verified checkout payment provisioning failed. Error type: {ErrorType}", exception.GetType().Name);
                await RecordProvisioningFailureAsync(db, order.Id, paymentReference, cancellationToken);
                return CheckoutResult("support");
            }

            if (invitationNeeded && invitationEmail is not null)
            {
                try
                {
                    var owner = await userManager.FindByEmailAsync(invitationEmail);
                    if (owner?.EmailConfirmed == true)
                    {
                        await AccountEndpoints.SendCompanyAccessAddedEmailAsync(invitationEmail, invitationName ?? invitationEmail,
                            companyName ?? "şirket alanı", "owner", configuration, cancellationToken, order.Slug);
                        invitationEmailSent = true;
                    }
                    else if (owner is not null)
                    {
                        var resetToken = await userManager.GeneratePasswordResetTokenAsync(owner);
                        await AccountEndpoints.SendAccountInvitationEmailAsync(invitationEmail, invitationName ?? invitationEmail,
                            resetToken, configuration, cancellationToken, order.Slug);
                        invitationEmailSent = true;
                    }
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogWarning("Paid company owner invitation could not be sent. Error type: {ErrorType}", exception.GetType().Name);
                }
            }
            return CheckoutResult("success", order.Slug, configuration, invitationEmailSent);
        }
    }

    private static bool SameRequest(CompanyCheckoutOrder order, string companyName, string slug, string contactEmail,
        string ownerName, string ownerEmail, Guid planId, int termDays, decimal amount, string currency, string noticeVersion) =>
        order.CompanyName == companyName && order.Slug == slug && order.ContactEmail == contactEmail &&
        order.OwnerName == ownerName && order.OwnerEmail.Equals(ownerEmail, StringComparison.OrdinalIgnoreCase) &&
        order.PlanId == planId && order.TermDays == termDays && order.Amount == amount && order.Currency == currency &&
        order.DataProcessingAcknowledgedAt is not null && order.DataProcessingNoticeVersion == noticeVersion;

    private static async Task SetStatusAsync(AppDbContext db, Guid id, string status,
        CancellationToken cancellationToken, string? paymentReference = null)
    {
        var order = await db.CompanyCheckoutOrders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null || order.Status == "Provisioned") return;
        order.Status = status;
        order.InitializationLeaseUntil = null;
        if (paymentReference is not null) order.PaymentReference = paymentReference;
        if (status == "PaymentFailed") order.PaidAt = null;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task RecordProvisioningFailureAsync(AppDbContext db, Guid id, string paymentReference,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        var order = await db.CompanyCheckoutOrders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null || order.Status == "Provisioned") return;
        order.Status = "ProvisioningFailed";
        order.InitializationLeaseUntil = null;
        order.PaymentReference = paymentReference;
        order.PaidAt ??= DateTimeOffset.UtcNow;
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); }
    }

    private static IResult CheckoutResult(string state, string? companySlug = null,
        IConfiguration? configuration = null, bool invitationEmailSent = false)
    {
        var (title, message) = state switch
        {
            "success" => ("Ödeme alındı", invitationEmailSent
                ? "Ödeme doğrulandı ve şirket alanınız açıldı. Parola belirleme veya şirket erişimi e-postası gönderildi. E-posta görünmüyorsa gereksiz klasörünü kontrol edin."
                : "Ödeme doğrulandı ve şirket alanınız açıldı. Erişim e-postası daha önce gönderilmiş olabilir; gelen kutusu ve gereksiz klasörünü kontrol edin. Portalı açın; giriş bağlantısı yoksa parola yenileme seçeneğini kullanın."),
            "review" => ("Ödeme inceleniyor", "Ödeme sağlayıcının güvenlik incelemesinde. Sonuç kesinleşince şirket alanı açılacak."),
            "support" => ("İşlem doğrulandı", "Ödeme doğrulandı; şirket alanının açılması için destek ekibiyle iletişime geçin."),
            _ => ("Ödeme tamamlanmadı", "Ödeme doğrulanamadı. Ücret alındığını düşünüyorsanız destek ekibiyle iletişime geçin.")
        };
        var portalUrl = state == "success" && companySlug is not null && configuration is not null
            ? TenantHostResolver.BuildPortalUrl(companySlug, configuration["TenantRouting:RootDomain"])
            : null;
        var portalLink = portalUrl is null ? string.Empty
            : $"<p>Şirket portalınız: <a href=\"{WebUtility.HtmlEncode(portalUrl)}\">{WebUtility.HtmlEncode(portalUrl)}</a></p>";
        var html = $"<!doctype html><html lang=\"tr\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>{WebUtility.HtmlEncode(title)} · EnvanterTR</title><body style=\"font:16px Segoe UI,Arial,sans-serif;background:#101c30;color:#18263b;display:grid;place-items:center;min-height:100vh;margin:0\"><main style=\"max-width:560px;margin:24px;padding:36px;border-radius:18px;background:white\"><h1>{WebUtility.HtmlEncode(title)}</h1><p>{WebUtility.HtmlEncode(message)}</p>{portalLink}<a href=\"/\">EnvanterTR ana sayfasına dön</a></main></body></html>";
        return Results.Content(html, "text/html; charset=utf-8", Encoding.UTF8);
    }

    private static bool IsTrustedCallbackUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
        uri.IsDefaultPort && (uri.Host is "envantertr.com" or "www.envantertr.com") && uri.AbsolutePath == "/api/checkout/callback";

    private static bool IsTrustedApiBase(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps && uri.UserInfo.Length == 0 && uri.AbsolutePath == "/" &&
        uri.Host is "api.iyzipay.com" or "sandbox-api.iyzipay.com";

    private static string? GetClientIp(HttpContext context)
    {
        var forwardedHeadersEnabled = context.RequestServices.GetRequiredService<IConfiguration>()
            .GetValue<bool>("ForwardedHeaders:Enabled");
        return forwardedHeadersEnabled ? context.Connection.RemoteIpAddress?.ToString() : null;
    }

    private static string HashToken(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static async Task<byte[]> ReadLimitedRequestBodyAsync(Stream body, int limit, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[4096];
        try
        {
            while (true)
            {
                var read = await body.ReadAsync(chunk, cancellationToken);
                if (read == 0) break;
                if (buffer.Length + read > limit) throw new InvalidDataException("Webhook body exceeds the allowed size.");
                buffer.Write(chunk, 0, read);
            }
            return buffer.ToArray();
        }
        finally { CryptographicOperations.ZeroMemory(chunk); }
    }

    private sealed class SensitiveBuffer(byte[] bytes) : IDisposable
    {
        public void Dispose() => CryptographicOperations.ZeroMemory(bytes);
    }

    private static string? ReadString(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) ? value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString() : null;
    private static bool TryReadDecimal(JsonElement root, string property, out decimal value) =>
        root.TryGetProperty(property, out var token) && token.TryGetDecimal(out value) || SetZero(out value);
    private static bool SetZero(out decimal value) { value = 0; return false; }
}
