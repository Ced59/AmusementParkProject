using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
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
    private readonly IHistoricalParkRolloutGateAssessmentService rolloutGateAssessmentService;

    public PublicParkHistoricalDataLoader(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IParkZoneRepository parkZoneRepository,
        IHistoricalFactRepository historicalFactRepository,
        IHistoricalParkRolloutGateAssessmentService rolloutGateAssessmentService)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.historicalFactRepository = historicalFactRepository;
        this.rolloutGateAssessmentService = rolloutGateAssessmentService;
    }

    public async Task<PublicParkHistoricalData?> LoadAsync(
        string parkId,
        CancellationToken cancellationToken)
    {
        PublicParkHistoricalScope? scope = await this.LoadScopeAsync(parkId, cancellationToken);
        if (scope is null)
        {
            return null;
        }

        (HistoricalSubject[] selectedSubjects, HistoricalFact[] publicFacts, HistoricalParkRolloutGate rolloutGate) =
            await this.LoadProjectionAsync(scope, cancellationToken);
        IReadOnlyDictionary<string, string> publicZoneNames = ResolvePublicZoneNames(
            scope,
            publicFacts);

        return new PublicParkHistoricalData(
            scope.Park,
            selectedSubjects,
            publicFacts,
            publicZoneNames,
            rolloutGate);
    }

    public async Task<HistoricalParkRolloutGate> AssessRolloutGateAsync(
        PublicParkHistoricalScope scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        (HistoricalSubject[] Subjects, HistoricalFact[] Facts, HistoricalParkRolloutGate Gate) projection =
            await this.LoadProjectionAsync(scope, cancellationToken);
        return projection.Gate;
    }

    public async Task<PublicParkHistoricalData?> LoadParkItemsAsync(
        string parkId,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken)
    {
        string normalizedParkId = parkId?.Trim() ?? string.Empty;
        ArgumentNullException.ThrowIfNull(parkItemIds);
        string[] normalizedIds = parkItemIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedParkId.Length == 0 || normalizedIds.Length == 0)
        {
            return null;
        }

        Park? park = await this.parkRepository.GetByIdAsync(
            normalizedParkId,
            false,
            cancellationToken);
        if (!HistoryPublicVisibility.IsPublicPark(park))
        {
            return null;
        }

        IReadOnlyCollection<ParkItem> loadedItems =
            await this.parkItemRepository.GetByIdsAsync(normalizedIds, cancellationToken);
        HistoricalSubject[] publicCurrentSubjects = loadedItems
            .Where(item => string.Equals(item.ParkId, normalizedParkId, StringComparison.Ordinal)
                && HistoryPublicVisibility.IsPublicParkItem(item))
            .Select(item => new HistoricalSubject(
                HistoricalSubjectType.ParkItem,
                item.Id,
                ResolveLabel(item.Name, "Park item"),
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                normalizedParkId))
            .DistinctBy(static subject => subject.Id)
            .ToArray();
        HistoricalSubject[] requestedSubjects = normalizedIds
            .Select(id => new HistoricalSubject(
                HistoricalSubjectType.ParkItem,
                id,
                "Park item",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                normalizedParkId))
            .ToArray();
        IReadOnlyCollection<HistoricalFact> latestFacts =
            await this.historicalFactRepository.GetLatestRevisionsForSubjectsAsync(
                requestedSubjects,
                cancellationToken);
        HashSet<HistoricalSubjectKey> publicCurrentSubjectKeys =
            publicCurrentSubjects
                .Select(static subject => new HistoricalSubjectKey(
                    subject.Type,
                    subject.Id,
                    subject.ContextParkId))
                .ToHashSet();
        HistoricalFact[] publicFacts = latestFacts
            .Where(fact => CanExposeFact(fact, publicCurrentSubjectKeys, normalizedParkId))
            .ToArray();

        return new PublicParkHistoricalData(
            park!,
            SelectSubjects(publicCurrentSubjects, publicFacts),
            publicFacts,
            new Dictionary<string, string>(StringComparer.Ordinal),
            null);
    }

    public async Task<PublicParkHistoricalScope?> LoadScopeAsync(
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
            false,
            cancellationToken);
        IReadOnlyCollection<ParkZone> parkZones = await this.parkZoneRepository.GetByParkIdAsync(
            park.Id,
            cancellationToken);
        HistoricalSubject[] candidateSubjects = BuildCandidateSubjects(park, parkItems, parkZones);
        HashSet<(HistoricalSubjectType Type, string Id)> publicCurrentSubjects = BuildPublicCurrentSubjectKeys(
            park,
            parkItems,
            parkZones);
        HistoricalSubject[] publicSubjects = candidateSubjects
            .Where(subject => publicCurrentSubjects.Contains((subject.Type, subject.Id)))
            .ToArray();
        Dictionary<string, string> zoneNames = parkZones
            .Where(static zone => zone.IsVisible
                && !string.IsNullOrWhiteSpace(zone.Id)
                && !string.IsNullOrWhiteSpace(zone.Name))
            .GroupBy(static zone => zone.Id, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.First().Name.Trim(),
                StringComparer.Ordinal);

        return new PublicParkHistoricalScope(park, publicSubjects, zoneNames);
    }

    public static IReadOnlyDictionary<string, string> ResolvePublicZoneNames(
        PublicParkHistoricalScope scope,
        IReadOnlyCollection<HistoricalFact> publicFacts)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(publicFacts);
        Dictionary<string, string> zoneNames = new(scope.ZoneNames, StringComparer.Ordinal);
        foreach (HistoricalSubject subject in publicFacts
                     .Where(fact => fact.IsDecisionEligible
                         && fact.Subject.Type == HistoricalSubjectType.ParkZone
                         && fact.Subject.PublicationPolicy
                             == HistoricalSubjectPublicationPolicy.HistoricalOnly
                         && string.Equals(
                             fact.Subject.ContextParkId,
                             scope.Park.Id,
                             StringComparison.Ordinal))
                     .Select(static fact => fact.Subject)
                     .DistinctBy(static subject => subject.Id))
        {
            zoneNames.TryAdd(subject.Id, subject.HistoricalLabel);
        }

        return zoneNames;
    }

    public Task<PagedResult<HistoricalFact>> GetTimelinePageAsync(
        PublicParkHistoricalScope scope,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return this.historicalFactRepository.GetLatestDecisionEligibleRevisionsForParkPageAsync(
            scope.Park.Id,
            scope.PublicCurrentSubjects,
            page,
            pageSize,
            cancellationToken);
    }

    private async Task<(HistoricalSubject[] Subjects, HistoricalFact[] Facts, HistoricalParkRolloutGate Gate)>
        LoadProjectionAsync(
            PublicParkHistoricalScope scope,
            CancellationToken cancellationToken)
    {
        IReadOnlyCollection<HistoricalFact> latestFacts =
            await this.historicalFactRepository.GetLatestDecisionEligibleRevisionsForParkAsync(
                scope.Park.Id,
                scope.PublicCurrentSubjects,
                cancellationToken);
        HashSet<HistoricalSubjectKey> publicCurrentSubjects = scope.PublicCurrentSubjects
            .Select(static subject => new HistoricalSubjectKey(
                subject.Type,
                subject.Id,
                subject.ContextParkId))
            .ToHashSet();
        HistoricalFact[] publicFacts = latestFacts
            .Where(fact => CanExposeFact(fact, publicCurrentSubjects, scope.Park.Id))
            .ToArray();
        HistoricalSubject[] selectedSubjects = SelectSubjects(
            scope.PublicCurrentSubjects,
            publicFacts);
        HistoricalParkRolloutGate rolloutGate = await this.rolloutGateAssessmentService.AssessAsync(
            scope.Park.Id,
            selectedSubjects,
            publicFacts,
            cancellationToken);
        return (selectedSubjects, publicFacts, rolloutGate);
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
                    HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                    park.Id),
            }
            .Concat(parkItems.Select(item => new HistoricalSubject(
                HistoricalSubjectType.ParkItem,
                item.Id,
                ResolveLabel(item.Name, "Park item"),
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                park.Id)))
            .Concat(parkZones.Select(zone => new HistoricalSubject(
                HistoricalSubjectType.ParkZone,
                zone.Id,
                ResolveLabel(zone.Name, "Park zone"),
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                park.Id)))
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
        IReadOnlySet<HistoricalSubjectKey> publicCurrentSubjects,
        string parkId)
    {
        if (!fact.IsDecisionEligible
            || fact.Subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.Suppressed)
        {
            return false;
        }

        return (fact.Subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.HistoricalOnly
                && string.Equals(fact.Subject.ContextParkId, parkId, StringComparison.Ordinal))
            || (fact.Subject.PublicationPolicy
                    == HistoricalSubjectPublicationPolicy.FollowCurrentSubject
                && publicCurrentSubjects.Contains(new HistoricalSubjectKey(
                    fact.Subject.Type,
                    fact.Subject.Id,
                    fact.Subject.ContextParkId)));
    }

    private static HistoricalSubject[] SelectSubjects(
        IReadOnlyCollection<HistoricalSubject> publicCurrentSubjects,
        IReadOnlyCollection<HistoricalFact> publicFacts)
    {
        Dictionary<(HistoricalSubjectType Type, string Id), HistoricalSubject> subjects =
            publicCurrentSubjects.ToDictionary(static subject => (subject.Type, subject.Id));
        foreach (HistoricalFact fact in publicFacts
                     .Where(static fact => fact.Subject.PublicationPolicy
                         == HistoricalSubjectPublicationPolicy.HistoricalOnly)
                     .OrderBy(static fact => fact.RecordedAtUtc)
                     .ThenBy(static fact => fact.Revision))
        {
            subjects[(fact.Subject.Type, fact.Subject.Id)] = fact.Subject;
        }

        return subjects.Values
            .OrderBy(static subject => subject.Type)
            .ThenBy(static subject => subject.HistoricalLabel, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static subject => subject.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ResolveLabel(string? label, string fallback)
    {
        return string.IsNullOrWhiteSpace(label) ? fallback : label.Trim();
    }
}
