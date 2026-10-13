using EnvanterTR.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class LocalPlatformAdminRecoveryTests
{
    [Fact]
    public async Task Local_recovery_is_denied_outside_development_before_reading_secrets_or_database()
    {
        using var services = new ServiceCollection().BuildServiceProvider();

        var result = await LocalPlatformAdminRecovery.RunAsync(services, isDevelopment: false,
            "Host=database;Database=envantertr;Username=test");

        Assert.Equal(2, result);
    }

    [Theory]
    [InlineData("Host=remote.example;Database=envantertr;Username=test")]
    [InlineData("Host=database;Database=another_database;Username=test")]
    public async Task Local_recovery_rejects_nonlocal_or_wrong_database_targets(string connectionString)
    {
        using var services = new ServiceCollection().BuildServiceProvider();

        var result = await LocalPlatformAdminRecovery.RunAsync(services, isDevelopment: true, connectionString);

        Assert.Equal(2, result);
    }
}
