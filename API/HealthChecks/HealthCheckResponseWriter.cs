using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace API.HealthChecks;

// Writes the health report as JSON: an overall status (Healthy / Degraded / Unhealthy) plus the
// status of each component (database, redis). Overall status is Degraded when a Degraded-severity
// check (Redis) fails but the DB is fine, and Unhealthy when the DB is down.
public static class HealthCheckResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            components = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description
                })
        };

        return context.Response.WriteAsJsonAsync(payload);
    }
}
