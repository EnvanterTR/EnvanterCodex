using System.Threading.RateLimiting;
using System.Globalization;
using Microsoft.AspNetCore.RateLimiting;

namespace EnvanterTR.Web.Data;

public static class ClientRateLimits
{
    // TrustedClientIp is the sole authority for forwarded client addresses.
    public static void Configure(RateLimiterOptions options)
    {
        options.AddPolicy("handover-email", context => RateLimitPartition.GetFixedWindowLimiter(
            $"{context.User.FindFirst("company_id")?.Value}:{context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10, Window = TimeSpan.FromMinutes(10), QueueLimit = 0, AutoReplenishment = true
            }));
    options.AddPolicy("login", context =>
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                clientIp,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        });
        options.AddPolicy("step-up", context =>
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                clientIp,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        });
        options.AddPolicy("customer-api", context =>
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
            });
        });
        options.AddPolicy("checkout", context =>
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0, AutoReplenishment = true
            });
        });
        options.AddPolicy("checkout-email", context =>
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0, AutoReplenishment = true
            });
        });
        options.OnRejected = async (context, cancellationToken) =>
        {
            var response = context.HttpContext.Response;
            response.StatusCode = StatusCodes.Status429TooManyRequests;
            response.Headers.CacheControl = "no-store";
            int? retryAfterSeconds = null;
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                retryAfterSeconds = (int)Math.Clamp(Math.Ceiling(retryAfter.TotalSeconds), 1, int.MaxValue);
                response.Headers.RetryAfter = retryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture);
            }
            var message = retryAfterSeconds is int seconds
                ? $"İstek sınırı aşıldı. {seconds} saniye sonra yeniden deneyin."
                : "İstek sınırı aşıldı. Bir süre bekleyip yeniden deneyin.";
            await response.WriteAsJsonAsync(new { message, retryAfterSeconds }, cancellationToken);
        };
    }
}
