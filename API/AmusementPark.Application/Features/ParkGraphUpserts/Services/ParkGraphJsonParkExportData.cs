using System.Globalization;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

internal sealed class ParkGraphJsonParkExportData
{
    public Park Park { get; init; } = new Park();

    public ParkGraphExportReferences? References { get; init; }

    public IReadOnlyCollection<ParkZone> Zones { get; init; } = Array.Empty<ParkZone>();

    public IReadOnlyCollection<ParkItem> Items { get; init; } = Array.Empty<ParkItem>();

    public IReadOnlyCollection<Image> Images { get; init; } = Array.Empty<Image>();

    public ParkOpeningHoursSchedule? OpeningHours { get; init; }

    public ParkPricingEntity? Pricing { get; init; }

    public IReadOnlyCollection<HistoryEvent> HistoryEvents { get; init; } = Array.Empty<HistoryEvent>();
}
