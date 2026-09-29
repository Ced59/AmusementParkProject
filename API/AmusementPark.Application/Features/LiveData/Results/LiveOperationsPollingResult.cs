using AmusementPark.Application.Features.LiveData.Models;

namespace AmusementPark.Application.Features.LiveData.Results;

public sealed record LiveOperationsPollingResult(
    string ExternalEntityId,
    DateTime? NextAttemptAtUtc,
    DateTime? LastPolledAtUtc,
    DateTime? LastSuccessfulPollAtUtc,
    int ConsecutiveFailures,
    DateTime? CircuitOpenUntilUtc,
    LivePollingCompletionDisposition? LastDisposition,
    bool LeaseActive,
    DateTime? LeaseExpiresAtUtc);
