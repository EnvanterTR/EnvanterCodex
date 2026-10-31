using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Invitation_email_token_survives_rejected_password_and_activates_once()
    {
        string email, originalStamp, originalHash;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var user = (await users.FindByIdAsync(_userId.ToString()))!;
            user.EmailConfirmed = false;
            Assert.True((await users.UpdateAsync(user)).Succeeded);
            email = user.Email!;
            originalStamp = user.SecurityStamp!;
            originalHash = user.PasswordHash!;
            var token = await users.GeneratePasswordResetTokenAsync(user);
            await AccountEndpoints.SendAccountInvitationEmailAsync(email, "Invitation acceptance", token,
                scope.ServiceProvider.GetRequiredService<IConfiguration>(), CancellationToken.None, "invitation-acceptance");
        }

        var html = DecodeMimeBody(Assert.Single(_smtpCapture.Messages));
        var linkMatch = Regex.Match(html, "href=\"([^\"]*reset-password\\.html[^\"]*)\"");
        Assert.True(linkMatch.Success, "SMTP invitation must contain its activation link.");
        var link = new Uri(WebUtility.HtmlDecode(linkMatch.Groups[1].Value));
        Assert.Equal(string.Empty, link.Query);
        var fragment = QueryHelpers.ParseQuery(link.Fragment.TrimStart('#'));
        Assert.Equal(email, fragment["email"].ToString());
        var deliveredToken = fragment["token"].ToString();
        Assert.False(string.IsNullOrWhiteSpace(deliveredToken));

        using var csrf = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var csrfToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(x => x.Split(';', 2)[0])
            .First(x => x.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        foreach (var rejectedPassword in new[] { "short", "synthetic-password-42!" })
        {
            using var rejected = await SendAsRoleAsync(HttpMethod.Post, "/api/session/password/reset", "admin",
                JsonContent.Create(new { email, token = deliveredToken, newPassword = rejectedPassword }), csrfToken, cookie);
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        }
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var user = (await users.FindByIdAsync(_userId.ToString()))!;
            Assert.False(user.EmailConfirmed);
            Assert.Equal(originalStamp, user.SecurityStamp);
            Assert.Equal(originalHash, user.PasswordHash);
        }

        const string acceptedPassword = "Synthetic.Invitation-Retry-42!";
        using (var accepted = await SendAsRoleAsync(HttpMethod.Post, "/api/session/password/reset", "admin",
                   JsonContent.Create(new { email, token = deliveredToken, newPassword = acceptedPassword }), csrfToken, cookie))
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var user = (await users.FindByIdAsync(_userId.ToString()))!;
            Assert.True(user.EmailConfirmed);
            Assert.True(await users.CheckPasswordAsync(user, acceptedPassword));
            Assert.NotEqual(originalStamp, user.SecurityStamp);
        }
        using var replay = await SendAsRoleAsync(HttpMethod.Post, "/api/session/password/reset", "admin",
            JsonContent.Create(new { email, token = deliveredToken, newPassword = acceptedPassword }), csrfToken, cookie);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
    }
}
