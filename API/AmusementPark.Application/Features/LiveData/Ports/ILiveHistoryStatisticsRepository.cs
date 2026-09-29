using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Ports;

public interface ILiveHistoryStatisticsRepository
{
    Task<IReadOnlyCollection<LiveWaitHistoryObservation>> GetAsync(
        LiveDataSourceId sourceId,
        LiveTargetType targetType,
        string targetId,
        string usagePolicyVersion,
        string retentionPolicyKey,
        TimeSpan bucketDuration,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);
}
