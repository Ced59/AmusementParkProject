using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Ports;

public interface ILiveTargetMappingRepository
{
    Task<ExternalLiveTargetMapping?> GetLatestByIdAsync(
        Guid mappingId,
        CancellationToken cancellationToken);

    Task<ExternalLiveTargetMapping?> GetLatestByNaturalKeyAsync(
        LiveDataSourceId sourceId,
        string externalTargetId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ExternalLiveTargetMapping>> GetLatestByExternalTargetIdsAsync(
        LiveDataSourceId sourceId,
        IReadOnlyCollection<string> externalTargetIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ExternalLiveTargetMapping>> GetLatestByExternalEntityAsync(
        LiveDataSourceId sourceId,
        string externalEntityId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LivePublicTargetCoverage>> GetEligiblePublicTargetCoverageByParkAsync(
        LiveDataSourceId sourceId,
        string externalEntityId,
        string internalParkId,
        CancellationToken cancellationToken);

    Task<PagedResult<ExternalLiveTargetMapping>> SearchLatestAsync(
        LiveTargetMappingSearchCriteria criteria,
        CancellationToken cancellationToken);

    Task<LiveTargetMappingWriteOutcome> AppendRevisionAsync(
        ExternalLiveTargetMapping mapping,
        int expectedRevision,
        CancellationToken cancellationToken);
}
