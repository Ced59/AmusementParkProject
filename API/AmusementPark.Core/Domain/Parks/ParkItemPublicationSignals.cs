namespace AmusementPark.Core.Domain.Parks;

public sealed class ParkItemPublicationSignals
{
    public bool IsVisible { get; init; }

    public AdminReviewStatus AdminReviewStatus { get; init; }

    public DateTime? LastUpdatedAtUtc { get; init; }

    public IReadOnlyCollection<string> AvailableLanguageCodes { get; init; } = Array.Empty<string>();

    public bool IsPublishable { get; init; }
}
