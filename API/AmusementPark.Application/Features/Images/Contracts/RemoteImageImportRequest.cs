using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Contracts;

public sealed class RemoteImageImportRequest
{
    public string SourceUrl { get; init; } = string.Empty;

    public ImageCategory Category { get; init; }

    public ImageOwnerType OwnerType { get; init; } = ImageOwnerType.None;

    public string? OwnerId { get; init; }

    public string? Description { get; init; }

    public bool WithWatermark { get; init; }

    public bool SetAsCurrent { get; init; }
}
