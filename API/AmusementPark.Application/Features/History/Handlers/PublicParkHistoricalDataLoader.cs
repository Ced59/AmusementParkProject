using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class PublicParkHistoricalDataLoader
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IHistoricalFactRepository historicalFactRepository;

    public PublicParkHistoricalDataLoader(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IParkZoneRepository parkZoneRepository,
        IHistoricalFactRepository historicalFactRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.historicalFactRepository = historicalFactRepository;
    }

    public async Task<PublicParkHistoricalData?> LoadAsync(
        string parkId,
        CancellationToken cancellationToken)
    {
        Park? park = await this.parkRepository.GetByIdAsync(parkId, false, cancellationToken);
        if (!HistoryPublicVisibility.IsPublicPark(park))
        {
            return null;
        }

        IReadOnlyCollection<ParkItem> parkItems = await this.parkItemRepository.GetByParkIdAsync(
            park!.Id,
            true,
            cancellationToken);
        IReadOnlyCollection<ParkZone> parkZones = await this.parkZoneRepository.GetByParkIdAsync(
            park.Id,
            cancellationToken);
        HistoricalSubject[] candidateSubjects = BuildCandidateSubjects(park, parkItems, parkZones);
        IReadOnlyCollection<HistoricalFact> latestFacts =
            await this.historicalFactRepository.GetLatestRevisionsForSubjectsAsync(
                candidateSubjects,
                cancellationToken);
        HashSet<(HistoricalSubjectType Type, string Id)> publicCurrentSubjects = BuildPublicCurrentSubjectKeys(
            park,
            parkItems,
            parkZones);
        HistoricalFact[] publicFacts = latestFacts
            .Where(fact => CanExposeFact(fact, publicCurrentSubjects))
            .ToArray();
        HashSet<(HistoricalSubjectType Type, string Id)> frozenHistoricalSubjects = publicFacts
            .Where(static fact => fact.Subject.PublicationPolicy
                == HistoricalSubjectPublicationPolicy.HistoricalOnly)
            .Select(static fact => (fact.Subject.Type, fact.Subject.Id))
            .ToHashSet();
        HistoricalSubject[] selectedSubjects = candidateSubjects
            .Where(subject => publicCurrentSubjects.Contains((subject.Type, subject.Id))
                || frozenHistoricalSubjects.Contains((subject.Type, subject.Id)))
            .ToArray();
        Dictionary<string, string> zoneNames = parkZones
            .Where(static zone => !string.IsNullOrWhiteSpace(zone.Id)
                && !string.IsNullOrWhiteSpace(zone.Name))
            .GroupBy(static zone => zone.Id, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.First().Name.Trim(),
                StringComparer.Ordinal);

        return new PublicParkHistoricalData(park, selectedSubjects, publicFacts, zoneNames);
    }

    private static HistoricalSubject[] BuildCandidateSubjects(
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
                    HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
            }
            .Concat(parkItems.Select(item => new HistoricalSubject(
                HistoricalSubjectType.ParkItem,
                item.Id,
                ResolveLabel(item.Name, "Park item"),
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject)))
            .Concat(parkZones.Select(zone => new HistoricalSubject(
                HistoricalSubjectType.ParkZone,
                zone.Id,
                ResolveLabel(zone.Name, "Park zone"),
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject)))
            .DistinctBy(static subject => (subject.Type, subject.Id))
            .ToArray();
    }

    private static HashSet<(HistoricalSubjectType Type, string Id)> BuildPublicCurrentSubjectKeys(
        Park park,
        IReadOnlyCollection<ParkItem> parkItems,
        IReadOnlyCollection<ParkZone> parkZones)
    {
        HashSet<(HistoricalSubjectType Type, string Id)> keys = new()
        {
            (HistoricalSubjectType.Park, park.Id),
        };
        keys.UnionWith(parkItems
            .Where(HistoryPublicVisibility.IsPublicParkItem)
            .Select(static item => (HistoricalSubjectType.ParkItem, item.Id)));
        keys.UnionWith(parkZones
            .Where(static zone => zone.IsVisible)
            .Select(static zone => (HistoricalSubjectType.ParkZone, zone.Id)));
        return keys;
    }

    private static bool CanExposeFact(
        HistoricalFact fact,
        IReadOnlySet<(HistoricalSubjectType Type, string Id)> publicCurrentSubjects)
    {
        if (!fact.IsDecisionEligible
            || fact.Subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.Suppressed)
        {
            return false;
        }

        return fact.Subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.HistoricalOnly
            || publicCurrentSubjects.Contains((fact.Subject.Type, fact.Subject.Id));
    }

    private static string ResolveLabel(string? label, string fallback)
    {
        return string.IsNullOrWhiteSpace(label) ? fallback : label.Trim();
    }
}
