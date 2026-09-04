using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

internal sealed record ParsedOccurrenceScope(
    string UserId,
    VisitId VisitId,
    RideOccurrenceId OccurrenceId);

internal static class PassportRideOccurrenceHandlerSupport
{
    public static bool TryNormalizeRequestScope(
        string? userId,
        string? visitIdValue,
        out string normalizedUserId,
        out VisitId visitId)
    {
        normalizedUserId = userId?.Trim() ?? string.Empty;
        visitId = default;
        if (normalizedUserId.Length == 0)
        {
            return false;
        }

        try
        {
            visitId = VisitId.Parse(visitIdValue);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static ParsedOccurrenceScope? ParseOccurrenceScope(
        string? userId,
        string? visitIdValue,
        string? occurrenceIdValue)
    {
        if (!TryNormalizeRequestScope(
            userId,
            visitIdValue,
            out string normalizedUserId,
            out VisitId visitId))
        {
            return null;
        }

        try
        {
            return new ParsedOccurrenceScope(
                normalizedUserId,
                visitId,
                RideOccurrenceId.Parse(occurrenceIdValue));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public static RideOccurrenceId? ParseOptionalOccurrenceId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return RideOccurrenceId.Parse(value);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public static string? NormalizeOperationId(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 or > CreateVisitCommandHandler.MaximumClientOperationIdLength
            || normalized.Any(char.IsControl))
        {
            return null;
        }

        return normalized;
    }

    public static ApplicationError? ValidateTargets(
        Visit visit,
        IReadOnlyCollection<RideOccurrenceCreationItem> items,
        IReadOnlyDictionary<string, VisitTarget> targets)
    {
        foreach (RideOccurrenceCreationItem item in items)
        {
            string parkItemId = item.ParkItemId.Trim();
            if (!targets.TryGetValue(parkItemId, out VisitTarget? target))
            {
                return PassportApplicationErrors.VisitTargetNotFound();
            }

            if (!string.Equals(target.ParkId, visit.ParkId, StringComparison.Ordinal))
            {
                return PassportApplicationErrors.VisitTargetParkMismatch();
            }

            if (target.Category != ParkItemCategory.Attraction)
            {
                return PassportApplicationErrors.VisitTargetNotAttraction();
            }

            HistoricalConsistency consistency =
                RideOccurrenceHistoricalConsistencyEvaluator.Evaluate(
                    visit.Date,
                    target.OpeningDate,
                    target.ClosingDate);
            if (consistency == HistoricalConsistency.ConfirmedConflict
                && !item.ConfirmHistoricalConflict)
            {
                return PassportApplicationErrors.HistoricalConflictConfirmationRequired();
            }
        }

        return null;
    }

    public static ApplicationError? ValidateEditable(Visit visit)
    {
        ArgumentNullException.ThrowIfNull(visit);
        return visit.Status == VisitStatus.Draft
            ? null
            : PassportApplicationErrors.VisitNotEditable();
    }
}
