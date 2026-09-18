using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripParticipantService
{
    private const string NeutralDisplayName = "—";
    private readonly ITripPlanRepository plans;
    private readonly ITripPreferenceRepository preferences;
    private readonly IUserRepository users;
    private readonly TimeProvider timeProvider;
    private readonly TripActivityRecorder? activityRecorder;

    public TripParticipantService(
        ITripPlanRepository plans,
        ITripPreferenceRepository preferences,
        IUserRepository users,
        TimeProvider? timeProvider = null,
        TripActivityRecorder? activityRecorder = null)
    {
        this.plans = plans ?? throw new ArgumentNullException(nameof(plans));
        this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        this.users = users ?? throw new ArgumentNullException(nameof(users));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.activityRecorder = activityRecorder;
    }

    public async Task<ApplicationResult<TripParticipantListResult>> ListAsync(
        string userId,
        string tripPlanId,
        CancellationToken cancellationToken)
    {
        if (!TryNormalize(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedTripId))
        {
            return NotFoundList();
        }

        TripPlan? trip = await this.plans.GetAccessibleAsync(
            normalizedUserId,
            parsedTripId,
            cancellationToken);
        return trip is null
            ? NotFoundList()
            : ApplicationResult<TripParticipantListResult>.Success(
                await this.BuildResultAsync(trip, normalizedUserId, cancellationToken));
    }

    public async Task<ApplicationResult<TripParticipantListResult>> ChangeRoleAsync(
        string userId,
        string tripPlanId,
        string memberId,
        TripDelegatedRole role,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        if (!TryNormalize(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedTripId)
            || !TryParseMember(memberId, out TripMemberId parsedMemberId)
            || expectedVersion < 1
            || !Enum.IsDefined(role))
        {
            return InvalidList();
        }

        TripPlan? trip = await this.plans.GetOwnedAsync(normalizedUserId, parsedTripId, cancellationToken);
        if (trip is null)
        {
            return NotFoundList();
        }

        if (trip.Version != expectedVersion)
        {
            return ChangedList(trip.Version);
        }

        try
        {
            trip.ChangeMemberRole(
                normalizedUserId,
                parsedMemberId,
                role,
                this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (TripPlanValidationException exception)
        {
            return ApplicationResult<TripParticipantListResult>.Failure(
                TripPlanApplicationErrors.Invalid(exception.Code, exception.Message));
        }

        TripActivityWrite? pendingActivity = this.activityRecorder?.CreateWrite(
            trip,
            normalizedUserId,
            TripActivityKind.ParticipantRoleChanged,
            TripActivityRecorder.RootOperationKey(TripActivityKind.ParticipantRoleChanged, trip.Version),
            1);
        TripPlanWriteResult write = await this.plans.ReplaceOwnedAsync(
            trip,
            expectedVersion,
            pendingActivity,
            cancellationToken);
        if (write.Outcome == TripPlanWriteOutcome.Success
            && write.PersistedTripPlan is not null
            && this.activityRecorder is not null)
        {
            await this.activityRecorder.RecordAsync(
                write.PersistedTripPlan,
                normalizedUserId,
                TripActivityKind.ParticipantRoleChanged,
                TripActivityRecorder.RootOperationKey(
                    TripActivityKind.ParticipantRoleChanged,
                    write.PersistedTripPlan.Version),
                1,
                CancellationToken.None);
        }

        return await this.MapWriteAsync(write, normalizedUserId, cancellationToken);
    }

    public async Task<ApplicationResult<TripParticipantListResult>> TransferOwnershipAsync(
        string userId,
        string tripPlanId,
        string newOwnerMemberId,
        TripDelegatedRole previousOwnerRole,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        if (!TryNormalize(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedTripId)
            || !TryParseMember(newOwnerMemberId, out TripMemberId parsedMemberId)
            || expectedVersion < 1
            || !Enum.IsDefined(previousOwnerRole))
        {
            return InvalidList();
        }

        TripPlan? trip = await this.plans.GetOwnedAsync(normalizedUserId, parsedTripId, cancellationToken);
        if (trip is null)
        {
            return NotFoundList();
        }

        if (trip.Version != expectedVersion)
        {
            return ChangedList(trip.Version);
        }

        TripMember? targetMember = trip.Members.SingleOrDefault(member =>
            member.Id == parsedMemberId && member.State == TripMembershipState.Active);
        if (targetMember is null)
        {
            return InvalidList();
        }

        User? targetUser = await this.users.GetByIdAsync(targetMember.UserId, cancellationToken);
        if (targetUser is null || !targetUser.IsActivated || targetUser.IsBlocked)
        {
            return InvalidList();
        }

        TripMember actingOwner = trip.Members.Single(member => string.Equals(
            member.UserId,
            normalizedUserId,
            StringComparison.Ordinal));
        TripEffectiveRole? actingOwnerRole = trip.ResolveRole(normalizedUserId);
        try
        {
            trip.TransferOwnership(
                normalizedUserId,
                parsedMemberId,
                previousOwnerRole,
                this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (TripPlanValidationException exception)
        {
            return ApplicationResult<TripParticipantListResult>.Failure(
                TripPlanApplicationErrors.Invalid(exception.Code, exception.Message));
        }

        TripActivityWrite? pendingActivity = this.activityRecorder?.CreateWrite(
            trip.Id,
            actingOwner.Id,
            actingOwnerRole,
            TripActivityKind.OwnershipTransferred,
            TripActivityRecorder.RootOperationKey(TripActivityKind.OwnershipTransferred, trip.Version),
            1);
        TripPlanWriteResult write = await this.plans.TransferOwnershipAsync(
            normalizedUserId,
            trip,
            expectedVersion,
            pendingActivity,
            cancellationToken);
        if (write.Outcome == TripPlanWriteOutcome.Success
            && write.PersistedTripPlan is not null
            && this.activityRecorder is not null
            && pendingActivity is not null)
        {
            await this.activityRecorder.PublishAsync(
                pendingActivity,
                CancellationToken.None);
        }

        return await this.MapWriteAsync(write, normalizedUserId, cancellationToken);
    }

    public async Task<ApplicationResult> LeaveAsync(
        string userId,
        string tripPlanId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        if (!TryNormalize(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedTripId)
            || expectedVersion < 1)
        {
            return ApplicationResult.Failure(TripPlanApplicationErrors.NotFound());
        }

        TripPlan? trip = await this.plans.GetAccessibleAsync(normalizedUserId, parsedTripId, cancellationToken);
        if (trip is null)
        {
            return ApplicationResult.Failure(TripPlanApplicationErrors.NotFound());
        }

        if (trip.Version != expectedVersion)
        {
            return ApplicationResult.Failure(TripPlanApplicationErrors.ChangedConcurrently(trip.Version));
        }

        TripMember? leavingMember = null;
        TripEffectiveRole? leavingRole = trip.ResolveRole(normalizedUserId);
        try
        {
            leavingMember = trip.BeginMemberDeparture(
                normalizedUserId,
                this.timeProvider.GetUtcNow().UtcDateTime);
            trip.RemoveLeavingMember(leavingMember.Id, this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (TripPlanValidationException exception)
        {
            return ApplicationResult.Failure(
                TripPlanApplicationErrors.Invalid(exception.Code, exception.Message));
        }

        TripActivityWrite? pendingActivity = leavingMember is null
            ? null
            : this.activityRecorder?.CreateWrite(
                parsedTripId,
                leavingMember.Id,
                leavingRole,
                TripActivityKind.ParticipantLeft,
                TripActivityRecorder.RootOperationKey(TripActivityKind.ParticipantLeft, trip.Version),
                1);
        TripPlanWriteResult write = await this.plans.ReplaceAccessibleAsync(
            normalizedUserId,
            trip,
            expectedVersion,
            pendingActivity,
            cancellationToken);
        if (write.Outcome == TripPlanWriteOutcome.Success)
        {
            await this.preferences.CompleteDepartureCleanupAsync(
                parsedTripId,
                normalizedUserId,
                CancellationToken.None);
            if (this.activityRecorder is not null && leavingMember is not null)
            {
                await this.activityRecorder.RecordAsync(
                    parsedTripId,
                    leavingMember.Id,
                    leavingRole,
                    TripActivityKind.ParticipantLeft,
                    TripActivityRecorder.RootOperationKey(
                        TripActivityKind.ParticipantLeft,
                        trip.Version),
                    1,
                    CancellationToken.None);
            }

            return ApplicationResult.Success();
        }

        return write.Outcome switch
        {
            TripPlanWriteOutcome.NotFound => ApplicationResult.Failure(TripPlanApplicationErrors.NotFound()),
            _ => ApplicationResult.Failure(
                TripPlanApplicationErrors.ChangedConcurrently(write.CurrentVersion)),
        };
    }

    private async Task<ApplicationResult<TripParticipantListResult>> MapWriteAsync(
        TripPlanWriteResult write,
        string currentUserId,
        CancellationToken cancellationToken)
    {
        if (write.Outcome == TripPlanWriteOutcome.NotFound)
        {
            return NotFoundList();
        }

        if (write.Outcome != TripPlanWriteOutcome.Success || write.PersistedTripPlan is null)
        {
            return ChangedList(write.CurrentVersion);
        }

        return ApplicationResult<TripParticipantListResult>.Success(
            await this.BuildResultAsync(write.PersistedTripPlan, currentUserId, cancellationToken));
    }

    private async Task<TripParticipantListResult> BuildResultAsync(
        TripPlan trip,
        string currentUserId,
        CancellationToken cancellationToken)
    {
        TripMember[] activeMembers = trip.Members
            .Where(static member => member.State == TripMembershipState.Active)
            .ToArray();
        IReadOnlyCollection<User> resolvedUsers = await this.users.GetByIdsAsync(
            activeMembers.Select(static member => member.UserId).ToArray(),
            cancellationToken);
        IReadOnlyDictionary<string, User> usersById = resolvedUsers.ToDictionary(
            static user => user.Id,
            StringComparer.Ordinal);
        TripParticipantResult[] participants = activeMembers
            .Select(member => new TripParticipantResult(
                member.Id.Value,
                ResolveDisplayName(usersById, member.UserId),
                trip.ResolveRole(member.UserId)
                    ?? throw new InvalidOperationException("An active trip member must have an effective role."),
                string.Equals(member.UserId, currentUserId, StringComparison.Ordinal),
                member.JoinedAtUtc))
            .OrderByDescending(static participant => participant.Role == TripEffectiveRole.Owner)
            .ThenBy(static participant => participant.JoinedAtUtc)
            .ThenBy(static participant => participant.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        TripEffectiveRole role = trip.ResolveRole(currentUserId)
            ?? throw new InvalidOperationException("An accessible trip must contain the current active member.");
        return new TripParticipantListResult(
            participants,
            TripAuthorizationPolicy.HasPermission(role, TripPermission.ChangeRoles),
            role == TripEffectiveRole.Owner && participants.Length > 1,
            TripAuthorizationPolicy.HasPermission(role, TripPermission.Leave),
            trip.Version);
    }

    private static string ResolveDisplayName(IReadOnlyDictionary<string, User> users, string userId)
    {
        return users.TryGetValue(userId, out User? user)
            ? user.ResolvePublicDisplayName() ?? NeutralDisplayName
            : NeutralDisplayName;
    }

    private static bool TryNormalize(
        string userId,
        string tripPlanId,
        out string normalizedUserId,
        out TripPlanId parsedTripId)
    {
        normalizedUserId = string.Empty;
        parsedTripId = default;
        try
        {
            normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
            return TripPlanId.TryParse(tripPlanId, out parsedTripId);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryParseMember(string memberId, out TripMemberId parsed)
    {
        parsed = default;
        try
        {
            parsed = TripMemberId.Parse(memberId);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static ApplicationResult<TripParticipantListResult> NotFoundList()
    {
        return ApplicationResult<TripParticipantListResult>.Failure(TripPlanApplicationErrors.NotFound());
    }

    private static ApplicationResult<TripParticipantListResult> InvalidList()
    {
        return ApplicationResult<TripParticipantListResult>.Failure(
            TripPlanApplicationErrors.Invalid(
                TripPlanErrorCodes.InvalidState,
                "The participant action is invalid."));
    }

    private static ApplicationResult<TripParticipantListResult> ChangedList(long? version)
    {
        return ApplicationResult<TripParticipantListResult>.Failure(
            TripPlanApplicationErrors.ChangedConcurrently(version));
    }
}
