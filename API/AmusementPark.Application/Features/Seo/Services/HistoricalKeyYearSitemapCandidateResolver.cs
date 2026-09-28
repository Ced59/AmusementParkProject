using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Seo.Services;

public static class HistoricalKeyYearSitemapCandidateResolver
{
    public static IReadOnlyCollection<HistoricalKeyYearSitemapCandidate> Resolve(
        IReadOnlyCollection<Park> parks,
        IReadOnlyCollection<ParkItem> parkItems,
        IReadOnlyCollection<ParkZone> parkZones,
        IReadOnlyCollection<HistoricalFact> facts,
        IParkHistoricalSnapshotBuilder snapshotBuilder)
    {
        ArgumentNullException.ThrowIfNull(parks);
        ArgumentNullException.ThrowIfNull(parkItems);
        ArgumentNullException.ThrowIfNull(parkZones);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(snapshotBuilder);

        Dictionary<string, Park> publicParks = parks
            .Where(IsPublicPark)
            .GroupBy(static park => park.Id, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.First(),
                StringComparer.Ordinal);
        Dictionary<string, HistoricalSubject[]> currentSubjectsByPark = publicParks.Values
            .ToDictionary(
                static park => park.Id,
                park => BuildCurrentSubjects(park, parkItems, parkZones),
                StringComparer.Ordinal);
        HashSet<(HistoricalSubjectType Type, string Id)> currentSubjectKeys = currentSubjectsByPark.Values
            .SelectMany(static subjects => subjects)
            .Select(static subject => (subject.Type, subject.Id))
            .ToHashSet();
        HistoricalFact[] publicFacts = facts
            .Where(static fact => fact.IsDecisionEligible)
            .Where(fact => CanExpose(fact, publicParks, currentSubjectKeys))
            .ToArray();

        List<HistoricalKeyYearSitemapCandidate> candidates = new List<HistoricalKeyYearSitemapCandidate>();
        foreach (IGrouping<string, HistoricalFact> parkFactsGroup in publicFacts
                     .GroupBy(ResolveParkId, StringComparer.Ordinal)
                     .Where(static group => group.Key.Length > 0))
        {
            string parkId = parkFactsGroup.Key;
            if (!publicParks.TryGetValue(parkId, out Park? park)
                || !currentSubjectsByPark.TryGetValue(parkId, out HistoricalSubject[]? currentSubjects))
            {
                continue;
            }

            HistoricalFact[] parkFacts = parkFactsGroup.ToArray();
            HistoricalSubject[] subjects = currentSubjects
                .Concat(parkFacts
                    .Where(static fact => fact.Subject.PublicationPolicy
                        == HistoricalSubjectPublicationPolicy.HistoricalOnly)
                    .Select(static fact => fact.Subject))
                .DistinctBy(static subject => (subject.Type, subject.Id))
                .ToArray();
            DateTime lastModifiedAtUtc = parkFacts.Max(static fact => fact.RecordedAtUtc);
            int[] keyYearCandidates = parkFacts
                .Where(static fact => fact.Importance == HistoricalImportance.Major)
                .SelectMany(static fact => BoundaryYears(fact.Period))
                .Distinct()
                .Order()
                .ToArray();

            foreach (int year in keyYearCandidates)
            {
                ParkHistoricalSnapshot snapshot = snapshotBuilder.Build(
                    parkId,
                    HistoricalInstant.ForYear(year),
                    subjects,
                    parkFacts);
                if (HistoricalSnapshotSeoEligibilityEvaluator.IsIndexableKeyYear(snapshot, parkFacts))
                {
                    candidates.Add(new HistoricalKeyYearSitemapCandidate(
                        parkId,
                        park.Name ?? string.Empty,
                        year,
                        lastModifiedAtUtc));
                }
            }
        }

        return candidates
            .OrderBy(static candidate => candidate.ParkName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static candidate => candidate.Year)
            .ToArray();
    }

    private static HistoricalSubject[] BuildCurrentSubjects(
        Park park,
        IReadOnlyCollection<ParkItem> parkItems,
        IReadOnlyCollection<ParkZone> parkZones)
    {
        return new[]
            {
                new HistoricalSubject(
                    HistoricalSubjectType.Park,
                    park.Id,
                    ResolveLabel(park.Name, "Park"),
                    HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                    park.Id),
            }
            .Concat(parkItems
                .Where(item => string.Equals(item.ParkId, park.Id, StringComparison.Ordinal)
                    && IsPublicParkItem(item))
                .Select(item => new HistoricalSubject(
                    HistoricalSubjectType.ParkItem,
                    item.Id,
                    ResolveLabel(item.Name, "Park item"),
                    HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                    park.Id)))
            .Concat(parkZones
                .Where(zone => string.Equals(zone.ParkId, park.Id, StringComparison.Ordinal)
                    && zone.IsVisible)
                .Select(zone => new HistoricalSubject(
                    HistoricalSubjectType.ParkZone,
                    zone.Id,
                    ResolveLabel(zone.Name, "Park zone"),
                    HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                    park.Id)))
            .DistinctBy(static subject => (subject.Type, subject.Id))
            .ToArray();
    }

    private static bool CanExpose(
        HistoricalFact fact,
        IReadOnlyDictionary<string, Park> publicParks,
        IReadOnlySet<(HistoricalSubjectType Type, string Id)> currentSubjectKeys)
    {
        string parkId = ResolveParkId(fact);
        if (parkId.Length == 0
            || !publicParks.ContainsKey(parkId)
            || fact.Subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.Suppressed)
        {
            return false;
        }

        return fact.Subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.HistoricalOnly
            || currentSubjectKeys.Contains((fact.Subject.Type, fact.Subject.Id));
    }

    private static string ResolveParkId(HistoricalFact fact)
    {
        return fact.Subject.ContextParkId
            ?? (fact.Subject.Type == HistoricalSubjectType.Park ? fact.Subject.Id : string.Empty);
    }

    private static IEnumerable<int> BoundaryYears(HistoricalPeriod period)
    {
        if (period.Start is not null)
        {
            yield return period.Start.Year;
        }

        if (period.End is not null && period.End.Year != period.Start?.Year)
        {
            yield return period.End.Year;
        }
    }

    private static bool IsPublicPark(Park park)
    {
        return park.IsVisible && park.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    private static bool IsPublicParkItem(ParkItem parkItem)
    {
        return parkItem.IsVisible && parkItem.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    private static string ResolveLabel(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
