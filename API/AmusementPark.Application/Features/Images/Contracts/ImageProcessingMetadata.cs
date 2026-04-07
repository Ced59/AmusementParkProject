using AmusementPark.Application.Common.Contracts;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Contracts;

/// <summary>
/// Métadonnées extraites depuis un binaire image.
/// </summary>
public sealed class ImageProcessingMetadata
{
    public int Width { get; init; }

    public int Height { get; init; }

    public long SizeInBytes { get; init; }

    public GeoPointValue? GeoLocation { get; init; }

    public ImageExifMetadata? ExifMetadata { get; init; }
}
