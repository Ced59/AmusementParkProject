using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Images;

/// <summary>
/// Contrat HTTP de mise à jour des métadonnées d'image.
/// </summary>
public sealed class UpdateImageAssetRequest
{
    public string? Description { get; set; }

    public ImageGeoLocationDto? GeoLocation { get; set; }

    public List<LocalizedTextDto> AltTexts { get; set; } = new();

    public List<LocalizedTextDto> Captions { get; set; } = new();

    public List<LocalizedTextDto> Credits { get; set; } = new();

    public List<string> TagIds { get; set; } = new();

    public bool IsPublished { get; set; } = true;
}
