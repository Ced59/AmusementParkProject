using AmusementPark.Application.Features.FactualEvents.Models;

namespace AmusementPark.Application.Features.ParkOpeningHours.Models;

public sealed record ParkOpeningHoursPendingFactualChange(
    string ParkId,
    FactualChangeOutboxEntry Entry);
