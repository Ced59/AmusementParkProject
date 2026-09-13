using System.Text.Json;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;

internal sealed class RawParkMapMarker
{
    public JsonElement Id { get; init; }

    public string? Name { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public int? Nb { get; init; }
}
