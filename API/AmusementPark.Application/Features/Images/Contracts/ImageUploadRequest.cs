using AmusementPark.Application.Common.Contracts;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Contracts;

/// <summary>
/// Données d'entrée applicatives pour l'upload d'une image.
/// </summary>
public sealed class ImageUploadRequest
{
    public ImageCategory Category { get; init; }

    public FilePayload File { get; init; } = new();

    public string? Description { get; init; }

    public bool WithWatermark { get; init; } = true;

    public ImageOwnerType OwnerType { get; init; } = ImageOwnerType.None;

    public string? OwnerId { get; init; }
}
