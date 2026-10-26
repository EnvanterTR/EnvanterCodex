using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Trial_welcome_failure_rolls_back_registration_and_parallel_retry_sends_once(bool existingOwner)
    {
        var plan = await SeedTrialPlan();
        await using var factory = EnabledTrialFactory();
        using var client = factory.CreateClient();
        await ConfigureTrialCsrf(client);
        var email = $"trial-welcome-{Guid.NewGuid():N}@example.invalid";
        string? beforeHash; string? beforeStamp; int beforeCompanies; int beforeMemberships;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.Id == _userId);
            if (existingOwner) email = user.Email!;
            beforeHash = user.PasswordHash; beforeStamp = user.SecurityStamp;
            beforeCompanies = await db.Companies.CountAsync(); beforeMemberships = await db.CompanyMemberships.CountAsync();
        }
        var slug = "welcome-" + Guid.NewGuid().ToString("N");
        using var requested = await client.PostAsJsonAsync("/api/trials/request", new RequestCloudTrial(
            "Welcome trial company", slug, "Trial welcome owner", email, plan.Id, true, CloudTrialEndpoints.NoticeVersion));
        Assert.Equal(HttpStatusCode.Accepted, requested.StatusCode);
        var token = LastTrialToken();
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var oldPort = configuration["Email:Smtp:Port"];
        var unavailable = new TcpListener(IPAddress.Loopback, 0); unavailable.Start();
        var failedPort = ((IPEndPoint)unavailable.LocalEndpoint).Port; unavailable.Stop();
        var beforeMessages = _smtpCapture.Messages.Count;
        try
        {
            configuration["Email:Smtp:Port"] = failedPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
            using var failed = await client.PostAsJsonAsync("/api/trials/confirm", new ConfirmCloudTrial(token));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.Companies.AnyAsync(c => c.Slug == slug));
            Assert.Equal(beforeCompanies, await db.Companies.CountAsync());
            Assert.Equal(beforeMemberships, await db.CompanyMemberships.CountAsync());
            if (!existingOwner) Assert.False(await db.Users.AnyAsync(u => u.Email == email));
            var owner = await db.Users.SingleAsync(u => u.Id == _userId);
            Assert.Equal(beforeHash, owner.PasswordHash); Assert.Equal(beforeStamp, owner.SecurityStamp);
            Assert.Equal(beforeMessages, _smtpCapture.Messages.Count);
        }
        finally { configuration["Email:Smtp:Port"] = oldPort; }
        var retried = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.PostAsJsonAsync("/api/trials/confirm", new ConfirmCloudTrial(token))));
        foreach (var response in retried)
        {
            using (response)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Assert.Equal($"https://{slug}.envantertr.com/", payload.RootElement.GetProperty("portalUrl").GetString());
            }
        }
        Assert.Equal(beforeMessages + 1, _smtpCapture.Messages.Count);
        Assert.Contains($"https://{slug}.envantertr.com/", DecodeMimeBody(_smtpCapture.Messages.Last()));
        await using var final = factory.Services.CreateAsyncScope();
        var verify = final.ServiceProvider.GetRequiredService<AppDbContext>();
        var company = await verify.Companies.SingleAsync(c => c.Slug == slug);
        var subscription = await verify.TenantSubscriptions.SingleAsync(s => s.CompanyId == company.Id);
        Assert.Equal("Trial", subscription.Status);
        Assert.Equal(TimeSpan.FromDays(15), subscription.EndsAt - subscription.StartsAt);
        Assert.Equal(1, await verify.CompanyMemberships.CountAsync(m => m.CompanyId == company.Id && m.RoleKey == "owner"));
        Assert.Equal(1, await verify.AuditEntries.CountAsync(a => a.CompanyId == company.Id && a.Action == "CloudTrialStarted"));
        Assert.Equal(beforeCompanies + 1, await verify.Companies.CountAsync());
        var saved = await verify.Users.SingleAsync(u => u.Id == _userId);
        Assert.Equal(beforeHash, saved.PasswordHash); Assert.Equal(beforeStamp, saved.SecurityStamp);
    }
}
