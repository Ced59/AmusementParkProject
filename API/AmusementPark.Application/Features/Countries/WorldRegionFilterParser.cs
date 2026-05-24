namespace AmusementPark.Application.Features.Countries;

/// <summary>
/// Convertit les identifiants HTTP/UI en filtres région applicatifs.
/// </summary>
public static class WorldRegionFilterParser
{
    public static WorldRegionFilter? Parse(string? value)
    {
        string normalizedValue = (value ?? string.Empty)
            .Trim()
            .Replace("_", "-", StringComparison.Ordinal)
            .ToLowerInvariant();

        return normalizedValue switch
        {
            "europe" => WorldRegionFilter.Europe,
            "north-america" or "northamerica" or "america-north" => WorldRegionFilter.NorthAmerica,
            "south-america" or "southamerica" or "america-south" => WorldRegionFilter.SouthAmerica,
            "orient" or "asia" or "asia-pacific" or "middle-east" => WorldRegionFilter.Orient,
            "africa" => WorldRegionFilter.Africa,
            _ => null,
        };
    }
}
