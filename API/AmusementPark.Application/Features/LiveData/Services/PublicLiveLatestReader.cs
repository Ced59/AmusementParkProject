using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class PublicLiveLatestReader
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly ILiveLatestObservationRepository observationRepository;
    private readonly ILiveTargetMappingRepository mappingRepository;
    private readonly ILiveDataSourceCatalog sourceCatalog;
    private readonly PublicLiveTargetResultFactory resultFactory;
    private readonly TimeProvider timeProvider;

    public PublicLiveLatestReader(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        ILiveLatestObservationRepository observationRepository,
        ILiveTargetMappingRepository mappingRepository,
        ILiveDataSourceCatalog sourceCatalog,
        PublicLiveTargetResultFactory resultFactory,
        TimeProvider? timeProvider = null)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.observationRepository = observationRepository;
        this.mappingRepository = mappingRepository;
        this.sourceCatalog = sourceCatalog;
        this.resultFactory = resultFactory;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PublicLiveTargetResult>> ReadParkAsync(
        string parkId,
        CancellationToken cancellationToken)
    {
        if (!this.sourceCatalog.IsPublicReadEnabled)
        {
            return ApplicationResult<PublicLiveTargetResult>.Failure(
                LiveDataApplicationErrors.PublicReadDisabled());
        }

        LivePollingTarget? publicTarget = this.sourceCatalog.PublicPollingTarget;
        if (publicTarget is null)
        {
            return ApplicationResult<PublicLiveTargetResult>.Failure(
                LiveDataApplicationErrors.PublicReadDisabled());
        }

        string? normalizedParkId = NormalizeIdentifier(parkId);
        if (normalizedParkId is null)
        {
            return ApplicationResult<PublicLiveTargetResult>.Failure(
                ApplicationErrors.Required("parkId"));
        }

        Park? park = await this.parkRepository.GetByIdAsync(
            normalizedParkId,
            false,
            cancellationToken);
        if (park is null)
        {
            return ApplicationResult<PublicLiveTargetResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(Park), normalizedParkId));
        }

        IReadOnlyCollection<LivePublicTargetCoverage> coverage =
            await this.mappingRepository.GetEligiblePublicTargetCoverageByParkAsync(
                publicTarget.SourceId,
                publicTarget.ExternalEntityId,
                normalizedParkId,
                cancellationToken);
        if (coverage.Count == 0)
        {
            return ApplicationResult<PublicLiveTargetResult>.Failure(
                LiveDataApplicationErrors.PublicReadDisabled());
        }

        IReadOnlyCollection<LiveLatestObservation> observations =
            await this.observationRepository.GetByTargetAsync(
                LiveTargetType.Park,
                normalizedParkId,
                normalizedParkId,
                cancellationToken);
        observations = FilterCoveredObservations(
            observations,
            publicTarget.SourceId,
            BuildCoverageByInternalTarget(coverage));
        DateTime asOfUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        PublicLiveTargetResult result = this.resultFactory.Create(
            normalizedParkId,
            LiveTargetType.Park,
            park.Name ?? string.Empty,
            normalizedParkId,
            park.Name ?? string.Empty,
            observations,
            asOfUtc);
        return ApplicationResult<PublicLiveTargetResult>.Success(result);
    }

    public async Task<ApplicationResult<PublicLiveTargetResult>> ReadParkItemAsync(
        string parkItemId,
        CancellationToken cancellationToken)
    {
        if (!this.sourceCatalog.IsPublicReadEnabled)
        {
            return ApplicationResult<PublicLiveTargetResult>.Failure(
                LiveDataApplicationErrors.PublicReadDisabled());
        }

        LivePollingTarget? publicTarget = this.sourceCatalog.PublicPollingTarget;
        if (publicTarget is null)
        {
            return ApplicationResult<PublicLiveTargetResult>.Failure(
                LiveDataApplicationErrors.PublicReadDisabled());
        }

        string? normalizedParkItemId = NormalizeIdentifier(parkItemId);
        if (normalizedParkItemId is null)
        {
            return ApplicationResult<PublicLiveTargetResult>.Failure(
                ApplicationErrors.Required("parkItemId"));
        }

        ParkItem? item = await this.parkItemRepository.GetByIdAsync(
            normalizedParkItemId,
            false,
            cancellationToken);
        if (item is null)
        {
            return ApplicationResult<PublicLiveTargetResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(ParkItem), normalizedParkItemId));
        }

        Park? park = await this.parkRepository.GetByIdAsync(item.ParkId, false, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<PublicLiveTargetResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(ParkItem), normalizedParkItemId));
        }

        IReadOnlyCollection<LivePublicTargetCoverage> coverage =
            await this.mappingRepository.GetEligiblePublicTargetCoverageByParkAsync(
                publicTarget.SourceId,
                publicTarget.ExternalEntityId,
                item.ParkId,
                cancellationToken);
        Dictionary<string, HashSet<string>> coverageByInternalTarget =
            BuildCoverageByInternalTarget(coverage);
        if (!coverageByInternalTarget.ContainsKey(normalizedParkItemId))
        {
            return ApplicationResult<PublicLiveTargetResult>.Failure(
                LiveDataApplicationErrors.PublicReadDisabled());
        }

        IReadOnlyCollection<LiveLatestObservation> observations =
            await this.observationRepository.GetByTargetAsync(
                LiveTargetType.ParkItem,
                normalizedParkItemId,
                item.ParkId,
                cancellationToken);
        observations = FilterCoveredObservations(
            observations,
            publicTarget.SourceId,
            coverageByInternalTarget);
        DateTime asOfUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        PublicLiveTargetResult result = this.resultFactory.Create(
            normalizedParkItemId,
            LiveTargetType.ParkItem,
            item.Name,
            park.Id,
            park.Name ?? string.Empty,
            observations,
            asOfUtc);
        return ApplicationResult<PublicLiveTargetResult>.Success(result);
    }

    public async Task<ApplicationResult<PublicParkLiveItemsResult>> ReadParkItemsAsync(
        string parkId,
        CancellationToken cancellationToken)
    {
        if (!this.sourceCatalog.IsPublicReadEnabled)
        {
            return ApplicationResult<PublicParkLiveItemsResult>.Failure(
                LiveDataApplicationErrors.PublicReadDisabled());
        }

        LivePollingTarget? publicTarget = this.sourceCatalog.PublicPollingTarget;
        if (publicTarget is null)
        {
            return ApplicationResult<PublicParkLiveItemsResult>.Failure(
                LiveDataApplicationErrors.PublicReadDisabled());
        }

        string? normalizedParkId = NormalizeIdentifier(parkId);
        if (normalizedParkId is null)
        {
            return ApplicationResult<PublicParkLiveItemsResult>.Failure(
                ApplicationErrors.Required("parkId"));
        }

        Park? park = await this.parkRepository.GetByIdAsync(
            normalizedParkId,
            false,
            cancellationToken);
        if (park is null)
        {
            return ApplicationResult<PublicParkLiveItemsResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(Park), normalizedParkId));
        }

        IReadOnlyCollection<ParkItem> items = await this.parkItemRepository.GetByParkIdAsync(
            normalizedParkId,
            false,
            cancellationToken);
        IReadOnlyCollection<LivePublicTargetCoverage> coverage =
            await this.mappingRepository.GetEligiblePublicTargetCoverageByParkAsync(
                publicTarget.SourceId,
                publicTarget.ExternalEntityId,
                normalizedParkId,
                cancellationToken);
        Dictionary<string, HashSet<string>> coverageByInternalTarget =
            BuildCoverageByInternalTarget(coverage);
        ParkItem[] coveredItems = items
            .Where(item => coverageByInternalTarget.ContainsKey(item.Id))
            .ToArray();
        IReadOnlyCollection<string> visibleTargetIds = coveredItems
            .Select(static item => item.Id)
            .ToList()
            .AsReadOnly();
        IReadOnlyCollection<LiveLatestObservation> observations =
            await this.observationRepository.GetParkItemsAsync(
                normalizedParkId,
                visibleTargetIds,
                cancellationToken);
        observations = FilterCoveredObservations(
            observations,
            publicTarget.SourceId,
            coverageByInternalTarget);

        Dictionary<string, IReadOnlyCollection<LiveLatestObservation>> observationsByTarget = observations
            .GroupBy(static observation => observation.Target.Id, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyCollection<LiveLatestObservation>)group.ToList().AsReadOnly(),
                StringComparer.Ordinal);
        DateTime asOfUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        string parkDisplayName = park.Name ?? string.Empty;
        List<PublicLiveTargetResult> results = coveredItems
            .OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static item => item.Id, StringComparer.Ordinal)
            .Select(item => this.resultFactory.Create(
                item.Id,
                LiveTargetType.ParkItem,
                item.Name,
                normalizedParkId,
                parkDisplayName,
                observationsByTarget.TryGetValue(item.Id, out IReadOnlyCollection<LiveLatestObservation>? itemObservations)
                    ? itemObservations
                    : Array.Empty<LiveLatestObservation>(),
                asOfUtc))
            .ToList();
        return ApplicationResult<PublicParkLiveItemsResult>.Success(
            new PublicParkLiveItemsResult(
                normalizedParkId,
                parkDisplayName,
                asOfUtc,
                results.AsReadOnly()));
    }

    private static string? NormalizeIdentifier(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static Dictionary<string, HashSet<string>> BuildCoverageByInternalTarget(
        IReadOnlyCollection<LivePublicTargetCoverage> coverage)
    {
        return coverage
            .GroupBy(static item => item.InternalTargetId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .Select(static item => item.ExternalTargetId)
                    .ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);
    }

    private static IReadOnlyCollection<LiveLatestObservation> FilterCoveredObservations(
        IReadOnlyCollection<LiveLatestObservation> observations,
        LiveDataSourceId sourceId,
        IReadOnlyDictionary<string, HashSet<string>> coverageByInternalTarget)
    {
        return observations
            .Where(observation => observation.Provenance.SourceId == sourceId
                && coverageByInternalTarget.TryGetValue(
                    observation.Target.Id,
                    out HashSet<string>? externalTargetIds)
                && externalTargetIds.Contains(observation.Provenance.ExternalTargetId))
            .ToList()
            .AsReadOnly();
    }
}
