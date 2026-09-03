using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AmusementPark.WebAPI.Extensions;

internal static class HttpRequestPublicPathExtensions
{
    internal const string ForwardedPrefixHeaderName = "X-Forwarded-Prefix";

    public static string GetPublicPathPrefix(this HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Headers.TryGetValue(ForwardedPrefixHeaderName, out StringValues values))
        {
            foreach (string? rawValue in values)
            {
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    continue;
                }

                string[] candidates = rawValue.Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (string candidate in candidates)
                {
                    string? normalizedPrefix = NormalizePublicPathPrefix(candidate);
                    if (normalizedPrefix is not null)
                    {
                        return normalizedPrefix;
                    }
                }
            }
        }

        if (!request.PathBase.HasValue)
        {
            return string.Empty;
        }

        return NormalizePublicPathPrefix(request.PathBase.Value ?? string.Empty) ?? string.Empty;
    }

    private static string? NormalizePublicPathPrefix(string value)
    {
        string trimmedValue = value.Trim().TrimEnd('/');
        if (trimmedValue.Length == 0 || string.Equals(trimmedValue, "/", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        if (!trimmedValue.StartsWith("/", StringComparison.Ordinal)
            || trimmedValue.StartsWith("//", StringComparison.Ordinal)
            || trimmedValue.Contains('\\', StringComparison.Ordinal)
            || trimmedValue.Contains(':', StringComparison.Ordinal)
            || trimmedValue.Contains('?', StringComparison.Ordinal)
            || trimmedValue.Contains('#', StringComparison.Ordinal))
        {
            return null;
        }

        return trimmedValue;
    }
}
