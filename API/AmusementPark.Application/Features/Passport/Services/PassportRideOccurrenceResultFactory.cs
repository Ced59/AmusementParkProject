using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class PassportRideOccurrenceResultFactory
{
    public static RideOccurrenceResult Create(
        RideOccurrence occurrence,
        VisitTarget? target = null,
        VisitDate? visitDate = null)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        bool hasCurrentHistoricalEvidence = target is not null && visitDate is not null;
        HistoricalConsistency historicalConsistency = occurrence.HistoricalConsistency;
        if (target is not null && visitDate is not null)
        {
            historicalConsistency = RideOccurrenceHistoricalConsistencyEvaluator.Evaluate(
                visitDate,
                target.OpeningDate,
                target.ClosingDate);
        }

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
            CreateTarget(occurrence, target),
            occurrence.Assessment is null
                ? null
                : new RideAssessmentResult(
                    occurrence.Assessment.Value.DoubleValue,
                    occurrence.Assessment.PrivateComment,
                    occurrence.Assessment.Revision,
                    occurrence.Assessment.CreatedAtUtc,
                    occurrence.Assessment.UpdatedAtUtc),
            !hasCurrentHistoricalEvidence
                && occurrence.HistoricalConsistency == HistoricalConsistency.ConfirmedConflict);
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
                false,
                target.OpeningDate,
                target.ClosingDate);
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
