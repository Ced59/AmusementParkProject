using AmusementPark.Application.Common.Contracts;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Contracts;

/// <summary>
/// Métadonnées applicatives modifiables d'une image.
/// </summary>
public sealed class ImageMetadataUpdate
{
    public string? Description { get; init; }

    public GeoPointValue? GeoLocation { get; init; }

    public IReadOnlyCollection<LocalizedTextValue> AltTexts { get; init; } = Array.Empty<LocalizedTextValue>();

    public IReadOnlyCollection<LocalizedTextValue> Captions { get; init; } = Array.Empty<LocalizedTextValue>();

    public IReadOnlyCollection<LocalizedTextValue> Credits { get; init; } = Array.Empty<LocalizedTextValue>();

    public IReadOnlyCollection<string> TagIds { get; init; } = Array.Empty<string>();

    public ImageCategory Category { get; init; }

    public ImageOwnerType? OwnerType { get; init; }

    public string? OwnerId { get; init; }

    public bool? IsCurrent { get; init; }

    public bool IsPublished { get; init; } = true;

    public string? SourceUrl { get; init; }
}
