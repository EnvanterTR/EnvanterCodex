using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Theory]
    [InlineData("membership")]
    [InlineData("email")]
    [InlineData("confirmation")]
    [InlineData("deleted-company")]
    [InlineData("inactive-company")]
    [InlineData("support-mailbox")]
    public async Task Queued_support_email_rechecks_current_recipient_access_before_sending(string change)
    {
        var recipient = $"queued-{Guid.NewGuid():N}@example.invalid";
        var ticket = new SupportTicket
        {
            CompanyId = _companyId, CreatedByUserId = _sameCompanyOtherUserId,
            Subject = "Private company support subject", Category = "Access"
        };
        var queued = new SupportTicketEmailOutbox
        {
            CompanyId = _companyId, TicketId = ticket.Id, RecipientEmail = recipient,
            EventType = "TicketCreated", StatusSnapshot = "Open",
            IsSupportMailbox = change == "support-mailbox", NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await using var transaction = await db.Database.BeginTransactionAsync();
            var requester = await db.Users.SingleAsync(user => user.Id == _sameCompanyOtherUserId);
            requester.Email = recipient; requester.NormalizedEmail = recipient.ToUpperInvariant(); requester.EmailConfirmed = true;
            if (change == "membership")
                await db.CompanyMemberships.Where(member => member.CompanyId == _companyId && member.UserId == requester.Id).ExecuteDeleteAsync();
            if (change == "email") requester.Email = $"changed-{Guid.NewGuid():N}@example.invalid";
            if (change == "confirmation") requester.EmailConfirmed = false;
            if (change is "deleted-company" or "inactive-company")
            {
                var company = await db.Companies.SingleAsync(row => row.Id == _companyId);
                company.IsActive = false;
                if (change == "deleted-company") company.DeletedAt = DateTimeOffset.UtcNow;
            }
            db.SupportTickets.Add(ticket); db.SupportTicketEmailOutbox.Add(queued);
            await db.SaveChangesAsync(); await transaction.CommitAsync();
        }
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<SupportTicketEmailService>();
            Assert.Equal(0, await service.RunOnceAsync(CancellationToken.None));
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var held = await db.SupportTicketEmailOutbox.SingleAsync(row => row.Id == queued.Id);
            Assert.Equal("Pending", held.Status);
            Assert.Null(held.SentAt);
            Assert.Equal("SupportEmailRecipientNoLongerAuthorized", held.LastErrorType);
            Assert.True(held.NextAttemptAt > DateTimeOffset.UtcNow);
            Assert.Empty(_smtpCapture.Messages);
        }
        // Restoring the exact authorized destination makes the queued message deliverable.
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var requester = await db.Users.SingleAsync(user => user.Id == _sameCompanyOtherUserId);
            requester.Email = recipient; requester.EmailConfirmed = true;
            if (change == "membership") db.CompanyMemberships.Add(new CompanyMembership
            { CompanyId = _companyId, UserId = requester.Id, RoleKey = "viewer" });
            if (change is "deleted-company" or "inactive-company")
            {
                var company = await db.Companies.SingleAsync(row => row.Id == _companyId);
                company.DeletedAt = null; company.IsActive = true;
            }
            var held = await db.SupportTicketEmailOutbox.SingleAsync(row => row.Id == queued.Id);
            if (change == "support-mailbox") held.RecipientEmail = "support@example.invalid";
            held.NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }
        await FlushSupportTicketEmailsAsync(ticket.Id, expectedSentCount: 1);
        var delivered = Assert.Single(_smtpCapture.Messages);
        var expected = change == "support-mailbox" ? "support@example.invalid" : recipient;
        Assert.Contains($"To: {expected}", delivered, StringComparison.OrdinalIgnoreCase);
    }
}
