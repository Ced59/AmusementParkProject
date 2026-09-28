using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Seo.Services;

public static class HistoricalKeyYearSitemapCandidateResolver
{
    public static async Task<IReadOnlyCollection<HistoricalKeyYearSitemapCandidate>> ResolveAsync(
        IReadOnlyCollection<Park> parks,
        IReadOnlyCollection<ParkItem> parkItems,
        IReadOnlyCollection<ParkZone> parkZones,
        IReadOnlyCollection<HistoricalFact> facts,
        IHistoricalParkRolloutGateAssessmentService rolloutGateAssessmentService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parks);
        ArgumentNullException.ThrowIfNull(parkItems);
        ArgumentNullException.ThrowIfNull(parkZones);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(rolloutGateAssessmentService);

        Dictionary<string, Park> publicParks = parks
            .Where(IsPublicPark)
            .GroupBy(static park => park.Id, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.First(),
                StringComparer.Ordinal);
        IReadOnlyDictionary<string, ParkItem[]> publicItemsByPark = parkItems
            .Where(IsPublicParkItem)
            .Where(static item => !string.IsNullOrWhiteSpace(item.ParkId))
            .GroupBy(static item => item.ParkId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.Ordinal);
        IReadOnlyDictionary<string, ParkZone[]> publicZonesByPark = parkZones
            .Where(static zone => zone.IsVisible)
            .Where(static zone => !string.IsNullOrWhiteSpace(zone.ParkId))
            .GroupBy(static zone => zone.ParkId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.Ordinal);
        Dictionary<string, HistoricalSubject[]> currentSubjectsByPark = publicParks.Values
            .ToDictionary(
                static park => park.Id,
                park => BuildCurrentSubjects(
                    park,
                    publicItemsByPark.GetValueOrDefault(park.Id) ?? Array.Empty<ParkItem>(),
                    publicZonesByPark.GetValueOrDefault(park.Id) ?? Array.Empty<ParkZone>()),
                StringComparer.Ordinal);
        HashSet<(HistoricalSubjectType Type, string Id, string ContextParkId)> currentSubjectKeys =
            currentSubjectsByPark.Values
            .SelectMany(static subjects => subjects)
            .Select(static subject => (subject.Type, subject.Id, subject.ContextParkId!))
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
            HistoricalParkRolloutGate rolloutGate = await rolloutGateAssessmentService.AssessAsync(
                parkId,
                subjects,
                parkFacts,
                cancellationToken);
            if (!rolloutGate.IsOpen)
            {
                continue;
            }

            DateTime lastModifiedAtUtc = parkFacts.Max(static fact => fact.RecordedAtUtc);
            foreach (int year in rolloutGate.IndexableKeyYears)
            {
                candidates.Add(new HistoricalKeyYearSitemapCandidate(
                    parkId,
                    park.Name ?? string.Empty,
                    year,
                    lastModifiedAtUtc));
            }
        }

        return candidates
            .OrderBy(static candidate => candidate.ParkName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static candidate => candidate.Year)
            .ToArray();
    }

    private static HistoricalSubject[] BuildCurrentSubjects(
        Park park,
        IReadOnlyCollection<ParkItem> publicParkItems,
        IReadOnlyCollection<ParkZone> publicParkZones)
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
            .Concat(publicParkItems
                .Select(item => new HistoricalSubject(
                    HistoricalSubjectType.ParkItem,
                    item.Id,
                    ResolveLabel(item.Name, "Park item"),
                    HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                    park.Id)))
            .Concat(publicParkZones
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
        IReadOnlySet<(HistoricalSubjectType Type, string Id, string ContextParkId)> currentSubjectKeys)
    {
        string parkId = ResolveParkId(fact);
        if (parkId.Length == 0
            || !publicParks.ContainsKey(parkId)
            || fact.Subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.Suppressed)
        {
            return false;
        }

        return fact.Subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.HistoricalOnly
            || currentSubjectKeys.Contains((fact.Subject.Type, fact.Subject.Id, parkId));
    }

    private static string ResolveParkId(HistoricalFact fact)
    {
        return fact.Subject.ContextParkId
            ?? (fact.Subject.Type == HistoricalSubjectType.Park ? fact.Subject.Id : string.Empty);
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
