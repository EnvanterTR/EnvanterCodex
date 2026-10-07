using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class TenantSessionAcceptanceTests : IAsyncLifetime
{
    private const string TestScheme = "SecurityAcceptance";
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _companyId = Guid.NewGuid();
    private string? _previousConnectionString;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("ENVANTERTR_SECURITY_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Set ENVANTERTR_SECURITY_TEST_CONNECTION to the disposable acceptance-test PostgreSQL database.");

        _previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__EnvanterTR");
        Environment.SetEnvironmentVariable("ConnectionStrings__EnvanterTR", connectionString);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:EnvanterTR"] = connectionString }));
            builder.ConfigureTestServices(services => services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestScheme;
                    options.DefaultChallengeScheme = TestScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, AcceptanceHeaderAuthenticationHandler>(TestScheme, _ => { }));
        });
        _client = _factory!.CreateClient();

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        db.Companies.Add(new Company { Id = _companyId, Name = "Security acceptance company", IsActive = true });
        db.Users.Add(new EnvanterUser
        {
            Id = _userId,
            UserName = $"acceptance-{_userId:N}",
            NormalizedUserName = $"ACCEPTANCE-{_userId:N}",
            Email = $"acceptance-{_userId:N}@example.invalid",
            NormalizedEmail = $"ACCEPTANCE-{_userId:N}@EXAMPLE.INVALID",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            DisplayName = "Security Acceptance User"
        });
        db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _companyId, UserId = _userId, RoleKey = "admin" });
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__EnvanterTR", _previousConnectionString);
    }

    [Fact]
    public async Task Role_downgrade_revocation_and_company_suspension_take_effect_on_the_next_request()
    {
        Assert.Equal(HttpStatusCode.OK, (await SendAsAdminAsync("/api/members")).StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _userId);
            membership.RoleKey = "viewer";
            await db.SaveChangesAsync();
        }

        // The request still presents an admin claim, as an old cookie would. The server
        // refreshes it from the database before authorization evaluates settings.manage.
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsAdminAsync("/api/members")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsAdminAsync("/api/assets?page=1&pageSize=20")).StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _userId);
            db.CompanyMemberships.Remove(membership);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsAdminAsync("/api/assets")).StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _companyId, UserId = _userId, RoleKey = "admin" });
            var company = await db.Companies.SingleAsync(x => x.Id == _companyId);
            company.IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsAdminAsync("/api/assets")).StatusCode);
    }

    private async Task<HttpResponseMessage> SendAsAdminAsync(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Acceptance-User", _userId.ToString());
        request.Headers.Add("X-Acceptance-Company", _companyId.ToString());
        request.Headers.Add("X-Acceptance-Role", "admin");
        return await _client!.SendAsync(request);
    }
}

internal sealed class AcceptanceHeaderAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Guid.TryParse(Request.Headers["X-Acceptance-User"], out var userId) ||
            !Guid.TryParse(Request.Headers["X-Acceptance-Company"], out var companyId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var role = Request.Headers["X-Acceptance-Role"].ToString();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, "Acceptance User"),
            new("company_id", companyId.ToString()),
            new("company_role", role)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
