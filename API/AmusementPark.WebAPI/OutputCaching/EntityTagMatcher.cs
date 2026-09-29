using Microsoft.Extensions.Primitives;

namespace AmusementPark.WebAPI.OutputCaching;

public static class EntityTagMatcher
{
    public static bool Matches(StringValues candidates, string expected)
    {
        return candidates
            .SelectMany(static value => value?.Split(',', StringSplitOptions.TrimEntries)
                ?? Array.Empty<string>())
            .Any(value => Matches(value, expected));
    }

    private static bool Matches(string candidate, string expected)
    {
        if (string.Equals(candidate, "*", StringComparison.Ordinal))
        {
            return true;
        }

        string normalized = candidate.StartsWith("W/", StringComparison.Ordinal)
            ? candidate[2..].TrimStart()
            : candidate;
        return string.Equals(normalized, expected, StringComparison.Ordinal);
    }
}
