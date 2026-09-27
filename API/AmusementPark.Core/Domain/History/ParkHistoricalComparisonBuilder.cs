namespace AmusementPark.Core.Domain.History;

public sealed class ParkHistoricalComparisonBuilder : IParkHistoricalComparisonBuilder
{
    public const string MethodologyVersion = "hist-compare-v1";

    public ParkHistoricalComparison Build(
        ParkHistoricalSnapshot from,
        ParkHistoricalSnapshot to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        Dictionary<(HistoricalSubjectType Type, string Id), HistoricalSubjectSnapshot> fromSubjects =
            from.Subjects.ToDictionary(static subject => (subject.Subject.Type, subject.Subject.Id));
        Dictionary<(HistoricalSubjectType Type, string Id), HistoricalSubjectSnapshot> toSubjects =
            to.Subjects.ToDictionary(static subject => (subject.Subject.Type, subject.Subject.Id));
        if (!fromSubjects.Keys.ToHashSet().SetEquals(toSubjects.Keys))
        {
            throw new ArgumentException("Historical comparisons require the same subject scope at both dates.");
        }

        HistoricalSubjectComparison[] comparisons = fromSubjects.Keys
            .Select(key => CompareSubject(fromSubjects[key], toSubjects[key]))
            .ToArray();
        (Dictionary<string, int> Counts, int UnclassifiedCount) fromCategories = CountOpenItemCategories(
            from.Subjects);
        (Dictionary<string, int> Counts, int UnclassifiedCount) toCategories = CountOpenItemCategories(
            to.Subjects);
        string[] categories = fromCategories.Counts.Keys
            .Concat(toCategories.Counts.Keys)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        HistoricalCategoryNetChange[] categoryChanges = categories
            .Select(category => new HistoricalCategoryNetChange(
                category,
                fromCategories.Counts.GetValueOrDefault(category),
                toCategories.Counts.GetValueOrDefault(category)))
            .ToArray();

        return new ParkHistoricalComparison(
            from,
            to,
            comparisons,
            categoryChanges,
            fromCategories.UnclassifiedCount,
            toCategories.UnclassifiedCount,
            MethodologyVersion);
    }

    private static HistoricalSubjectComparison CompareSubject(
        HistoricalSubjectSnapshot from,
        HistoricalSubjectSnapshot to)
    {
        return new HistoricalSubjectComparison(
            from,
            to,
            ResolvePresenceChange(from.OperationalState, to.OperationalState),
            ResolveKnownAttribute(from, HistoricalAttributeKind.Name),
            ResolveKnownAttribute(to, HistoricalAttributeKind.Name),
            ResolveKnownAttribute(from, HistoricalAttributeKind.Zone),
            ResolveKnownAttribute(to, HistoricalAttributeKind.Zone),
            ResolveKnownAttribute(from, HistoricalAttributeKind.Category),
            ResolveKnownAttribute(to, HistoricalAttributeKind.Category));
    }

    private static HistoricalPresenceChange ResolvePresenceChange(
        HistoricalOperationalState from,
        HistoricalOperationalState to)
    {
        if (from == HistoricalOperationalState.KnownOpen && to == HistoricalOperationalState.KnownOpen)
        {
            return HistoricalPresenceChange.PresentAtBoth;
        }

        if (from == HistoricalOperationalState.KnownClosed && to == HistoricalOperationalState.KnownOpen)
        {
            return HistoricalPresenceChange.Opened;
        }

        if (from == HistoricalOperationalState.KnownOpen && to == HistoricalOperationalState.KnownClosed)
        {
            return HistoricalPresenceChange.Closed;
        }

        return from == HistoricalOperationalState.KnownClosed && to == HistoricalOperationalState.KnownClosed
            ? HistoricalPresenceChange.AbsentAtBoth
            : HistoricalPresenceChange.Uncertain;
    }

    private static string? ResolveKnownAttribute(
        HistoricalSubjectSnapshot subject,
        HistoricalAttributeKind kind)
    {
        HistoricalAttributeSnapshot? attribute = subject.Attributes.FirstOrDefault(
            candidate => candidate.Kind == kind
                && candidate.State == HistoricalAttributeValueState.Known);
        return attribute?.Value;
    }

    private static (Dictionary<string, int> Counts, int UnclassifiedCount) CountOpenItemCategories(
        IReadOnlyCollection<HistoricalSubjectSnapshot> subjects)
    {
        Dictionary<string, int> counts = new(StringComparer.Ordinal);
        int unclassifiedCount = 0;
        foreach (HistoricalSubjectSnapshot subject in subjects.Where(static subject =>
                     subject.Subject.Type == HistoricalSubjectType.ParkItem
                     && subject.OperationalState == HistoricalOperationalState.KnownOpen))
        {
            string? category = ResolveKnownAttribute(subject, HistoricalAttributeKind.Category);
            if (category is null)
            {
                unclassifiedCount++;
                continue;
            }

            counts[category] = counts.GetValueOrDefault(category) + 1;
        }

        return (counts, unclassifiedCount);
    }
}
