using AmusementPark.Core.Domain.History;

namespace AmusementPark.Core.Domain.Visits;

public static class PassportHistoricalStatisticsCalculator
{
    public static PassportHistoricalStatistics Calculate(
        IReadOnlyCollection<PassportHistoricalVisitContextObservation> visits,
        IReadOnlyCollection<PassportHistoricalRideContextObservation> rides)
    {
        ArgumentNullException.ThrowIfNull(visits);
        ArgumentNullException.ThrowIfNull(rides);
        EnsureRideVisitsExist(visits, rides);

        PassportHistoricalRideContextObservation[] completedRides = rides
            .Where(static ride => ride.Status == RideOccurrenceStatus.Completed)
            .ToArray();
        PassportHistoricalRideContextObservation[] canonicalRides = completedRides
            .Where(static ride => ride.TargetAtVisit?.IsCanonical == true)
            .ToArray();
        PassportHistoricalParkEraStatistic[] parksAcrossEras = BuildParkEras(visits);

        return new PassportHistoricalStatistics(
            visits.LongCount(),
            visits.Count == 0 ? null : visits.Min(static visit => visit.VisitDate.Year),
            completedRides.LongCount(),
            canonicalRides.LongCount(),
            completedRides.Length == 0
                ? 0d
                : (double)canonicalRides.LongLength / completedRides.LongLength,
            parksAcrossEras.LongCount(static park => park.CanonicalEraCount > 1),
            parksAcrossEras,
            BuildDisappearedAttractions(canonicalRides),
            BuildTransformations(canonicalRides),
            BuildHistoricalNames(canonicalRides),
            BuildHistoricalCategories(canonicalRides));
    }

    private static PassportHistoricalParkEraStatistic[] BuildParkEras(
        IReadOnlyCollection<PassportHistoricalVisitContextObservation> visits)
    {
        return visits.GroupBy(static visit => visit.ParkId, StringComparer.Ordinal)
            .Select(group => new PassportHistoricalParkEraStatistic(
                group.Key,
                group.Min(static visit => visit.VisitDate.Year),
                group.Max(static visit => visit.VisitDate.Year),
                group.LongCount(),
                group.Select(CreateCanonicalEraFingerprint)
                    .Where(static fingerprint => fingerprint is not null)
                    .Distinct(StringComparer.Ordinal)
                    .LongCount()))
            .OrderByDescending(static park => park.CanonicalEraCount)
            .ThenBy(static park => park.FirstVisitYear)
            .ThenBy(static park => park.ParkId, StringComparer.Ordinal)
            .ToArray();
    }

    private static string? CreateCanonicalEraFingerprint(
        PassportHistoricalVisitContextObservation visit)
    {
        string[] knownOpenTargets = visit.Targets
            .Where(static target => target.IsCanonical
                && target.OperationalState == HistoricalOperationalState.KnownOpen)
            .OrderBy(static target => target.ParkItemId, StringComparer.Ordinal)
            .Select(static target => string.Join(
                '\u001f',
                target.ParkItemId,
                target.Name.ToUpperInvariant(),
                target.Category.ToUpperInvariant()))
            .ToArray();
        return knownOpenTargets.Length == 0
            ? null
            : string.Join('\u001e', knownOpenTargets);
    }

