using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace EnvanterTR.Web.Data;

public sealed class IyzipayOptions
{
    public string ApiKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://sandbox-api.iyzipay.com";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(SecretKey);
}

public sealed class IyzipayApiException(HttpStatusCode statusCode, string? errorCode, string? errorMessage)
    : Exception("iyzico isteği başarısız oldu." )
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? ErrorCode { get; } = errorCode;
    public string? ProviderMessage { get; } = errorMessage;
}

/// <summary>
/// Server-side Checkout Form transport. Never send API credentials or signatures to a browser.
/// Payment success must still be confirmed with the provider's retrieve endpoint.
/// </summary>
public sealed class IyzipayCheckoutClient(HttpClient httpClient, IOptions<IyzipayOptions> configuredOptions)
{
    private const int MaxResponseBytes = 1_048_576;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> AllowedPaths = new(StringComparer.Ordinal)
    {
        "/payment/iyzipos/checkoutform/initialize/auth/ecom",
        "/payment/iyzipos/checkoutform/auth/ecom/detail"
    };
    private IyzipayOptions Options => configuredOptions.Value;

    public async Task<JsonDocument> InitializeAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        => await PostAsync("/payment/iyzipos/checkoutform/initialize/auth/ecom", request, cancellationToken);

    public async Task<JsonDocument> RetrieveAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        => await PostAsync("/payment/iyzipos/checkoutform/auth/ecom/detail", request, cancellationToken);

    private async Task<JsonDocument> PostAsync<TRequest>(string path, TRequest request, CancellationToken cancellationToken)
    {
        var options = Options;
        if (!options.IsConfigured)
            throw new InvalidOperationException("iyzico API anahtarları yapılandırılmamış.");
        if (!AllowedPaths.Contains(path))
            throw new InvalidOperationException("iyzico isteği izin verilen Checkout Form uçlarından biri değil.");

        var baseUri = httpClient.BaseAddress ?? new Uri(options.BaseUrl, UriKind.Absolute);
        if (baseUri.Scheme != Uri.UriSchemeHttps || baseUri.UserInfo.Length != 0 ||
            baseUri.Host is not ("api.iyzipay.com" or "sandbox-api.iyzipay.com") || baseUri.AbsolutePath != "/")
            throw new InvalidOperationException("iyzico adresi yalnızca resmi HTTPS API uç noktası olabilir.");

        var body = JsonSerializer.Serialize(request, JsonOptions);
        var randomKey = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{RandomNumberGenerator.GetInt32(100000, 1_000_000)}";
        var signaturePayload = Encoding.UTF8.GetBytes(randomKey + path + body);
        var secret = Encoding.UTF8.GetBytes(options.SecretKey);
        string signature;
        try
        {
            signature = Convert.ToHexStringLower(HMACSHA256.HashData(secret, signaturePayload));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
            CryptographicOperations.ZeroMemory(signaturePayload);
        }

        var authorizationValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"apiKey:{options.ApiKey}&randomKey:{randomKey}&signature:{signature}"));
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, path))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        message.Headers.TryAddWithoutValidation("Authorization", "IYZWSv2 " + authorizationValue);
        message.Headers.TryAddWithoutValidation("x-iyzi-rnd", randomKey);
        using var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var responseBytes = await ReadLimitedAsync(response.Content, MaxResponseBytes, cancellationToken);
        try
        {
            using var parsed = JsonDocument.Parse(responseBytes);
            if (!response.IsSuccessStatusCode ||
                (parsed.RootElement.TryGetProperty("status", out var status) &&
                 string.Equals(status.GetString(), "failure", StringComparison.OrdinalIgnoreCase)))
            {
                var errorCode = parsed.RootElement.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
                var errorMessage = parsed.RootElement.TryGetProperty("errorMessage", out var providerMessage) ? providerMessage.GetString() : null;
                throw new IyzipayApiException(response.StatusCode, errorCode, errorMessage);
            }

            return JsonDocument.Parse(responseBytes.AsMemory().ToArray());
        }
        catch (JsonException) when (!response.IsSuccessStatusCode)
        {
            throw new IyzipayApiException(response.StatusCode, null, null);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(responseBytes);
        }
    }

    private static async Task<byte[]> ReadLimitedAsync(HttpContent content, int limit, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is long contentLength && contentLength > limit)
            throw new InvalidDataException("iyzico yanıtı izin verilen boyutu aşıyor.");
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        while (true)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0) break;
            if (buffer.Length + read > limit)
                throw new InvalidDataException("iyzico yanıtı izin verilen boyutu aşıyor.");
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }
}
