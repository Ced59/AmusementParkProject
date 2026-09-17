using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripChildMutationExecutor
{
    private readonly ITripChildMutationLeaseRepository leaseRepository;

    public TripChildMutationExecutor(ITripChildMutationLeaseRepository leaseRepository)
    {
        this.leaseRepository = leaseRepository
            ?? throw new ArgumentNullException(nameof(leaseRepository));
    }

    public async Task<ApplicationResult<TResult>> ExecuteOwnedAsync<TResult>(
        TripPlan trip,
        string operationId,
        Func<TripChildMutationLease, Task<ApplicationResult<TResult>>> action,
        CancellationToken cancellationToken)
    {
        TripChildMutationLease? lease = await this.TryAcquireOwnedAsync(
            trip,
            operationId,
            cancellationToken);
        if (lease is null)
        {
            return ApplicationResult<TResult>.Failure(
                TripPlanApplicationErrors.ChildMutationUnavailable());
        }

        try
        {
            return await action(lease);
        }
        finally
        {
            await this.leaseRepository.ReleaseAsync(trip.Id, lease, CancellationToken.None);
        }
    }

    public async Task<ApplicationResult> ExecuteOwnedAsync(
        TripPlan trip,
        string operationId,
        Func<TripChildMutationLease, Task<ApplicationResult>> action,
        CancellationToken cancellationToken)
    {
        TripChildMutationLease? lease = await this.TryAcquireOwnedAsync(
            trip,
            operationId,
            cancellationToken);
        if (lease is null)
        {
            return ApplicationResult.Failure(TripPlanApplicationErrors.ChildMutationUnavailable());
        }

        try
        {
            return await action(lease);
        }
        finally
        {
            await this.leaseRepository.ReleaseAsync(trip.Id, lease, CancellationToken.None);
        }
    }

    private Task<TripChildMutationLease?> TryAcquireOwnedAsync(
        TripPlan trip,
        string operationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trip);
        TripMember owner = trip.Members.Single(member => member.State == TripMembershipState.Active
            && string.Equals(member.UserId, trip.OwnerUserId, StringComparison.Ordinal));
        return this.leaseRepository.TryAcquireOwnedAsync(
            trip.Id,
            trip.OwnerUserId,
            owner.Id,
            trip.Version,
            trip.ChildMutationEpoch,
            operationId,
            cancellationToken);
    }
}
