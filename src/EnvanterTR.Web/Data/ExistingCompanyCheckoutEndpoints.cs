using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static partial class CheckoutEndpoints
{
    private sealed record ExistingCheckoutOwner(Company Company, EnvanterUser Account);
    private sealed record ExistingCompanyCheckoutMetadata;

    private static void MapExistingCompanyCheckoutEndpoints(IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/subscription/checkout").RequireAuthorization("company.member");
        api.MapPost("/initialize", InitializeAsync).RequireRateLimiting("checkout")
            .WithMetadata(new RequestSizeLimitAttribute(16384), new ExistingCompanyCheckoutMetadata());
        api.MapGet("/context", async (ClaimsPrincipal user, AppDbContext db, UserManager<EnvanterUser> users, CancellationToken ct) =>
        {
            var owner = await ResolveExistingCheckoutOwnerAsync(user, db, users, ct);
            return owner is null ? Results.Forbid() : Results.Ok(new { companyId = owner.Company.Id,
                companyName = owner.Company.Name, slug = owner.Company.Slug, ownerEmail = owner.Account.Email,
                ownerName = owner.Account.DisplayName, mfaEnabled = owner.Account.TwoFactorEnabled, annualOnly = true });
        });
        api.MapGet("/orders/{checkoutId:guid}", async (Guid checkoutId, ClaimsPrincipal user, AppDbContext db,
            UserManager<EnvanterUser> users, CancellationToken ct) =>
        {
            var owner = await ResolveExistingCheckoutOwnerAsync(user, db, users, ct);
            if (owner is null) return Results.Forbid();
            var order = await db.CompanyCheckoutOrders.AsNoTracking().Where(item => item.Id == checkoutId &&
                item.TargetCompanyId == owner.Company.Id && item.RequestedByUserId == owner.Account.Id)
                .Select(item => new { orderId = item.Id, item.Status, item.Amount, item.Currency, item.CreatedAt, item.PaidAt }).SingleOrDefaultAsync(ct);
            return order is null ? Results.NotFound() : Results.Ok(order);
        });
    }

    private static async Task<ExistingCheckoutOwner?> ResolveExistingCheckoutOwnerAsync(ClaimsPrincipal user,
        AppDbContext db, UserManager<EnvanterUser> users, CancellationToken ct)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty ||
            !Guid.TryParse(user.FindFirstValue("company_id"), out var companyId) || companyId == Guid.Empty ||
            !await db.CompanyMemberships.AnyAsync(member => member.CompanyId == companyId && member.UserId == userId && member.RoleKey == "owner", ct)) return null;
        var company = await db.Companies.SingleOrDefaultAsync(item => item.Id == companyId && item.IsActive, ct);
        var account = await users.FindByIdAsync(userId.ToString());
        return company is null || !TenantHostResolver.IsValidSlug(company.Slug) || account?.EmailConfirmed != true
            ? null : new(company, account);
    }

    private static async Task<IResult?> ReauthenticateCheckoutOwnerAsync(EnvanterUser owner, InitializeCompanyCheckoutRequest request,
        UserManager<EnvanterUser> users, SignInManager<EnvanterUser> signIn)
    {
        if (!owner.TwoFactorEnabled)
            return Results.Json(new { code = "company_checkout_mfa_required", message = "Ödeme başlatmadan önce Authenticator güvenliğini etkinleştirin." }, statusCode: 428);
        if (string.IsNullOrEmpty(request.CurrentPassword) || request.CurrentPassword.Length > 1024 ||
            string.IsNullOrWhiteSpace(request.TwoFactorCode) || request.TwoFactorCode.Length > 32)
            return Results.Json(new { code = "company_checkout_reauth_required", message = "Ödeme için mevcut parolanızı ve Authenticator kodunuzu doğrulayın." }, statusCode: 428);
        if (!await signIn.CanSignInAsync(owner) || await users.IsLockedOutAsync(owner))
            return Results.BadRequest(new { message = "Hesap doğrulaması başarısız." });
        if (!await users.CheckPasswordAsync(owner, request.CurrentPassword))
        {
            await users.AccessFailedAsync(owner);
            return Results.BadRequest(new { message = "Hesap doğrulaması başarısız." });
        }
        var code = request.TwoFactorCode.Trim();
        var verified = await users.VerifyTwoFactorTokenAsync(owner, TokenOptions.DefaultAuthenticatorProvider,
            code.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal));
        if (!verified) verified = (await users.RedeemTwoFactorRecoveryCodeAsync(owner, code)).Succeeded;
        if (!verified)
        {
            await users.AccessFailedAsync(owner);
            return Results.BadRequest(new { message = "Authenticator veya kurtarma kodu doğrulanamadı." });
        }
        await users.ResetAccessFailedCountAsync(owner);
        return null;
    }
}
