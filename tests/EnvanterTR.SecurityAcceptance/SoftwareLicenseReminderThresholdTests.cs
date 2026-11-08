using EnvanterTR.Web.Data;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class SoftwareLicenseReminderThresholdTests
{
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    [InlineData(15, true)]
    [InlineData(16, false)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public void License_reminder_is_due_only_at_the_configured_day_thresholds(int daysUntilExpiration, bool expected)
    {
        var today = new DateOnly(2026, 11, 4);

        Assert.Equal(expected, OperationalNotificationEmailService.IsLicenseReminderDay(today, today.AddDays(daysUntilExpiration)));
    }
}
