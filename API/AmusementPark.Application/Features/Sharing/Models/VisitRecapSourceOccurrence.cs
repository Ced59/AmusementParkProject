using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record VisitRecapSourceOccurrence(
    string ParkItemId,
    RideOccurrenceStatus Status,
    string? HistoricalName,
    ParkItemCategory? HistoricalCategory,
    RatingValue? Rating);
