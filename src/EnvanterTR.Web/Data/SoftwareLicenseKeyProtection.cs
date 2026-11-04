using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace EnvanterTR.Web.Data;

public sealed class SoftwareLicenseKeyProtection(IDataProtectionProvider provider)
{
    private const string Prefix = "enc:v1:";
    private const int MaxProtectedValueLength = 12000;

    public string Protect(Guid companyId, Guid licenseId, string key)
    {
        if (companyId == Guid.Empty || licenseId == Guid.Empty)
            throw new ArgumentException("Company and license identifiers are required.");
        if (string.IsNullOrWhiteSpace(key) || key.Length > 2048)
            throw new ArgumentException("The license key must contain 1 to 2,048 characters.", nameof(key));

        var protectedValue = Prefix + CreateProtector(companyId, licenseId).Protect(key.Trim());
        if (protectedValue.Length > MaxProtectedValueLength)
            throw new CryptographicException("The protected license key exceeds the storage limit.");
        return protectedValue;
    }

    public string Unprotect(Guid companyId, Guid licenseId, string? protectedKey)
    {
        if (string.IsNullOrEmpty(protectedKey) || !protectedKey.StartsWith(Prefix, StringComparison.Ordinal))
            throw new CryptographicException("The stored license key is not in the expected protected format.");

        try
        {
            return CreateProtector(companyId, licenseId).Unprotect(protectedKey[Prefix.Length..]);
        }
        catch (CryptographicException)
        {
            throw new CryptographicException("The license key could not be decrypted. Verify the Data Protection keys and backup coverage.");
        }
    }

    private IDataProtector CreateProtector(Guid companyId, Guid licenseId) =>
        provider.CreateProtector("EnvanterTR.SoftwareLicenseKey.v1", companyId.ToString("N"), licenseId.ToString("N"));
}
