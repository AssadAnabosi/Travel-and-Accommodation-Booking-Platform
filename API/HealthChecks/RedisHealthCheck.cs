using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace API.HealthChecks;

// AI GENERATED SECTION HAHAHA - [if ur reading this] - hi, hello and how are you!
// Pings Redis via the shared multiplexer. Registered with FailureStatus = Degraded, because the API
// stays usable when Redis is down (rate limiting fails open) — a Redis outage is a degraded state,
// not a dead one.
public sealed class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy("Redis is reachable.");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Redis is unreachable.", ex);
        }
    }
}
