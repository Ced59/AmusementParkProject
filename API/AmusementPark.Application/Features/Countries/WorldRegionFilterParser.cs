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
            "asia" => WorldRegionFilter.Asia,
            "middle-east" or "middleeast" => WorldRegionFilter.MiddleEast,
            "oceania" or "australia" or "pacific" => WorldRegionFilter.Oceania,
            "orient" or "asia-pacific" => WorldRegionFilter.Orient,
            "africa" => WorldRegionFilter.Africa,
            _ => null,
        };
    }
}
