namespace AmusementPark.Core.Domain.Parks;

public sealed class ParkItemContentQuality
{
    public bool StructureComplete { get; init; }

    public bool HasAnyDescription { get; init; }

    public bool HasFrenchDescription { get; init; }

    public bool HasEnglishDescription { get; init; }

    public bool HasZone { get; init; }

    public bool HasPreciseType { get; init; }

    public bool HasLocation { get; init; }

    public bool HasAccessConditions { get; init; }

    public bool IsPublishable { get; init; }

    public IReadOnlyCollection<string> AvailableLanguageCodes { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> MissingRequirementKeys { get; init; } = Array.Empty<string>();
}

public sealed class ParkItemPublicationSignals
{
    public bool IsVisible { get; init; }

    public AdminReviewStatus AdminReviewStatus { get; init; }

    public DateTime? LastUpdatedAtUtc { get; init; }

    public IReadOnlyCollection<string> AvailableLanguageCodes { get; init; } = Array.Empty<string>();

    public bool IsPublishable { get; init; }
}
