using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class PassportRideOccurrenceResultFactory
{
    public static RideOccurrenceResult Create(
        RideOccurrence occurrence,
        VisitTarget? target = null)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        return new RideOccurrenceResult(
            occurrence.Id.Value,
            occurrence.VisitId.Value,
            occurrence.ParkId,
            occurrence.ParkItemId,
            occurrence.SortPosition,
            new RideOccurrenceMomentResult(
                occurrence.Moment.LocalTime,
                occurrence.Moment.IsApproximate),
            occurrence.Status,
            occurrence.Source,
            occurrence.HistoricalConsistency,
            occurrence.PrivateNote,
            occurrence.CountsAsRide,
            occurrence.Version,
            occurrence.CreatedAtUtc,
            occurrence.UpdatedAtUtc,
            CreateTarget(occurrence, target));
    }

    private static RideOccurrenceTargetResult? CreateTarget(
        RideOccurrence occurrence,
        VisitTarget? target)
    {
        if (target is not null
            && string.Equals(target.ParkId, occurrence.ParkId, StringComparison.Ordinal))
        {
            return new RideOccurrenceTargetResult(
                target.Name,
                target.Category.ToString(),
                target.LifecycleStatus,
                false);
        }

        return occurrence.HistoricalTarget is null
            ? null
            : new RideOccurrenceTargetResult(
                occurrence.HistoricalTarget.Name,
                occurrence.HistoricalTarget.Category,
                null,
                true);
    }
}
