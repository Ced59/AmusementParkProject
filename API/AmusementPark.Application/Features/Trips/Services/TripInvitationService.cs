using System.Net.Mail;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripInvitationService
{
    public const int MaximumIdempotencyKeyLength = 200;
    private const int MaximumTokenAttempts = 3;
    private const string NeutralInviterDisplayName = "Amusement Parks";

    private readonly ITripPlanRepository tripPlanRepository;
    private readonly ITripInvitationRepository invitationRepository;
    private readonly ITripInvitationSecurity security;
    private readonly TripChildMutationExecutor mutationExecutor;
    private readonly IUserRepository userRepository;
    private readonly TimeProvider timeProvider;

    public TripInvitationService(
        ITripPlanRepository tripPlanRepository,
        ITripInvitationRepository invitationRepository,
        ITripInvitationSecurity security,
        TripChildMutationExecutor mutationExecutor,
        IUserRepository userRepository,
        TimeProvider? timeProvider = null)
    {
        this.tripPlanRepository = tripPlanRepository
            ?? throw new ArgumentNullException(nameof(tripPlanRepository));
        this.invitationRepository = invitationRepository
            ?? throw new ArgumentNullException(nameof(invitationRepository));
        this.security = security ?? throw new ArgumentNullException(nameof(security));
        this.mutationExecutor = mutationExecutor ?? throw new ArgumentNullException(nameof(mutationExecutor));
        this.userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<TripInvitationCreationResult>> CreateAsync(
        string userId,
        string tripPlanId,
        string clientOperationId,
        TripInvitationCreateInput input,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeIdentity(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedTripId)
            || input is null)
        {
            return InvalidCreation(TripInvitationErrorCodes.InvalidState, "A valid trip invitation request is required.");
        }

        string normalizedOperationId = clientOperationId?.Trim() ?? string.Empty;
        string? normalizedTargetEmail = NormalizeOptionalEmail(input.TargetEmail);
        if (normalizedOperationId.Length is 0 or > MaximumIdempotencyKeyLength
            || input.ExpectedPlanVersion < 1
            || !Enum.IsDefined(input.ProposedRole)
            || input.LifetimeHours < TripInvitation.MinimumLifetime.TotalHours
            || input.LifetimeHours > TripInvitation.MaximumLifetime.TotalHours
            || (input.TargetEmail is not null && normalizedTargetEmail is null))
        {
            return InvalidCreation(TripInvitationErrorCodes.InvalidState, "The invitation role, lifetime or recipient is invalid.");
        }

        TripPlan? trip = await this.tripPlanRepository.GetOwnedAsync(
            normalizedUserId,
            parsedTripId,
            cancellationToken);
        if (trip is null)
        {
            return ApplicationResult<TripInvitationCreationResult>.Failure(TripPlanApplicationErrors.NotFound());
        }

        TripMember inviter = ResolveOwner(trip);
        string operationKeyHash = this.security.HashOperationKey(normalizedUserId, normalizedOperationId);
        TripInvitationCreationRecord? existing = await this.invitationRepository.ResolveCreationAsync(
            trip.Id,
            inviter.Id,
            operationKeyHash,
            cancellationToken);
        string requestHash = this.security.HashCreationPayload(
            input.ProposedRole,
            input.LifetimeHours,
            normalizedTargetEmail,
            existing?.RequestHash);
        ApplicationResult<TripInvitationCreationResult>? replay = this.TryMapReplay(
            existing,
            normalizedUserId,
            requestHash);
        if (replay is not null)
        {
            return replay;
        }

        bool resumesPreparedCreation = existing?.Invitation.Status == TripInvitationStatus.Prepared;
        if (!resumesPreparedCreation && trip.Version != input.ExpectedPlanVersion)
        {
            return ApplicationResult<TripInvitationCreationResult>.Failure(
                TripPlanApplicationErrors.ChangedConcurrently(trip.Version));
        }

        if (!trip.CanAcceptMembers)
        {
            return ApplicationResult<TripInvitationCreationResult>.Failure(
                TripInvitationApplicationErrors.AdmissionsClosed());
        }

        User? inviterUser = await this.userRepository.GetByIdAsync(normalizedUserId, cancellationToken);
        string inviterDisplayName = inviterUser?.ResolvePublicDisplayName() ?? NeutralInviterDisplayName;
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        TripInvitationPeriodPreview periodPreview = BuildPeriodPreview(trip.DateProposal);
        TripInvitationMemberCountBand memberCountBand = TripInvitation.ResolveMemberCountBand(
            trip.Members.Count(static member => member.State == TripMembershipState.Active));
        TripInvitationEmailFingerprint? emailFingerprint = normalizedTargetEmail is null
            ? null
            : this.security.FingerprintEmail(normalizedTargetEmail);

        return await this.mutationExecutor.ExecuteOwnedAsync(
            trip,
            operationKeyHash,
            async lease => await this.CreateUnderLeaseAsync(
                trip,
                inviter,
                inviterDisplayName,
                input,
                emailFingerprint,
                periodPreview,
                memberCountBand,
                nowUtc,
                operationKeyHash,
                requestHash,
                normalizedUserId,
                lease,
                cancellationToken),
            cancellationToken);
    }

    public async Task<ApplicationResult<TripInvitationListResult>> ListAsync(
        string userId,
        string tripPlanId,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeIdentity(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedTripId))
        {
            return ApplicationResult<TripInvitationListResult>.Failure(
                TripPlanApplicationErrors.NotFound());
        }

        TripPlan? trip = await this.tripPlanRepository.GetOwnedAsync(
            normalizedUserId,
            parsedTripId,
            cancellationToken);
        if (trip is null)
        {
            return ApplicationResult<TripInvitationListResult>.Failure(
                TripPlanApplicationErrors.NotFound());
        }

        IReadOnlyCollection<TripInvitation> invitations = await this.invitationRepository.ListActiveAsync(
            trip.Id,
            cancellationToken);
        User? inviterUser = await this.userRepository.GetByIdAsync(normalizedUserId, cancellationToken);
        string inviterDisplayName = inviterUser?.ResolvePublicDisplayName() ?? NeutralInviterDisplayName;
        return ApplicationResult<TripInvitationListResult>.Success(new TripInvitationListResult(
            inviterDisplayName,
            invitations.Select(ToSummary).ToArray()));
    }

    public async Task<ApplicationResult<TripInvitationPreviewResult>> PreviewAsync(
        string token,
        CancellationToken cancellationToken)
    {
        if (!this.security.TryHashPublicToken(token, out string tokenHash))
        {
            return ApplicationResult<TripInvitationPreviewResult>.Failure(
                TripInvitationApplicationErrors.NotFound());
        }

        TripInvitation? invitation = await this.invitationRepository.GetPublicByTokenHashAsync(
            tokenHash,
            cancellationToken);
        if (invitation is null
            || !invitation.IsPubliclyResolvable(this.timeProvider.GetUtcNow().UtcDateTime))
        {
            return ApplicationResult<TripInvitationPreviewResult>.Failure(
                TripInvitationApplicationErrors.NotFound());
        }

        return ApplicationResult<TripInvitationPreviewResult>.Success(new TripInvitationPreviewResult(
            invitation.TripTitle,
            invitation.InviterDisplayName,
            invitation.ProposedRole,
            invitation.PeriodPreview.Kind,
            invitation.PeriodPreview.StartMonth,
            invitation.PeriodPreview.EndMonth,
            invitation.MemberCountBand,
            invitation.ExpiresAtUtc,
            invitation.IsTargeted));
    }

    public async Task<ApplicationResult> RevokeAsync(
        string userId,
        string tripPlanId,
        string invitationId,
        long expectedInvitationVersion,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeIdentity(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedTripId)
            || !TripInvitationId.TryParse(invitationId, out TripInvitationId parsedInvitationId)
            || expectedInvitationVersion < 1)
        {
            return ApplicationResult.Failure(TripInvitationApplicationErrors.NotFound());
        }

        string normalizedOperationId = clientOperationId?.Trim() ?? string.Empty;
        if (normalizedOperationId.Length is 0 or > MaximumIdempotencyKeyLength)
        {
            return ApplicationResult.Failure(TripInvitationApplicationErrors.Invalid(
                TripInvitationErrorCodes.InvalidState,
                "A bounded idempotency key is required."));
        }

        TripPlan? trip = await this.tripPlanRepository.GetOwnedAsync(
            normalizedUserId,
            parsedTripId,
            cancellationToken);
        if (trip is null)
        {
            return ApplicationResult.Failure(TripPlanApplicationErrors.NotFound());
        }

        TripInvitation? invitation = await this.invitationRepository.GetOwnedAsync(
            trip.Id,
            parsedInvitationId,
            cancellationToken);
        if (invitation is null)
        {
            return ApplicationResult.Failure(TripInvitationApplicationErrors.NotFound());
        }

        if (invitation.Status == TripInvitationStatus.Revoked)
        {
            return ApplicationResult.Success();
        }

        if (invitation.Version != expectedInvitationVersion)
        {
            return ApplicationResult.Failure(TripInvitationApplicationErrors.ChangedConcurrently());
        }

        try
        {
            invitation.Revoke(this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (TripInvitationValidationException exception)
        {
            return ApplicationResult.Failure(TripInvitationApplicationErrors.Invalid(
                exception.Code,
                exception.Message));
        }

        string leaseOperationId = this.security.HashOperationKey(normalizedUserId, normalizedOperationId);
        return await this.mutationExecutor.ExecuteOwnedAsync(
            trip,
            leaseOperationId,
            async lease => MapWriteOutcome(await this.invitationRepository.RevokeAsync(
                invitation,
                expectedInvitationVersion,
                lease,
                cancellationToken)),
            cancellationToken);
    }

    private async Task<ApplicationResult<TripInvitationCreationResult>> CreateUnderLeaseAsync(
        TripPlan trip,
        TripMember inviter,
        string inviterDisplayName,
        TripInvitationCreateInput input,
        TripInvitationEmailFingerprint? emailFingerprint,
        TripInvitationPeriodPreview periodPreview,
        TripInvitationMemberCountBand memberCountBand,
        DateTime nowUtc,
        string operationKeyHash,
        string requestHash,
        string actorUserId,
        TripChildMutationLease lease,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < MaximumTokenAttempts; attempt++)
        {
            TripInvitationId invitationId = TripInvitationId.New();
            TripInvitationTokenMaterial token = this.security.CreateToken(
                invitationId,
                actorUserId,
                operationKeyHash,
                requestHash);
            TripInvitation invitation = TripInvitation.Create(
                invitationId,
                trip.Id,
                trip.Title,
                token.TokenHash,
                token.TokenHint,
                input.ProposedRole,
                inviter.Id,
                inviterDisplayName,
                emailFingerprint?.Hmac,
                emailFingerprint?.KeyVersion,
                periodPreview,
                memberCountBand,
                nowUtc,
                nowUtc.AddHours(input.LifetimeHours));
            TripInvitationCreationWriteResult write = await this.invitationRepository.CreateAsync(
                invitation,
                lease,
                operationKeyHash,
                requestHash,
                token.SealedToken,
                token.KeyVersion,
                cancellationToken);
            if (write.Outcome == TripInvitationCreationWriteOutcome.TokenCollision)
            {
                continue;
            }

            return this.MapCreationWrite(write, actorUserId);
        }

        return ApplicationResult<TripInvitationCreationResult>.Failure(
            TripInvitationApplicationErrors.CreationUnavailable());
    }

    private ApplicationResult<TripInvitationCreationResult> MapCreationWrite(
        TripInvitationCreationWriteResult write,
        string actorUserId)
    {
        if (write.Outcome == TripInvitationCreationWriteOutcome.IdempotencyConflict)
        {
            return ApplicationResult<TripInvitationCreationResult>.Failure(
                TripInvitationApplicationErrors.IdempotencyConflict());
        }

        if (write.Outcome == TripInvitationCreationWriteOutcome.LimitReached)
        {
            return ApplicationResult<TripInvitationCreationResult>.Failure(
                TripInvitationApplicationErrors.LimitReached());
        }

        if (write.Outcome != TripInvitationCreationWriteOutcome.Success || write.Record is null)
        {
            return ApplicationResult<TripInvitationCreationResult>.Failure(
                TripInvitationApplicationErrors.CreationUnavailable());
        }

        if (!write.Record.Invitation.IsPubliclyResolvable(this.timeProvider.GetUtcNow().UtcDateTime)
            || !this.security.TryRevealToken(write.Record, actorUserId, out string token))
        {
            return ApplicationResult<TripInvitationCreationResult>.Failure(
                TripInvitationApplicationErrors.ReplayUnavailable());
        }

        return Success(write.Record.Invitation, token, write.Record.WasReplayed);
    }

    private ApplicationResult<TripInvitationCreationResult>? TryMapReplay(
        TripInvitationCreationRecord? existing,
        string actorUserId,
        string requestHash)
    {
        if (existing is null)
        {
            return null;
        }

        if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return ApplicationResult<TripInvitationCreationResult>.Failure(
                TripInvitationApplicationErrors.IdempotencyConflict());
        }

        if (existing.Invitation.Status == TripInvitationStatus.Prepared)
        {
            return null;
        }

        if (!existing.Invitation.IsPubliclyResolvable(this.timeProvider.GetUtcNow().UtcDateTime)
            || !this.security.TryRevealToken(existing, actorUserId, out string token))
        {
            return ApplicationResult<TripInvitationCreationResult>.Failure(
                TripInvitationApplicationErrors.ReplayUnavailable());
        }

        return Success(existing.Invitation, token, true);
    }

    private static ApplicationResult<TripInvitationCreationResult> Success(
        TripInvitation invitation,
        string token,
        bool wasReplayed)
    {
        return ApplicationResult<TripInvitationCreationResult>.Success(new TripInvitationCreationResult(
            invitation.Id.Value,
            token,
            invitation.InviterDisplayName,
            invitation.ProposedRole,
            invitation.ExpiresAtUtc,
            invitation.IsTargeted,
            wasReplayed));
    }

    private static TripInvitationSummaryResult ToSummary(TripInvitation invitation)
    {
        return new TripInvitationSummaryResult(
            invitation.Id.Value,
            invitation.TokenHint,
            invitation.ProposedRole,
            invitation.ExpiresAtUtc,
            invitation.IsTargeted,
            invitation.Version,
            invitation.CreatedAtUtc);
    }

    private static ApplicationResult MapWriteOutcome(TripInvitationWriteOutcome outcome)
    {
        return outcome switch
        {
            TripInvitationWriteOutcome.Success => ApplicationResult.Success(),
            TripInvitationWriteOutcome.NotFound => ApplicationResult.Failure(
                TripInvitationApplicationErrors.NotFound()),
            _ => ApplicationResult.Failure(TripInvitationApplicationErrors.ChangedConcurrently()),
        };
    }

    private static TripInvitationPeriodPreview BuildPeriodPreview(TripDateProposal proposal)
    {
        return proposal.Kind switch
        {
            TripDateProposalKind.None => TripInvitationPeriodPreview.Unspecified(),
            TripDateProposalKind.Candidates => TripInvitationPeriodPreview.FromDates(proposal.CandidateDates),
            _ => TripInvitationPeriodPreview.FromDates(new[]
            {
                proposal.StartDate!.Value,
                proposal.EndDate!.Value,
            }),
        };
    }

    private static TripMember ResolveOwner(TripPlan trip)
    {
        return trip.Members.Single(member => member.State == TripMembershipState.Active
            && string.Equals(member.UserId, trip.OwnerUserId, StringComparison.Ordinal));
    }

    private static string? NormalizeOptionalEmail(string? value)
    {
        if (value is null)
        {
            return null;
        }

        string normalized = value.Trim().ToLowerInvariant();
        return normalized.Length is > 0 and <= 254
            && MailAddress.TryCreate(normalized, out MailAddress? parsed)
            && string.Equals(parsed.Address, normalized, StringComparison.OrdinalIgnoreCase)
                ? normalized
                : null;
    }

    private static bool TryNormalizeIdentity(
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
        }
        catch (ArgumentException)
        {
            return false;
        }

        return TripPlanId.TryParse(tripPlanId, out parsedTripId);
    }

    private static ApplicationResult<TripInvitationCreationResult> InvalidCreation(
        string code,
        string message)
    {
        return ApplicationResult<TripInvitationCreationResult>.Failure(
            TripInvitationApplicationErrors.Invalid(code, message));
    }
}
