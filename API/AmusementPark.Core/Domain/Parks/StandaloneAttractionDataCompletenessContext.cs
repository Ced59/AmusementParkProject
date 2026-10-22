using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Facts loaded for one autonomous attraction; projection never mutates their visibility.
/// </summary>
public sealed record StandaloneAttractionDataCompletenessContext
{
    public bool ProjectForPublication { get; init; }

    public DateTime AsOfUtc { get; init; } = DateTime.UtcNow;

    public IReadOnlyCollection<Image> Images { get; init; } = Array.Empty<Image>();

    public IReadOnlyCollection<Image> ReferenceImages { get; init; } = Array.Empty<Image>();

    public IReadOnlyCollection<HistoryEvent> HistoryEvents { get; init; } = Array.Empty<HistoryEvent>();

    public ParkOperator? Operator { get; init; }

    public AttractionManufacturer? Manufacturer { get; init; }

    public ParkOpeningHoursSchedule? OpeningHours { get; init; }

    public ParkPricing? Pricing { get; init; }
}
