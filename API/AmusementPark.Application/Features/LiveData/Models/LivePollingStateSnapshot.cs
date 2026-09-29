using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Models;

public sealed record LivePollingStateSnapshot(
    LiveDataSourceId SourceId,
    string ExternalEntityId,
    DateTime? NextAttemptAtUtc,
    DateTime? LastPolledAtUtc,
    DateTime? LastSuccessfulPollAtUtc,
    int ConsecutiveFailures,
    DateTime? CircuitOpenUntilUtc,
    LivePollingCompletionDisposition? LastDisposition,
    bool LeaseActive,
    DateTime? LeaseExpiresAtUtc);
