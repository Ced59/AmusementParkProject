namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Décide si un snapshot annuel possède assez de valeur éditoriale pour être indexé.
/// Les sélections mensuelles ou journalières restent consultables mais ne créent jamais
/// de nouvelles pages SEO.
/// </summary>
public static class HistoricalSnapshotSeoEligibilityEvaluator
{
    public const int MinimumSupportingFactCount = 2;

    public static bool IsIndexableKeyYear(
        ParkHistoricalSnapshot snapshot,
        IReadOnlyCollection<HistoricalFact> facts)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(facts);

        if (snapshot.RequestedInstant.Precision != HistoryDatePrecision.Year
            || snapshot.Coverage.Status == HistoricalCoverageStatus.Partial)
        {
            return false;
        }

        int requestedYear = snapshot.RequestedInstant.Year;
        bool hasMajorEvent = facts.Any(fact =>
            fact.IsDecisionEligible
            && fact.Importance == HistoricalImportance.Major
            && HasBoundaryInYear(fact.Period, requestedYear));
        if (!hasMajorEvent)
        {
            return false;
        }

        HashSet<Guid> supportingFactIds = snapshot.Subjects
            .SelectMany(static subject => subject.SupportingFactIds)
            .ToHashSet();
        int supportingFactCount = facts
            .Where(static fact => fact.IsDecisionEligible)
            .Select(static fact => fact.Id)
            .Distinct()
            .Count(supportingFactIds.Contains);

        return supportingFactCount >= MinimumSupportingFactCount;
    }

    private static bool HasBoundaryInYear(HistoricalPeriod period, int year)
    {
        return period.Start?.Year == year || period.End?.Year == year;
    }
}