    private static PassportHistoricalDisappearedAttractionStatistic[]
        BuildDisappearedAttractions(
            IReadOnlyCollection<PassportHistoricalRideContextObservation> rides)
    {
        return rides.Where(static ride =>
                ride.TargetAtVisit!.OperationalState == HistoricalOperationalState.KnownOpen
                && ride.CurrentTarget?.IsCanonical == true
                && ride.CurrentTarget.OperationalState == HistoricalOperationalState.KnownClosed)
            .GroupBy(
                static ride => (ride.ParkId, ride.ParkItemId),
                EqualityComparer<(string ParkId, string ParkItemId)>.Default)
            .Select(group => new PassportHistoricalDisappearedAttractionStatistic(
                group.Key.ParkId,
                group.Key.ParkItemId,
                group.OrderByDescending(static ride => ride.VisitDate.ChronologicalOrderValue)
                    .First().TargetAtVisit!.Name,
                group.Min(static ride => ride.VisitDate.Year),
                group.Max(static ride => ride.VisitDate.Year),
                group.LongCount()))
            .OrderByDescending(static item => item.CompletedRideCount)
            .ThenBy(static item => item.NameAtVisit, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static PassportHistoricalTransformationStatistic[] BuildTransformations(
        IReadOnlyCollection<PassportHistoricalRideContextObservation> rides)
    {
        return rides.Where(static ride => ride.CurrentTarget?.IsCanonical == true
                && (HasChanged(ride.TargetAtVisit!.Name, ride.CurrentTarget.Name)
                    || HasChanged(ride.TargetAtVisit.Category, ride.CurrentTarget.Category)))
            .GroupBy(
                static ride => (ride.ParkId, ride.ParkItemId),
                EqualityComparer<(string ParkId, string ParkItemId)>.Default)
            .Select(group =>
            {
                PassportHistoricalTargetStateObservation current = group.First().CurrentTarget!;
                return new PassportHistoricalTransformationStatistic(
                    group.Key.ParkId,
                    group.Key.ParkItemId,
                    current.Name,
                    current.Category,
                    group.Select(static ride => ride.TargetAtVisit!.Name)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                    group.Select(static ride => ride.TargetAtVisit!.Category)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(static category => category, StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                    group.Min(static ride => ride.VisitDate.Year),
                    group.Max(static ride => ride.VisitDate.Year),
                    group.LongCount());
            })
            .OrderByDescending(static item => item.CompletedRideCount)
            .ThenBy(static item => item.CurrentName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static PassportHistoricalNameStatistic[] BuildHistoricalNames(
        IReadOnlyCollection<PassportHistoricalRideContextObservation> rides)
    {
        return rides.GroupBy(
                static ride => (ride.ParkId, ride.ParkItemId, ride.TargetAtVisit!.Name),
                EqualityComparer<(string ParkId, string ParkItemId, string Name)>.Default)
            .Select(group => new PassportHistoricalNameStatistic(
                group.Key.ParkId,
                group.Key.ParkItemId,
                group.Key.Name,
                group.First().CurrentTarget?.IsCanonical == true
                    ? group.First().CurrentTarget!.Name
                    : null,
                group.Min(static ride => ride.VisitDate.Year),
                group.Max(static ride => ride.VisitDate.Year),
                group.LongCount()))
            .OrderByDescending(static item => item.CompletedRideCount)
            .ThenBy(static item => item.NameAtVisit, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static PassportHistoricalCategoryStatistic[] BuildHistoricalCategories(
        IReadOnlyCollection<PassportHistoricalRideContextObservation> rides)
    {
        return rides.GroupBy(
                static ride => ride.TargetAtVisit!.Category,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => new PassportHistoricalCategoryStatistic(
                group.Key,
                group.LongCount(),
                group.Select(static ride => ride.ParkItemId)
                    .Distinct(StringComparer.Ordinal)
                    .LongCount()))
            .OrderByDescending(static item => item.CompletedRideCount)
            .ThenBy(static item => item.Category, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool HasChanged(string historicalValue, string currentValue)
    {
        return !string.Equals(
            historicalValue.Trim(),
            currentValue.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureRideVisitsExist(
        IReadOnlyCollection<PassportHistoricalVisitContextObservation> visits,
        IReadOnlyCollection<PassportHistoricalRideContextObservation> rides)
    {
        HashSet<string> visitIds = visits.Select(static visit => visit.VisitId)
            .ToHashSet(StringComparer.Ordinal);
        if (rides.Any(ride => !visitIds.Contains(ride.VisitId)))
        {
            throw new ArgumentException("Every ride observation must belong to a supplied visit.");
        }
    }
}
