using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class PassportRideOccurrenceResultFactory
{
    public static RideOccurrenceResult Create(
        RideOccurrence occurrence,
        PassportHistoricalTarget? target = null)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        PassportHistoricalTarget? resolvedTarget = target is not null
            && string.Equals(target.ParkId, occurrence.ParkId, StringComparison.Ordinal)
                ? target
                : null;
        HistoricalConsistency historicalConsistency =
            resolvedTarget is null || resolvedTarget.IsValidationFallback
                ? occurrence.HistoricalConsistency
                : resolvedTarget.HistoricalConsistency;

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
            historicalConsistency,
            occurrence.PrivateNote,
            occurrence.CountsAsRide,
            occurrence.Version,
            occurrence.CreatedAtUtc,
            occurrence.UpdatedAtUtc,
            CreateTarget(occurrence, resolvedTarget),
            occurrence.Assessment is null
                ? null
                : new RideAssessmentResult(
                    occurrence.Assessment.Value.DoubleValue,
                    occurrence.Assessment.PrivateComment,
                    occurrence.Assessment.Revision,
                    occurrence.Assessment.CreatedAtUtc,
                    occurrence.Assessment.UpdatedAtUtc),
            (resolvedTarget is null || resolvedTarget.IsValidationFallback)
                && occurrence.HistoricalConsistency == HistoricalConsistency.ConfirmedConflict);
    }

    private static RideOccurrenceTargetResult? CreateTarget(
        RideOccurrence occurrence,
        PassportHistoricalTarget? target)
    {
        if (target is not null
            && string.Equals(target.ParkId, occurrence.ParkId, StringComparison.Ordinal))
        {
            if (target.IsValidationFallback && occurrence.HistoricalTarget is not null)
            {
                return new RideOccurrenceTargetResult(
                    occurrence.HistoricalTarget.Name,
                    occurrence.HistoricalTarget.Category,
                    target.LifecycleStatus,
                    true,
                    target.OpeningDate,
                    target.ClosingDate,
                    true);
            }

            return new RideOccurrenceTargetResult(
                target.Name,
                target.Category,
                target.LifecycleStatus,
                target.IsHistoricalOnly,
                target.OpeningDate,
                target.ClosingDate,
                true);
        }

        return occurrence.HistoricalTarget is null
            ? null
            : new RideOccurrenceTargetResult(
                occurrence.HistoricalTarget.Name,
                occurrence.HistoricalTarget.Category,
                null,
                true,
                null,
                null,
                false);
    }
}
