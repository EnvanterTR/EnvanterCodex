using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Software_license_key_plaintext_never_enters_host_logs()
    {
        var secret = $"synthetic-license-log-sentinel-{Guid.NewGuid():N}";
        var captureMarker = $"license-log-capture-active-{Guid.NewGuid():N}";
        var provider = new CapturingLoggerProvider();
        using var loggingFactory = _factory!.WithWebHostBuilder(builder => builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddProvider(provider);
        }));
        using var client = loggingFactory.CreateClient();

        loggingFactory.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("LicenseKeyLogAcceptance")
            .LogWarning("Capture sink active {CaptureMarker}", captureMarker);
        Assert.Contains(provider.Messages, message => message.Contains(captureMarker, StringComparison.Ordinal));
        provider.Clear();

        using var csrfRequest = new HttpRequestMessage(HttpMethod.Get, "/api/session/csrf");
        AddAcceptanceHeaders(csrfRequest, "admin");
        using var csrfResponse = await client.SendAsync(csrfRequest);
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var token = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        async Task<HttpResponseMessage> Send(HttpMethod method, string path, object? body = null)
        {
            var request = new HttpRequestMessage(method, path);
            AddAcceptanceHeaders(request, "admin");
            request.Headers.Add("X-CSRF-TOKEN", token);
            request.Headers.Add("Cookie", cookie);
            if (body is not null) request.Content = JsonContent.Create(body);
            return await client.SendAsync(request);
        }

        var keyPath = $"/api/software-licenses/{_ownLicenseId}/key";
        using (var set = await Send(HttpMethod.Put, keyPath, new { key = secret }))
            Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);
        using (var reveal = await Send(HttpMethod.Post, keyPath + "/reveal", new { }))
        {
            Assert.Equal(HttpStatusCode.OK, reveal.StatusCode);
            using var payload = JsonDocument.Parse(await reveal.Content.ReadAsStringAsync());
            Assert.Equal(secret, payload.RootElement.GetProperty("key").GetString());
        }

        var capturedLogs = string.Join(Environment.NewLine, provider.Messages);
        Assert.DoesNotContain(secret, capturedLogs, StringComparison.Ordinal);
    }

    private void AddAcceptanceHeaders(HttpRequestMessage request, string role)
    {
        request.Headers.Add("X-Acceptance-User", _userId.ToString());
        request.Headers.Add("X-Acceptance-Company", _companyId.ToString());
        request.Headers.Add("X-Acceptance-Role", role);
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _messages = new();
        public IReadOnlyCollection<string> Messages => _messages.ToArray();

        public void Clear()
        {
            while (_messages.TryDequeue(out _)) { }
        }

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _messages);
        public void Dispose() { }

        private sealed class CapturingLogger(string categoryName, ConcurrentQueue<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                var rendered = formatter(state, exception);
                var structured = state is IEnumerable<KeyValuePair<string, object?>> values
                    ? string.Join(" ", values.Select(value => $"{value.Key}={Convert.ToString(value.Value, CultureInfo.InvariantCulture)}"))
                    : Convert.ToString(state, CultureInfo.InvariantCulture) ?? string.Empty;
                messages.Enqueue($"{categoryName} {logLevel} {rendered} {structured} {exception}");
            }
        }
    }
}
