using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using RedisRateLimiting;
using StackExchange.Redis;

namespace API.RateLimiting;

public static class RateLimitingExtensions
{
    /// <summary>
    /// Registers the shared Redis <see cref="IConnectionMultiplexer"/> (also used by the health check)
    /// and Redis-backed rate limiting: a global sliding window (per user, else per client IP) chained
    /// with a stricter fixed window on the auth endpoints, wrapped so it fails OPEN if Redis is down.
    /// </summary>
    public static IServiceCollection AddRedisRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(configuration["Redis:ConnectionString"] ?? "localhost:6379");
            options.AbortOnConnectFail = false; // don't crash startup if Redis is briefly unavailable
            options.ConnectTimeout = 1000;       // fail fast when Redis is down (ms) rather than hang requests
            options.SyncTimeout = 1000;
            options.ConnectRetry = 1;
            return ConnectionMultiplexer.Connect(options);
        });

        var globalPermit = configuration.GetValue("RateLimiting:Global:PermitLimit", 100);
        var globalWindow = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:Global:WindowSeconds", 60));
        var authPermit = configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);
        var authWindow = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:Auth:WindowSeconds", 60));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers["Retry-After"] = ((int)retryAfter.TotalSeconds).ToString();
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ProblemDetails { Title = "Too many requests. Please slow down.", Status = StatusCodes.Status429TooManyRequests },
                    options: null, contentType: "application/problem+json", cancellationToken: token);
            };

            // Global: per authenticated user when present, else per client IP (real IP via forwarded headers).
            var global = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var mux = httpContext.RequestServices.GetRequiredService<IConnectionMultiplexer>();
                var key = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) is { Length: > 0 } uid
                    ? $"u:{uid}"
                    : $"ip:{httpContext.Connection.RemoteIpAddress}";
                return RedisRateLimitPartition.GetSlidingWindowRateLimiter(key, _ => new RedisSlidingWindowRateLimiterOptions
                {
                    ConnectionMultiplexerFactory = () => mux,
                    PermitLimit = globalPermit,
                    Window = globalWindow
                });
            });

            // Stricter limit on the auth endpoints only (brute-force protection), keyed by client IP.
            var auth = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                if (!httpContext.Request.Path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase))
                    return RateLimitPartition.GetNoLimiter("non-auth");

                var mux = httpContext.RequestServices.GetRequiredService<IConnectionMultiplexer>();
                return RedisRateLimitPartition.GetFixedWindowRateLimiter(
                    $"auth:{httpContext.Connection.RemoteIpAddress}", _ => new RedisFixedWindowRateLimiterOptions
                    {
                        ConnectionMultiplexerFactory = () => mux,
                        PermitLimit = authPermit,
                        Window = authWindow
                    });
            });

            // Chain global + auth and fail OPEN: a Redis outage degrades to "unthrottled but up".
            options.GlobalLimiter = new FailOpenRateLimiter(PartitionedRateLimiter.CreateChained(global, auth));
        });

        return services;
    }
}
