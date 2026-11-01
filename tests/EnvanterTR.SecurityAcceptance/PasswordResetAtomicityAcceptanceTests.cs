using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Invitation_activation_database_failure_preserves_password_sessions_token_and_audits_for_retry()
    {
        string email, resetToken, originalHash, originalStamp;
        var sessionId = Guid.NewGuid();
        var function = "acceptance_confirm_failure_" + _userId.ToString("N");
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var user = (await users.FindByIdAsync(_userId.ToString()))!;
            user.EmailConfirmed = false;
            Assert.True((await users.UpdateAsync(user)).Succeeded);
            email = user.Email!; originalHash = user.PasswordHash!; originalStamp = user.SecurityStamp!;
            resetToken = await users.GeneratePasswordResetTokenAsync(user);
            db.UserSessions.Add(new UserSession { Id = sessionId, UserId = _userId, CompanyId = _companyId,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) });
            await db.SaveChangesAsync();
            // Only this disposable fixture account is affected; no production connection is used.
#pragma warning disable EF1002 // DDL identifiers/literal are derived only from a Guid, never external input; SQL parameters cannot represent identifiers.
            await db.Database.ExecuteSqlRawAsync($$"""
                CREATE FUNCTION "{{function}}"() RETURNS trigger LANGUAGE plpgsql AS $guard$
                BEGIN
                    IF NEW."Id" = '{{_userId}}'::uuid AND NEW."EmailConfirmed" AND NOT OLD."EmailConfirmed" THEN
                        RAISE EXCEPTION 'Synthetic account confirmation storage failure';
                    END IF;
                    RETURN NEW;
                END;
                $guard$;
                CREATE TRIGGER "{{function}}" BEFORE UPDATE ON "AspNetUsers"
                    FOR EACH ROW EXECUTE FUNCTION "{{function}}"();
                """);
#pragma warning restore EF1002
        }
        using var csrf = await _client!.GetAsync("/api/session/csrf");
        using var json = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var csrfToken = json.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(v => v.Split(';', 2)[0])
            .First(v => v.Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        const string password = "Synthetic.Atomic-Activation-42!";
        HttpStatusCode? failedStatus = null;
        try
        {
            try
            {
                using var response = await SendAnonymousEndpointAsync(_client, "/api/session/password/reset",
                    JsonContent.Create(new { email, token = resetToken, newPassword = password }), csrfToken, cookie);
                failedStatus = response.StatusCode;
            }
            catch (DbUpdateException) { failedStatus = HttpStatusCode.InternalServerError; }

            await using var scope = _factory!.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == _userId);
            Assert.True(originalHash == user.PasswordHash, "Failed activation must preserve the original password hash.");
            Assert.True(originalStamp == user.SecurityStamp, "Failed activation must preserve the original token/security stamp.");
            Assert.False(user.EmailConfirmed);
            Assert.Null(await db.UserSessions.Where(s => s.Id == sessionId).Select(s => s.RevokedAt).SingleAsync());
            Assert.False(await db.AuditEntries.AnyAsync(a => a.TargetId == _userId && a.Action == "PasswordReset"));
            Assert.False(await db.PlatformAuditEntries.AnyAsync(a => a.TargetId == _userId && a.Action == "PasswordReset"));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failedStatus);
        }
        finally
        {
            await using var scope = _factory!.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
#pragma warning disable EF1002 // Same fixture-Guid-only DDL identifiers as creation above.
            await db.Database.ExecuteSqlRawAsync($"DROP TRIGGER IF EXISTS \"{function}\" ON \"AspNetUsers\"; DROP FUNCTION IF EXISTS \"{function}\"();");
#pragma warning restore EF1002
        }

        using (var retried = await SendAnonymousEndpointAsync(_client, "/api/session/password/reset",
            JsonContent.Create(new { email, token = resetToken, newPassword = password }), csrfToken, cookie))
            Assert.Equal(HttpStatusCode.OK, retried.StatusCode);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var user = (await users.FindByIdAsync(_userId.ToString()))!;
            Assert.True(user.EmailConfirmed); Assert.True(await users.CheckPasswordAsync(user, password));
            Assert.NotNull(await db.UserSessions.Where(s => s.Id == sessionId).Select(s => s.RevokedAt).SingleAsync());
            Assert.Equal(1, await db.AuditEntries.CountAsync(a => a.CompanyId == _companyId && a.TargetId == _userId && a.Action == "PasswordReset"));
            Assert.Equal(1, await db.PlatformAuditEntries.CountAsync(a => a.TargetId == _userId && a.Action == "PasswordReset"));
        }
        using var replay = await SendAnonymousEndpointAsync(_client, "/api/session/password/reset",
            JsonContent.Create(new { email, token = resetToken, newPassword = password }), csrfToken, cookie);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
    }
}
