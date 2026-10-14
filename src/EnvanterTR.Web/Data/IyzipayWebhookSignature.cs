using System.Security.Cryptography;
using System.Text;

namespace EnvanterTR.Web.Data;

/// <summary>Verifies iyzico Checkout Form V3 webhook signatures without exposing provider secrets.</summary>
public static class IyzipayWebhookSignature
{
    public static bool Verify(string secretKey, string eventType, string paymentId, string token,
        string paymentConversationId, string status, string signature)
    {
        if (string.IsNullOrWhiteSpace(secretKey) || signature.Length != 64) return false;
        byte[] supplied;
        try { supplied = Convert.FromHexString(signature); }
        catch (FormatException) { return false; }

        var key = Encoding.UTF8.GetBytes(secretKey);
        byte[]? expected = null;
        try
        {
            using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, key);
            Append(hmac, key);
            Append(hmac, eventType);
            Append(hmac, paymentId);
            Append(hmac, token);
            Append(hmac, paymentConversationId);
            Append(hmac, status);
            expected = hmac.GetHashAndReset();
            return CryptographicOperations.FixedTimeEquals(expected, supplied);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(supplied);
            if (expected is not null) CryptographicOperations.ZeroMemory(expected);
        }
    }

    private static void Append(IncrementalHash hmac, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        try { hmac.AppendData(bytes); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    private static void Append(IncrementalHash hmac, byte[] value) => hmac.AppendData(value);
}
