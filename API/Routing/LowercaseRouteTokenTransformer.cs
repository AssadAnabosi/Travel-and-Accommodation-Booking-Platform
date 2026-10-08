namespace API.Routing;

// Lowercases route tokens like [controller]/[action] so routes are canonical lowercase
// (e.g. /api/auth instead of /api/Auth). This keeps generated URLs, Swagger paths, and the
// case-sensitive refresh-token cookie path (/api/auth) all consistent.
public class LowercaseRouteTokenTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value) => value?.ToString()?.ToLowerInvariant();
}
