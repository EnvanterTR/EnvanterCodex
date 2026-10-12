using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class AssetCustomFieldProtectionTests
{
    [Fact]
    public void Sensitive_values_are_encrypted_tenant_bound_hidden_and_stable_on_unchanged_updates()
    {
        var protection = new AssetCustomFieldProtection(new EphemeralDataProtectionProvider());
        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        var field = new AssetCustomFieldDefinition
        {
            Id = Guid.NewGuid(), CompanyId = companyId, Key = "recovery_code",
            Name = "Recovery code", FieldType = "sensitive", IsActive = true
        };
        var fields = new[] { field };
        const string secret = "sensitive-value-7ab91";
        var plaintextJson = JsonSerializer.Serialize(new Dictionary<string, string> { [field.Key] = secret });

        var protectedResult = protection.Protect(plaintextJson, null, companyId, fields);
        Assert.Null(protectedResult.Error);
        Assert.NotNull(protectedResult.Json);
        Assert.DoesNotContain(secret, protectedResult.Json, StringComparison.Ordinal);
        using (var stored = JsonDocument.Parse(protectedResult.Json!))
        {
            var ciphertext = stored.RootElement.GetProperty(field.Key).GetString();
            Assert.StartsWith("enc:v1:", ciphertext, StringComparison.Ordinal);
        }

        var adminValues = protection.Read(protectedResult.Json, fields, companyId, revealSensitive: true);
        Assert.Equal(secret, adminValues[field.Key]);
        Assert.Empty(protection.Read(protectedResult.Json, fields, companyId, revealSensitive: false));

        var unchanged = protection.Protect(plaintextJson, protectedResult.Json, companyId, fields);
        Assert.Equal(protectedResult.Json, unchanged.Json);
        Assert.Null(unchanged.Error);

        Assert.Throws<CryptographicException>(() => protection.Read(protectedResult.Json, fields,
            otherCompanyId, revealSensitive: true));
        Assert.False(AssetCustomFieldEndpoints.CanReadSensitive(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("company_role", "viewer")], "test"))));
        Assert.True(AssetCustomFieldEndpoints.CanReadSensitive(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("company_role", "admin")], "test"))));
    }

    [Fact]
    public void Sensitive_plaintext_in_storage_fails_closed()
    {
        var protection = new AssetCustomFieldProtection(new EphemeralDataProtectionProvider());
        var companyId = Guid.NewGuid();
        var field = new AssetCustomFieldDefinition
        {
            Id = Guid.NewGuid(), CompanyId = companyId, Key = "secret", Name = "Secret", FieldType = "sensitive"
        };

        Assert.Throws<CryptographicException>(() => protection.Read(
            "{\"secret\":\"legacy-plaintext\"}", [field], companyId, revealSensitive: true));
    }
}
