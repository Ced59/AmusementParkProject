using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripChildMutationExecutor
{
    private readonly ITripChildMutationLeaseRepository leaseRepository;
    private readonly ILogger<TripChildMutationExecutor> logger;

    public TripChildMutationExecutor(
        ITripChildMutationLeaseRepository leaseRepository,
        ILogger<TripChildMutationExecutor> logger)
    {
        this.leaseRepository = leaseRepository
            ?? throw new ArgumentNullException(nameof(leaseRepository));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        return await this.ExecuteWithLeaseAsync(
            trip.Id,
            lease,
            () => action(lease));
    }

    public async Task<ApplicationResult<TResult>> ExecuteAccessibleAsync<TResult>(
        TripPlan trip,
        string actorUserId,
        TripPermission requiredPermission,
        string operationId,
        Func<TripChildMutationLease, Task<ApplicationResult<TResult>>> action,
        CancellationToken cancellationToken)
    {
        TripChildMutationLease? lease = await this.TryAcquireAccessibleAsync(
            trip,
            actorUserId,
            requiredPermission,
            operationId,
            cancellationToken);
        if (lease is null)
        {
            return ApplicationResult<TResult>.Failure(
                TripPlanApplicationErrors.ChildMutationUnavailable());
        }

        return await this.ExecuteWithLeaseAsync(
            trip.Id,
            lease,
            () => action(lease));
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

        return await this.ExecuteWithLeaseAsync(
            trip.Id,
            lease,
            () => action(lease));
    }

    public async Task<ApplicationResult> ExecuteAccessibleAsync(
        TripPlan trip,
        string actorUserId,
        TripPermission requiredPermission,
        string operationId,
        Func<TripChildMutationLease, Task<ApplicationResult>> action,
        CancellationToken cancellationToken)
    {
        TripChildMutationLease? lease = await this.TryAcquireAccessibleAsync(
            trip,
            actorUserId,
            requiredPermission,
            operationId,
            cancellationToken);
        if (lease is null)
        {
            return ApplicationResult.Failure(TripPlanApplicationErrors.ChildMutationUnavailable());
        }

        return await this.ExecuteWithLeaseAsync(
            trip.Id,
            lease,
            () => action(lease));
    }

    private async Task<TResult> ExecuteWithLeaseAsync<TResult>(
        TripPlanId tripPlanId,
        TripChildMutationLease lease,
        Func<Task<TResult>> action)
    {
        bool outcomeIsKnown = true;
        try
        {
            return await action();
        }
        catch (OperationCanceledException)
        {
            outcomeIsKnown = false;
            throw;
        }
        catch (TimeoutException)
        {
            outcomeIsKnown = false;
            throw;
        }
        finally
        {
            if (outcomeIsKnown)
            {
                await this.ReleaseBestEffortAsync(tripPlanId, lease);
            }
        }
    }

    private async Task ReleaseBestEffortAsync(
        TripPlanId tripPlanId,
        TripChildMutationLease lease)
    {
        try
        {
            await this.leaseRepository.ReleaseAsync(tripPlanId, lease, CancellationToken.None);
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(
                exception,
                "Trip child mutation lease {OperationId} generation {Generation} could not be released; it will expire automatically.",
                lease.OperationId,
                lease.Generation);
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

    private Task<TripChildMutationLease?> TryAcquireAccessibleAsync(
        TripPlan trip,
        string actorUserId,
        TripPermission requiredPermission,
        string operationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trip);
        TripEffectiveRole? role = trip.ResolveRole(actorUserId);
        if (!role.HasValue || !TripAuthorizationPolicy.HasPermission(role.Value, requiredPermission))
        {
            return Task.FromResult<TripChildMutationLease?>(null);
        }

        TripMember actor = trip.Members.Single(member => member.State == TripMembershipState.Active
            && string.Equals(member.UserId, actorUserId, StringComparison.Ordinal));
        return this.leaseRepository.TryAcquireAccessibleAsync(
            trip.Id,
            actorUserId,
            actor.Id,
            trip.Version,
            trip.ChildMutationEpoch,
            operationId,
            cancellationToken);
    }
}
