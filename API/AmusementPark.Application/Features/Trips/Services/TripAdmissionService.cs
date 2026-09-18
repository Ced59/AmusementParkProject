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

    public TripAdmissionService(
        ITripAdmissionRepository repository,
        ITripPlanRepository tripPlanRepository,
        ITripInvitationSecurity security,
        IUserRepository users,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.tripPlanRepository = tripPlanRepository ?? throw new ArgumentNullException(nameof(tripPlanRepository));
        this.security = security ?? throw new ArgumentNullException(nameof(security));
        this.users = users ?? throw new ArgumentNullException(nameof(users));
        this.timeProvider = timeProvider ?? TimeProvider.System;
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
                if (establishedTrip?.ResolveRole(normalizedUserId) is not null)
                {
                    await this.repository.MarkInvitationAdmissionCompletedAsync(
                        invitation.Id,
                        resumedFence,
                        cancellationToken);
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
        return outcome is TripAdmissionWriteOutcome.Success or TripAdmissionWriteOutcome.AlreadyCompleted
            ? ApplicationResult<TripInvitationDecisionResult>.Success(
                new TripInvitationDecisionResult(
                    invitation.TripPlanId.Value,
                    outcome == TripAdmissionWriteOutcome.AlreadyCompleted))
            : ApplicationResult<TripInvitationDecisionResult>.Failure(
                TripInvitationApplicationErrors.ChangedConcurrently());
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
            if (establishedTrip?.ResolveRole(invitation.AcceptingUserId) is not null)
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
        return ApplicationResult<TripInvitationDecisionResult>.Success(
            new TripInvitationDecisionResult(invitation.TripPlanId.Value, wasReplayed));
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
