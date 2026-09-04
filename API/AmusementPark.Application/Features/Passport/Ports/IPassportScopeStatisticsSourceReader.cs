using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

public sealed record PassportParkStatisticsSource(
    IReadOnlyCollection<PassportVisitStatisticsObservation> Visits,
    IReadOnlyCollection<PassportRideStatisticsObservation> Rides,
    RatingValue? CurrentGlobalRating,
    IReadOnlyCollection<PassportCurrentItemRatingObservation> CurrentItemRatings);

public sealed record PassportYearStatisticsSource(
    IReadOnlyCollection<PassportVisitStatisticsObservation> Visits,
    IReadOnlyCollection<PassportRideStatisticsObservation> Rides);

/// <summary>
/// Lit les projections privées minimales nécessaires aux statistiques d'un parc ou d'une année.
/// </summary>
public interface IPassportScopeStatisticsSourceReader
{
    Task<PassportParkStatisticsSource> ReadParkAsync(
        string userId,
        string parkId,
        CancellationToken cancellationToken);

    Task<PassportYearStatisticsSource> ReadYearAsync(
        string userId,
        int year,
        CancellationToken cancellationToken);
}
