using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripParkCandidateRepository
{
    Task<IReadOnlyCollection<TripParkCandidate>> ListAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken);

    Task<TripParkCandidate?> GetAsync(
        TripPlanId tripPlanId,
        TripParkCandidateId candidateId,
        CancellationToken cancellationToken);

    Task<TripParkCandidateWriteResult> ResolveCreationAsync(
        TripPlanId tripPlanId,
        string operationId,
        string requestHash,
        CancellationToken cancellationToken);

    Task<TripParkCandidateWriteResult> CreateAsync(
        TripParkCandidate candidate,
        TripChildMutationLease lease,
        string requestHash,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken);

    Task<TripParkCandidateWriteResult> ReplaceAsync(
        TripParkCandidate candidate,
        long expectedVersion,
        TripChildMutationLease lease,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken);

    Task<TripChildWriteOutcome> ApplyOrderAsync(
        TripPlanId tripPlanId,
        TripParkCandidateOrderPlan orderPlan,
        TripChildMutationLease lease,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken);

    Task<TripParkCandidateWriteResult> DeleteAsync(
        TripPlanId tripPlanId,
        TripParkCandidateId candidateId,
        long expectedVersion,
        TripChildMutationLease lease,
        DateTime deletedAtUtc,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken);
}
