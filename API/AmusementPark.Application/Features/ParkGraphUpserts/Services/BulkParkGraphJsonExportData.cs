using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed class BulkParkGraphJsonExportData
{
    public IReadOnlyDictionary<string, IReadOnlyCollection<ParkZone>> ZonesByParkId { get; init; } =
        new Dictionary<string, IReadOnlyCollection<ParkZone>>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, IReadOnlyCollection<ParkItem>> ItemsByParkId { get; init; } =
        new Dictionary<string, IReadOnlyCollection<ParkItem>>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, ParkGraphExportReferences?> ReferencesByParkId { get; init; } =
        new Dictionary<string, ParkGraphExportReferences?>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, IReadOnlyCollection<Image>> ImagesByParkId { get; init; } =
        new Dictionary<string, IReadOnlyCollection<Image>>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, ParkOpeningHoursSchedule?> OpeningHoursByParkId { get; init; } =
        new Dictionary<string, ParkOpeningHoursSchedule?>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, IReadOnlyCollection<HistoryEvent>> HistoryEventsByParkId { get; init; } =
        new Dictionary<string, IReadOnlyCollection<HistoryEvent>>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, ParkPricingEntity?> PricingByParkId { get; init; } =
        new Dictionary<string, ParkPricingEntity?>(StringComparer.Ordinal);
}
