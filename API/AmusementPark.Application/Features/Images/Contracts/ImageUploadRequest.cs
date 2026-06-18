using AmusementPark.Application.Common.Contracts;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Contracts;

/// <summary>
/// Données d'entrée applicatives pour l'upload d'une image.
/// </summary>
public sealed class ImageUploadRequest
{
    public string? ImageId { get; init; }

    public ImageCategory Category { get; init; }

    public FilePayload File { get; init; } = new();

    public string? Description { get; init; }

    public bool WithWatermark { get; init; } = true;

    public ImageOwnerType OwnerType { get; init; } = ImageOwnerType.None;

    public string? OwnerId { get; init; }

    public string? StoragePath { get; init; }

    public string? SourceUrl { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public long SizeInBytes { get; init; }

    public GeoPointValue? GeoLocation { get; init; }

    public ImageExifMetadata? ExifMetadata { get; init; }
}
