namespace AmusementPark.Application.Features.Parks.Contracts;

/// <summary>
/// Convertit les identifiants HTTP/UI en filtres de rayonnement applicatifs.
/// </summary>
public static class ParkAudienceClassificationFilterParser
{
    public static ParkAudienceClassificationFilter? Parse(string? value)
    {
        string normalizedValue = Normalize(value);

        return normalizedValue switch
        {
            "international" => ParkAudienceClassificationFilter.International,
            "national" => ParkAudienceClassificationFilter.National,
            "regional" => ParkAudienceClassificationFilter.Regional,
            "local" => ParkAudienceClassificationFilter.Local,
            "unspecified" or "missing" or "unset" or "notset" or "notspecified" or "unclassified" or "none" or "null" or "nonrenseigne" => ParkAudienceClassificationFilter.Unspecified,
            _ => null,
        };
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty)
            .Trim()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("é", "e", StringComparison.Ordinal)
            .Replace("è", "e", StringComparison.Ordinal)
            .Replace("ê", "e", StringComparison.Ordinal)
            .Replace("É", "e", StringComparison.Ordinal)
            .Replace("È", "e", StringComparison.Ordinal)
            .Replace("Ê", "e", StringComparison.Ordinal)
            .ToLowerInvariant();
    }
}
