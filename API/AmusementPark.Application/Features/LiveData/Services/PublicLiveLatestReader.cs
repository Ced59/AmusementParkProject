using AmusementPark.Application.Errors;
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
    private readonly PublicLiveTargetResultFactory resultFactory;
    private readonly TimeProvider timeProvider;

    public PublicLiveLatestReader(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        ILiveLatestObservationRepository observationRepository,
        PublicLiveTargetResultFactory resultFactory,
        TimeProvider? timeProvider = null)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.observationRepository = observationRepository;
        this.resultFactory = resultFactory;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PublicLiveTargetResult>> ReadParkAsync(
        string parkId,
        CancellationToken cancellationToken)
    {
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

        IReadOnlyCollection<LiveLatestObservation> observations =
            await this.observationRepository.GetByTargetAsync(
                LiveTargetType.Park,
                normalizedParkId,
                normalizedParkId,
                cancellationToken);
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

        IReadOnlyCollection<LiveLatestObservation> observations =
            await this.observationRepository.GetByTargetAsync(
                LiveTargetType.ParkItem,
                normalizedParkItemId,
                item.ParkId,
                cancellationToken);
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
        IReadOnlyCollection<string> visibleTargetIds = items
            .Select(static item => item.Id)
            .ToList()
            .AsReadOnly();
        IReadOnlyCollection<LiveLatestObservation> observations =
            await this.observationRepository.GetParkItemsAsync(
                normalizedParkId,
                visibleTargetIds,
                cancellationToken);

        Dictionary<string, IReadOnlyCollection<LiveLatestObservation>> observationsByTarget = observations
            .GroupBy(static observation => observation.Target.Id, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyCollection<LiveLatestObservation>)group.ToList().AsReadOnly(),
                StringComparer.Ordinal);
        DateTime asOfUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        string parkDisplayName = park.Name ?? string.Empty;
        List<PublicLiveTargetResult> results = items
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
}
