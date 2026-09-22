using System.Threading.RateLimiting;

namespace API.RateLimiting;

public sealed class FailOpenRateLimiter(PartitionedRateLimiter<HttpContext> inner)
    : PartitionedRateLimiter<HttpContext>
{
    public override RateLimiterStatistics? GetStatistics(HttpContext resource)
    {
        try
        {
            return inner.GetStatistics(resource);
        }
        catch
        {
            return null;
        }
    }

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(
        HttpContext resource, int permitCount, CancellationToken cancellationToken)
    {
        try
        {
            return await inner.AcquireAsync(resource, permitCount, cancellationToken);
        }
        catch (Exception ex)
        {
            return FailOpen(resource, ex);
        }
    }

    protected override RateLimitLease AttemptAcquireCore(HttpContext resource, int permitCount)
    {
        try
        {
            return inner.AttemptAcquire(resource, permitCount);
        }
        catch (Exception ex)
        {
            return FailOpen(resource, ex);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) inner.Dispose();
    }

    protected override ValueTask DisposeAsyncCore() => inner.DisposeAsync();

    private static RateLimitLease FailOpen(HttpContext resource, Exception ex)
    {
        resource.RequestServices.GetService<ILoggerFactory>()
            ?.CreateLogger("RateLimiting")
            .LogWarning(ex, "Rate limiter backend unavailable — failing open (request allowed).");
        return FailOpenLease.Instance;
    }
}

internal sealed class FailOpenLease : RateLimitLease
{
    public static readonly FailOpenLease Instance = new();
    public override bool IsAcquired => true;
    public override IEnumerable<string> MetadataNames => [];

    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        metadata = null;
        return false;
    }
}