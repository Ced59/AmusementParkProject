using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Parks;

public sealed class ParkMapItemDto
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    public required string Category { get; set; }

    public required string Type { get; set; }

    public string? Subtype { get; set; }

    public string? ZoneId { get; set; }

    public List<LocalizedTextDto> Descriptions { get; set; } = new();

    public ParkMapAttractionDetailsDto? AttractionDetails { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}
