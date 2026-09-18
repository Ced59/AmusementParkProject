using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Trips.Models;

internal sealed record EligibleTripItems(
    IReadOnlyDictionary<string, Park> ParksById,
    IReadOnlyCollection<ParkItem> OrderedItems,
    IReadOnlyDictionary<string, ParkItem> ItemsById);
