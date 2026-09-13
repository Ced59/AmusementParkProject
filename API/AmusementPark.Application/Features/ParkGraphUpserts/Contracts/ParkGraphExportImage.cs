using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportImage
{
    public string ImageId { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public ImageOwnerType OwnerType { get; init; }

    public string? OwnerId { get; init; }

    public string? OwnerKey { get; init; }

    public ImageCategory Category { get; init; }

    public bool IsPublished { get; init; }

    public bool IsCurrent { get; init; }

    public bool SetAsCurrent { get; init; }

    public bool WithWatermark { get; init; }

    public bool IsWatermarked { get; init; }

    public string? SourceUrl { get; init; }

    public string? InternalUrl { get; init; }

    public string? Description { get; init; }

    public List<LocalizedText> AltTexts { get; init; } = new List<LocalizedText>();

    public List<LocalizedText> Captions { get; init; } = new List<LocalizedText>();

    public List<LocalizedText> Credits { get; init; } = new List<LocalizedText>();

    public List<string> TagIds { get; init; } = new List<string>();

    public GeoPoint? GeoLocation { get; init; }

    public string? OriginalFileName { get; init; }

    public string? ContentType { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public long SizeInBytes { get; init; }
}
