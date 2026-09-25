using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripActivityRecorder
{
    private readonly ITripAuditWriter writer;
    private readonly TimeProvider timeProvider;

    public TripActivityRecorder(ITripAuditWriter writer, TimeProvider? timeProvider = null)
    {
        this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task RecordAsync(
        TripPlan trip,
        string actorUserId,
        TripActivityKind kind,
        string operationKey,
        int affectedCount,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trip);
        TripMember actor = trip.Members.Single(member =>
            member.State == TripMembershipState.Active
            && string.Equals(member.UserId, actorUserId, StringComparison.Ordinal));
        return this.RecordAsync(
            trip.Id,
            actor.Id,
            trip.ResolveRole(actor.UserId),
            kind,
            operationKey,
            affectedCount,
            cancellationToken);
    }

    public async Task RecordAsync(
        TripPlanId tripPlanId,
        TripMemberId? actorMemberId,
        TripEffectiveRole? actorRole,
        TripActivityKind kind,
        string operationKey,
        int affectedCount,
        CancellationToken cancellationToken)
    {
        _ = await this.writer.AppendAsync(
            this.CreateWrite(
                tripPlanId,
                actorMemberId,
                actorRole,
                kind,
                operationKey,
                affectedCount),
            cancellationToken);
    }

    public TripActivityWrite CreateWrite(
        TripPlan trip,
        string actorUserId,
        TripActivityKind kind,
        string operationKey,
        int affectedCount)
    {
        ArgumentNullException.ThrowIfNull(trip);
        TripMember actor = trip.Members.Single(member =>
            member.State == TripMembershipState.Active
            && string.Equals(member.UserId, actorUserId, StringComparison.Ordinal));
        return this.CreateWrite(
            trip.Id,
            actor.Id,
            trip.ResolveRole(actor.UserId),
            kind,
            operationKey,
            affectedCount);
    }

    public TripActivityWrite CreateWrite(
        TripPlanId tripPlanId,
        TripMemberId? actorMemberId,
        TripEffectiveRole? actorRole,
        TripActivityKind kind,
        string operationKey,
        int affectedCount,
        TripChildMutationLease? childLease = null)
    {
        return new TripActivityWrite(
            tripPlanId,
            actorMemberId,
            actorRole,
            kind,
            operationKey,
            affectedCount,
            this.timeProvider.GetUtcNow().UtcDateTime,
            childLease?.OperationId,
            childLease?.ChildMutationEpoch,
            childLease?.Generation);
    }

    public Task<bool> PublishAsync(
        TripActivityWrite activity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return this.writer.AppendAsync(activity, cancellationToken);
    }

    public Task<bool> PublishReadOnlyAsync(
        TripActivityWrite activity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return this.writer.AppendReadOnlyAsync(activity, cancellationToken);
    }

    public static string RootOperationKey(TripActivityKind kind, long version)
    {
        return $"root:{kind}:{version}";
    }

    public static string IdempotentOperationKey(TripActivityKind kind, string operationId)
    {
        string normalizedOperationId = operationId?.Trim() ?? string.Empty;
        if (normalizedOperationId.Length == 0)
        {
            throw new ArgumentException("An operation identifier is required.", nameof(operationId));
        }

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedOperationId));
        return $"idempotent:{kind}:{Convert.ToHexStringLower(digest)}";
    }

    public static string ChildOperationKey(
        TripActivityKind kind,
        TripChildMutationLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        return $"child:{kind}:{lease.OperationId}:{lease.Generation}";
    }
}
