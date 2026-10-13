using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class IyzipayCheckoutClientTests
{
    [Fact]
    public async Task Checkout_requests_use_the_official_server_side_signature_and_preserve_the_signed_body()
    {
        var handler = new CaptureHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"status\":\"success\",\"token\":\"sandbox-token\"}", Encoding.UTF8, "application/json")
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://sandbox-api.iyzipay.com") };
        var client = new IyzipayCheckoutClient(http, Options.Create(new IyzipayOptions
        {
            ApiKey = "sandbox-api-key", SecretKey = "synthetic-acceptance-secret"
        }));

        using var response = await client.InitializeAsync(new { conversationId = "synthetic-correlation", price = "100.00" });

        Assert.Equal("sandbox-token", response.RootElement.GetProperty("token").GetString());
        Assert.NotNull(handler.Request);
        Assert.Equal("/payment/iyzipos/checkoutform/initialize/auth/ecom", handler.Request!.RequestUri!.AbsolutePath);
        Assert.Equal("application/json", handler.Request.Content!.Headers.ContentType!.MediaType);
        Assert.True(handler.Request.Headers.TryGetValues("x-iyzi-rnd", out var randomValues));
        var randomKey = Assert.Single(randomValues!);
        Assert.Matches("^[0-9]{16,}$", randomKey);
        Assert.True(handler.Request.Headers.TryGetValues("Authorization", out var authorizationValues));
        var authorization = Assert.Single(authorizationValues!);
        Assert.StartsWith("IYZWSv2 ", authorization, StringComparison.Ordinal);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authorization["IYZWSv2 ".Length..]));
        var body = await handler.Request.Content.ReadAsStringAsync();
        var path = handler.Request.RequestUri.AbsolutePath;
        var signature = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes("synthetic-acceptance-secret"),
            Encoding.UTF8.GetBytes(randomKey + path + body)));
        Assert.Equal($"apiKey:sandbox-api-key&randomKey:{randomKey}&signature:{signature}", decoded);
        Assert.DoesNotContain("synthetic-acceptance-secret", authorization, StringComparison.Ordinal);

        using var retrieved = await client.RetrieveAsync(new { conversationId = "synthetic-correlation", token = "sandbox-token" });
        Assert.Equal("success", retrieved.RootElement.GetProperty("status").GetString());
        Assert.NotNull(handler.Request);
        var retrievePath = "/payment/iyzipos/checkoutform/auth/ecom/detail";
        Assert.Equal(retrievePath, handler.Request!.RequestUri!.AbsolutePath);
        var retrieveRandom = Assert.Single(handler.Request.Headers.GetValues("x-iyzi-rnd"));
        var retrieveAuthorization = Assert.Single(handler.Request.Headers.GetValues("Authorization"));
        var retrieveBody = await handler.Request.Content!.ReadAsStringAsync();
        var retrieveSignature = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes("synthetic-acceptance-secret"),
            Encoding.UTF8.GetBytes(retrieveRandom + retrievePath + retrieveBody)));
        var retrieveDecoded = Encoding.UTF8.GetString(Convert.FromBase64String(retrieveAuthorization["IYZWSv2 ".Length..]));
        Assert.Equal($"apiKey:sandbox-api-key&randomKey:{retrieveRandom}&signature:{retrieveSignature}", retrieveDecoded);
    }

    [Fact]
    public async Task Checkout_client_fails_closed_without_credentials_or_with_an_untrusted_api_host()
    {
        var noCall = new CaptureHandler(_ => throw new InvalidOperationException("The provider must not be called."));
        using var missingCredentialsHttp = new HttpClient(noCall) { BaseAddress = new Uri("https://sandbox-api.iyzipay.com") };
        var unconfigured = new IyzipayCheckoutClient(missingCredentialsHttp, Options.Create(new IyzipayOptions()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => unconfigured.InitializeAsync(new { price = "1.00" }));
        Assert.Null(noCall.Request);

        using var untrustedHostHttp = new HttpClient(noCall) { BaseAddress = new Uri("https://attacker.example") };
        var trustedOnly = new IyzipayCheckoutClient(untrustedHostHttp, Options.Create(new IyzipayOptions
        {
            ApiKey = "synthetic-key", SecretKey = "synthetic-secret"
        }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => trustedOnly.RetrieveAsync(new { token = "fake" }));
        Assert.Null(noCall.Request);
    }

    [Fact]
    public async Task Checkout_provider_failure_is_not_returned_as_a_success_document()
    {
        var handler = new CaptureHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"status\":\"failure\",\"errorCode\":\"synthetic\",\"errorMessage\":\"declined\"}", Encoding.UTF8, "application/json")
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://sandbox-api.iyzipay.com") };
        var client = new IyzipayCheckoutClient(http, Options.Create(new IyzipayOptions
        {
            ApiKey = "synthetic-key", SecretKey = "synthetic-secret"
        }));

        var exception = await Assert.ThrowsAsync<IyzipayApiException>(() => client.RetrieveAsync(new { token = "synthetic-token" }));
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal("synthetic", exception.ErrorCode);
        Assert.DoesNotContain("declined", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CaptureHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = request.Content is null ? null : new StringContent(await request.Content.ReadAsStringAsync(cancellationToken),
                    Encoding.UTF8, request.Content.Headers.ContentType?.MediaType)
            };
            foreach (var header in request.Headers)
                Request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            return respond(request);
        }
    }
}
