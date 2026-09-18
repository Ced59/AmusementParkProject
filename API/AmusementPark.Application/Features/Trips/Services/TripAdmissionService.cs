using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripAdmissionService
{
    private readonly ITripAdmissionRepository repository;
    private readonly ITripPlanRepository tripPlanRepository;
    private readonly ITripInvitationSecurity security;
    private readonly IUserRepository users;
    private readonly TimeProvider timeProvider;
    private readonly TripActivityRecorder? activityRecorder;

    public TripAdmissionService(
        ITripAdmissionRepository repository,
        ITripPlanRepository tripPlanRepository,
        ITripInvitationSecurity security,
        IUserRepository users,
        TimeProvider? timeProvider = null,
        TripActivityRecorder? activityRecorder = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.tripPlanRepository = tripPlanRepository ?? throw new ArgumentNullException(nameof(tripPlanRepository));
        this.security = security ?? throw new ArgumentNullException(nameof(security));
        this.users = users ?? throw new ArgumentNullException(nameof(users));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.activityRecorder = activityRecorder;
    }

    public async Task<ApplicationResult<TripInvitationDecisionResult>> AcceptAsync(
        string userId,
        string token,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeRequest(
            userId,
            token,
            clientOperationId,
            out string normalizedUserId,
            out string tokenHash,
            out string operationKeyHash))
        {
            return NotFoundDecision();
        }

        TripInvitation? invitation = await this.repository.GetInvitationByTokenHashAsync(
            tokenHash,
            cancellationToken);
        if (invitation is null)
        {
            return NotFoundDecision();
        }

        string operationId = operationKeyHash;
        if (invitation.Status == TripInvitationStatus.Accepting
            || invitation.Status == TripInvitationStatus.Accepted)
        {
            if (!string.Equals(invitation.AcceptingUserId, normalizedUserId, StringComparison.Ordinal)
                || !string.Equals(invitation.AcceptanceOperationId, operationId, StringComparison.Ordinal)
                || !invitation.AcceptanceGeneration.HasValue
                || !invitation.AcceptanceLeaseExpiresAtUtc.HasValue)
            {
                return ApplicationResult<TripInvitationDecisionResult>.Failure(
                    TripInvitationApplicationErrors.ChangedConcurrently());
            }

            TripMemberAdmissionFence resumedFence = RestoreFence(invitation);
            if (invitation.Status == TripInvitationStatus.Accepted)
            {
                TripPlan? establishedTrip = await this.tripPlanRepository.GetAccessibleAsync(
                    normalizedUserId,
                    invitation.TripPlanId,
                    cancellationToken);
                if (HasEstablishedAdmission(establishedTrip, resumedFence))
                {
                    await this.repository.MarkInvitationAdmissionCompletedAsync(
                        invitation.Id,
                        resumedFence,
                        cancellationToken);
                    await this.RecordAdmissionAsync(
                        invitation,
                        resumedFence,
                        TripActivityKind.InvitationAccepted);
                    return ApplicationResult<TripInvitationDecisionResult>.Success(
                        new TripInvitationDecisionResult(invitation.TripPlanId.Value, true));
                }
            }

            return await this.ResumeAcceptanceAsync(
                invitation,
                resumedFence,
                true,
                cancellationToken);
        }

        if (!invitation.IsPubliclyResolvable(this.timeProvider.GetUtcNow().UtcDateTime))
        {
            return NotFoundDecision();
        }

        User? user = await this.users.GetByIdAsync(normalizedUserId, cancellationToken);
        if (!CanUseInvitation(invitation, user))
        {
            return ApplicationResult<TripInvitationDecisionResult>.Failure(
                TripInvitationApplicationErrors.RecipientMismatch());
        }

        TripAdmissionFenceWriteResult prepared = await this.repository.PrepareFenceAsync(
            invitation.TripPlanId,
            invitation.Id,
            normalizedUserId,
            operationId,
            cancellationToken);
        if (prepared.Outcome is not TripAdmissionWriteOutcome.Success
            and not TripAdmissionWriteOutcome.AlreadyCompleted
            || prepared.Fence is null)
        {
            return ApplicationResult<TripInvitationDecisionResult>.Failure(
                prepared.Outcome == TripAdmissionWriteOutcome.MemberLimitReached
                    ? TripInvitationApplicationErrors.MemberLimitReached()
                    : TripInvitationApplicationErrors.AdmissionUnavailable());
        }

        TripAdmissionWriteOutcome reserved = await this.repository.ReserveInvitationAsync(
            invitation,
            normalizedUserId,
            operationId,
            operationKeyHash,
            prepared.Fence,
            cancellationToken);
        if (reserved is not TripAdmissionWriteOutcome.Success
            and not TripAdmissionWriteOutcome.AlreadyCompleted)
        {
            _ = await this.repository.CancelFenceAsync(
                invitation.TripPlanId,
                prepared.Fence,
                cancellationToken);
            return ApplicationResult<TripInvitationDecisionResult>.Failure(
                TripInvitationApplicationErrors.ChangedConcurrently());
        }

        return await this.ResumeAcceptanceAsync(
            invitation,
            prepared.Fence,
            false,
            cancellationToken);
    }

    public async Task<ApplicationResult<TripInvitationDecisionResult>> DeclineAsync(
        string userId,
        string token,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeRequest(
            userId,
            token,
            clientOperationId,
            out string normalizedUserId,
            out string tokenHash,
            out string operationKeyHash))
        {
            return NotFoundDecision();
        }

        TripInvitation? invitation = await this.repository.GetInvitationByTokenHashAsync(
            tokenHash,
            cancellationToken);
        if (invitation is null)
        {
            return NotFoundDecision();
        }

        User? user = await this.users.GetByIdAsync(normalizedUserId, cancellationToken);
        if (!CanUseInvitation(invitation, user))
        {
            return ApplicationResult<TripInvitationDecisionResult>.Failure(
                TripInvitationApplicationErrors.RecipientMismatch());
        }

        if (invitation.Status != TripInvitationStatus.Declined
            && !invitation.IsPubliclyResolvable(this.timeProvider.GetUtcNow().UtcDateTime))
        {
            return NotFoundDecision();
        }

        TripAdmissionWriteOutcome outcome = await this.repository.DeclineInvitationAsync(
            invitation,
            normalizedUserId,
            operationKeyHash,
            cancellationToken);
        if (outcome is not TripAdmissionWriteOutcome.Success
            and not TripAdmissionWriteOutcome.AlreadyCompleted)
        {
            return ApplicationResult<TripInvitationDecisionResult>.Failure(
                TripInvitationApplicationErrors.ChangedConcurrently());
        }

        if (this.activityRecorder is not null)
        {
            await this.activityRecorder.RecordAsync(
                invitation.TripPlanId,
                null,
                null,
                TripActivityKind.InvitationDeclined,
                $"invitation:decline:{operationKeyHash}",
                1,
                CancellationToken.None);
        }

        return ApplicationResult<TripInvitationDecisionResult>.Success(
            new TripInvitationDecisionResult(
                invitation.TripPlanId.Value,
                outcome == TripAdmissionWriteOutcome.AlreadyCompleted));
    }

    internal async Task<bool> ReconcileAsync(
        TripInvitation invitation,
        CancellationToken cancellationToken)
    {
        if (invitation.Status is not TripInvitationStatus.Accepting
            and not TripInvitationStatus.Accepted
            || !invitation.AcceptanceGeneration.HasValue
            || !invitation.AcceptanceLeaseExpiresAtUtc.HasValue
            || string.IsNullOrWhiteSpace(invitation.AcceptanceOperationId)
            || string.IsNullOrWhiteSpace(invitation.AcceptingUserId))
        {
            return true;
        }

        TripMemberAdmissionFence fence = RestoreFence(invitation);
        if (invitation.Status == TripInvitationStatus.Accepted)
        {
            TripPlan? establishedTrip = await this.tripPlanRepository.GetAccessibleAsync(
                invitation.AcceptingUserId,
                invitation.TripPlanId,
                cancellationToken);
            if (HasEstablishedAdmission(establishedTrip, fence))
            {
                await this.repository.MarkInvitationAdmissionCompletedAsync(
                    invitation.Id,
                    fence,
                    cancellationToken);
                return true;
            }
        }

        if (this.timeProvider.GetUtcNow().UtcDateTime >= fence.LeaseExpiresAtUtc)
        {
            TripAdmissionWriteOutcome cancelled = await this.repository.CancelExpiredFenceAsync(
                invitation.TripPlanId,
                fence,
                cancellationToken);
            if (cancelled == TripAdmissionWriteOutcome.Success)
            {
                await this.repository.CancelInvitationAcceptanceAsync(
                    invitation.Id,
                    fence,
                    cancellationToken);
                return true;
            }

            return cancelled == TripAdmissionWriteOutcome.AlreadyCompleted;
        }

        ApplicationResult<TripInvitationDecisionResult> result = await this.ResumeAcceptanceAsync(
            invitation,
            fence,
            true,
            cancellationToken);
        return result.IsSuccess;
    }

    private static bool HasEstablishedAdmission(
        TripPlan? trip,
        TripMemberAdmissionFence fence)
    {
        return trip?.Members.Any(member =>
            member.State == TripMembershipState.Active
            && string.Equals(member.UserId, fence.CandidateUserId, StringComparison.Ordinal)
            && string.Equals(
                member.AdmissionOperationId,
                fence.OperationId,
                StringComparison.Ordinal)) == true;
    }

    private async Task<ApplicationResult<TripInvitationDecisionResult>> ResumeAcceptanceAsync(
        TripInvitation invitation,
        TripMemberAdmissionFence fence,
        bool wasReplayed,
        CancellationToken cancellationToken)
    {
        TripAdmissionWriteOutcome armed = await this.repository.ArmFenceAsync(
            invitation.TripPlanId,
            fence,
            cancellationToken);
        if (!CanContinue(armed))
        {
            return AdmissionUnavailable();
        }

        TripAdmissionWriteOutcome applied = await this.repository.ApplyProvisionalMemberAsync(
            invitation.TripPlanId,
            fence,
            invitation.ProposedRole,
            cancellationToken);
        if (!CanContinue(applied))
        {
            return AdmissionUnavailable();
        }

        TripAdmissionWriteOutcome accepted = await this.repository.MarkInvitationAcceptedAsync(
            invitation.Id,
            fence,
            cancellationToken);
        if (!CanContinue(accepted))
        {
            return AdmissionUnavailable();
        }

        TripAdmissionWriteOutcome established = await this.repository.EstablishMemberAsync(
            invitation.TripPlanId,
            fence,
            cancellationToken);
        if (!CanContinue(established))
        {
            return AdmissionUnavailable();
        }

        await this.repository.MarkInvitationAdmissionCompletedAsync(
            invitation.Id,
            fence,
            cancellationToken);
        await this.RecordAdmissionAsync(
            invitation,
            fence,
            TripActivityKind.InvitationAccepted);
        return ApplicationResult<TripInvitationDecisionResult>.Success(
            new TripInvitationDecisionResult(invitation.TripPlanId.Value, wasReplayed));
    }

    private async Task RecordAdmissionAsync(
        TripInvitation invitation,
        TripMemberAdmissionFence fence,
        TripActivityKind kind)
    {
        if (this.activityRecorder is null)
        {
            return;
        }

        TripPlan? trip = await this.tripPlanRepository.GetAccessibleAsync(
            fence.CandidateUserId,
            invitation.TripPlanId,
            CancellationToken.None);
        TripMember? member = trip?.Members.SingleOrDefault(candidate =>
            candidate.State == TripMembershipState.Active
            && string.Equals(candidate.UserId, fence.CandidateUserId, StringComparison.Ordinal));
        if (member is null)
        {
            return;
        }

        await this.activityRecorder.RecordAsync(
            invitation.TripPlanId,
            member.Id,
            trip!.ResolveRole(fence.CandidateUserId),
            kind,
            $"invitation:accept:{fence.OperationId}",
            1,
            CancellationToken.None);
    }

    private bool TryNormalizeRequest(
        string userId,
        string token,
        string clientOperationId,
        out string normalizedUserId,
        out string tokenHash,
        out string operationKeyHash)
    {
        normalizedUserId = string.Empty;
        tokenHash = string.Empty;
        operationKeyHash = string.Empty;
        string normalizedOperationId = clientOperationId?.Trim() ?? string.Empty;
        try
        {
            normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (normalizedOperationId.Length is 0 or > TripInvitationService.MaximumIdempotencyKeyLength
            || !this.security.TryHashPublicToken(token, out tokenHash))
        {
            return false;
        }

        operationKeyHash = this.security.HashOperationKey(normalizedUserId, normalizedOperationId);
        return true;
    }

    private bool CanUseInvitation(TripInvitation invitation, User? user)
    {
        if (user is null || !user.IsActivated || user.IsBlocked)
        {
            return false;
        }

        if (!invitation.IsTargeted)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(user.Email)
            && this.security.MatchesEmailFingerprint(
                user.Email.Trim(),
                invitation.TargetEmailHmac!,
                invitation.TargetEmailHmacKeyVersion!);
    }

    private static TripMemberAdmissionFence RestoreFence(TripInvitation invitation)
    {
        return TripMemberAdmissionFence.Restore(
            invitation.Id,
            invitation.AcceptanceOperationId!,
            invitation.AcceptingUserId!,
            invitation.AcceptanceGeneration!.Value,
            invitation.AcceptanceLeaseExpiresAtUtc!.Value,
            TripMemberAdmissionFenceState.Prepared);
    }

    private static bool CanContinue(TripAdmissionWriteOutcome outcome)
    {
        return outcome is TripAdmissionWriteOutcome.Success or TripAdmissionWriteOutcome.AlreadyCompleted;
    }

    private static ApplicationResult<TripInvitationDecisionResult> NotFoundDecision()
    {
        return ApplicationResult<TripInvitationDecisionResult>.Failure(
            TripInvitationApplicationErrors.NotFound());
    }

    private static ApplicationResult<TripInvitationDecisionResult> AdmissionUnavailable()
    {
        return ApplicationResult<TripInvitationDecisionResult>.Failure(
            TripInvitationApplicationErrors.AdmissionUnavailable());
    }
}
