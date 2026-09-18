using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Trips;

public sealed class TripPlan
{
    public const int MaximumPlansPerOwner = 50;
    public const int MaximumMembers = 50;
    public const int MaximumTitleLength = 120;
    public const int MaximumTimeZoneIdLength = 100;
    public static readonly TimeSpan CreationReplayRetention = TimeSpan.FromHours(24);

    private TripPlan(
        TripPlanId id,
        string ownerUserId,
        string title,
        TripDateProposal dateProposal,
        string? destinationTimeZoneId,
        TripPlanStatus status,
        TripPlanAccessScope accessScope,
        IReadOnlyCollection<TripMember> members,
        TripAdmissionClosureState admissionClosureState,
        TripDeletionState deletionState,
        long childMutationEpoch,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version,
        TripMemberAdmissionFence? memberAdmissionFence)
    {
        _ = id.Value;
        string normalizedOwnerUserId = IdentifierRules.NormalizeRequired(ownerUserId, nameof(ownerUserId));
        string normalizedTitle = NormalizeTitle(title);
        ArgumentNullException.ThrowIfNull(dateProposal);
        string? normalizedTimeZoneId = NormalizeTimeZoneId(destinationTimeZoneId, dateProposal.Kind);
        ValidateEnums(status, accessScope, admissionClosureState, deletionState);
        ValidateTimestamps(createdAtUtc, updatedAtUtc);
        if (childMutationEpoch < 1)
        {
            throw Invalid(TripPlanErrorCodes.InvalidState, "The child mutation epoch must be positive.");
        }
        if (version < 1)
        {
            throw Invalid(TripPlanErrorCodes.InvalidVersion, "The trip plan version must be positive.");
        }

        TripMember[] normalizedMembers = members.ToArray();
        if (normalizedMembers.Length is < 1 or > MaximumMembers
            || normalizedMembers.Select(static member => member.UserId).Distinct(StringComparer.Ordinal).Count()
                != normalizedMembers.Length)
        {
            throw Invalid(TripPlanErrorCodes.InvalidState, "The trip member collection is invalid.");
        }
        TripMember[] activeOwners = normalizedMembers
            .Where(member => member.State == TripMembershipState.Active
                && string.Equals(member.UserId, normalizedOwnerUserId, StringComparison.Ordinal))
            .ToArray();
        if (activeOwners.Length != 1 || activeOwners[0].DelegatedRole.HasValue)
        {
            throw Invalid(
                TripPlanErrorCodes.InvalidOwner,
                "The authoritative trip owner must have exactly one active owner membership.");
        }

        this.Id = id;
        this.OwnerUserId = normalizedOwnerUserId;
        this.Title = normalizedTitle;
        this.DateProposal = dateProposal;
        this.DestinationTimeZoneId = normalizedTimeZoneId;
        this.Status = status;
        this.AccessScope = accessScope;
        this.members = normalizedMembers.ToList();
        this.MemberAdmissionFence = memberAdmissionFence;
        this.AdmissionClosureState = admissionClosureState;
        this.DeletionState = deletionState;
        this.ChildMutationEpoch = childMutationEpoch;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
        this.Version = version;
    }

    public TripPlanId Id { get; }
    private readonly List<TripMember> members;

    public string OwnerUserId { get; private set; }
    public string Title { get; private set; }
    public TripDateProposal DateProposal { get; private set; }
    public string? DestinationTimeZoneId { get; private set; }
    public TripPlanStatus Status { get; }
    public TripPlanAccessScope AccessScope { get; }
    public IReadOnlyCollection<TripMember> Members => this.members;
    public TripMemberAdmissionFence? MemberAdmissionFence { get; private set; }
    public TripAdmissionClosureState AdmissionClosureState { get; private set; }
    public TripDeletionState DeletionState { get; private set; }
    public long ChildMutationEpoch { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public bool CanAcceptMembers => this.DeletionState == TripDeletionState.None
        && this.AdmissionClosureState == TripAdmissionClosureState.Open
        && this.Status is TripPlanStatus.Draft or TripPlanStatus.OpenForVotes or TripPlanStatus.Decided;

    public static TripPlan Create(
        TripPlanId id,
        string ownerUserId,
        string title,
        TripDateProposal dateProposal,
        string? destinationTimeZoneId,
        DateTime nowUtc)
    {
        TripMember owner = TripMember.CreateOwner(ownerUserId, nowUtc);
        return new TripPlan(
            id,
            ownerUserId,
            title,
            dateProposal,
            destinationTimeZoneId,
            TripPlanStatus.Draft,
            TripPlanAccessScope.MembersOnly,
            new[] { owner },
            TripAdmissionClosureState.Open,
            TripDeletionState.None,
            1,
            nowUtc,
            nowUtc,
            1,
            null);
    }

    public static TripPlan Restore(
        TripPlanId id,
        string ownerUserId,
        string title,
        TripDateProposal dateProposal,
        string? destinationTimeZoneId,
        TripPlanStatus status,
        TripPlanAccessScope accessScope,
        IReadOnlyCollection<TripMember> members,
        TripAdmissionClosureState admissionClosureState,
        TripDeletionState deletionState,
        long childMutationEpoch,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version,
        TripMemberAdmissionFence? memberAdmissionFence = null)
    {
        ArgumentNullException.ThrowIfNull(members);
        return new TripPlan(
            id,
            ownerUserId,
            title,
            dateProposal,
            destinationTimeZoneId,
            status,
            accessScope,
            members,
            admissionClosureState,
            deletionState,
            childMutationEpoch,
            createdAtUtc,
            updatedAtUtc,
            version,
            memberAdmissionFence);
    }

    public TripEffectiveRole? ResolveRole(string userId)
    {
        TripMember? member = this.members.SingleOrDefault(candidate =>
            candidate.State == TripMembershipState.Active
            && string.Equals(candidate.UserId, userId, StringComparison.Ordinal));
        if (member is null)
        {
            return null;
        }

        if (string.Equals(this.OwnerUserId, member.UserId, StringComparison.Ordinal))
        {
            return TripEffectiveRole.Owner;
        }

        return member.DelegatedRole switch
        {
            TripDelegatedRole.Editor => TripEffectiveRole.Editor,
            TripDelegatedRole.Participant => TripEffectiveRole.Participant,
            TripDelegatedRole.Viewer => TripEffectiveRole.Viewer,
            _ => null,
        };
    }

    public void PrepareAdmission(
        TripInvitationId invitationId,
        string operationId,
        string candidateUserId,
        long generation,
        DateTime leaseExpiresAtUtc,
        DateTime nowUtc)
    {
        this.ValidateMutation(nowUtc);
        if (!this.CanAcceptMembers
            || this.members.Count >= MaximumMembers
            || this.members.Any(member => string.Equals(member.UserId, candidateUserId, StringComparison.Ordinal))
            || (this.MemberAdmissionFence is not null
                && this.MemberAdmissionFence.State != TripMemberAdmissionFenceState.Cancelled))
        {
            throw Invalid(TripPlanErrorCodes.InvalidState, "The trip cannot prepare this member admission.");
        }

        this.MemberAdmissionFence = TripMemberAdmissionFence.Prepare(
            invitationId,
            operationId,
            candidateUserId,
            generation,
            leaseExpiresAtUtc);
    }

    public void ArmAdmission(string operationId, long generation)
    {
        TripMemberAdmissionFence fence = this.RequireAdmissionFence(operationId, generation);
        fence.Arm();
    }

    public void ApplyAdmission(
        string operationId,
        long generation,
        TripDelegatedRole role,
        DateTime nowUtc)
    {
        TripMemberAdmissionFence fence = this.RequireAdmissionFence(operationId, generation);
        if (!this.CanAcceptMembers || fence.State != TripMemberAdmissionFenceState.Active)
        {
            throw Invalid(TripPlanErrorCodes.InvalidState, "The trip admission cannot be applied.");
        }

        this.members.Add(TripMember.CreateProvisional(fence.CandidateUserId, role, operationId, nowUtc));
        fence.MarkApplied();
    }

    public void EstablishAdmission(string operationId, long generation, DateTime nowUtc)
    {
        this.ValidateMutation(nowUtc);
        TripMemberAdmissionFence fence = this.RequireAdmissionFence(operationId, generation);
        TripMember member = this.members.Single(candidate =>
            string.Equals(candidate.AdmissionOperationId, operationId, StringComparison.Ordinal));
        if (!this.CanAcceptMembers || fence.State != TripMemberAdmissionFenceState.Applied)
        {
            throw Invalid(TripPlanErrorCodes.InvalidState, "The trip admission cannot be established.");
        }

        this.PrepareMutation();
        member.Activate(operationId);
        this.MemberAdmissionFence = null;
        this.CommitMutation(nowUtc);
    }

    public void CancelAdmission(string operationId, long generation, DateTime nowUtc)
    {
        this.ValidateMutation(nowUtc);
        TripMemberAdmissionFence fence = this.RequireAdmissionFence(operationId, generation);
        this.members.RemoveAll(member =>
            string.Equals(member.AdmissionOperationId, operationId, StringComparison.Ordinal));
        fence.Cancel();
        this.MemberAdmissionFence = null;
        this.UpdatedAtUtc = nowUtc;
    }

    public void ChangeMemberRole(
        string ownerUserId,
        TripMemberId memberId,
        TripDelegatedRole role,
        DateTime nowUtc)
    {
        this.ValidateOwner(ownerUserId);
        this.ValidateMutation(nowUtc);
        TripMember member = this.members.SingleOrDefault(candidate => candidate.Id == memberId)
            ?? throw Invalid(TripPlanErrorCodes.InvalidState, "The trip member was not found.");
        if (string.Equals(member.UserId, this.OwnerUserId, StringComparison.Ordinal))
        {
            throw Invalid(TripPlanErrorCodes.InvalidOwner, "The owner role cannot be changed directly.");
        }

        this.PrepareMutation();
        member.ChangeDelegatedRole(role);
        this.IncrementChildMutationEpoch();
        this.CommitMutation(nowUtc);
    }

    public void TransferOwnership(
        string ownerUserId,
        TripMemberId newOwnerMemberId,
        TripDelegatedRole previousOwnerRole,
        DateTime nowUtc)
    {
        this.ValidateOwner(ownerUserId);
        this.ValidateMutation(nowUtc);
        TripMember previousOwner = this.members.Single(member =>
            string.Equals(member.UserId, this.OwnerUserId, StringComparison.Ordinal));
        TripMember newOwner = this.members.SingleOrDefault(member => member.Id == newOwnerMemberId)
            ?? throw Invalid(TripPlanErrorCodes.InvalidState, "The new trip owner was not found.");
        if (newOwner.State != TripMembershipState.Active
            || string.Equals(newOwner.UserId, previousOwner.UserId, StringComparison.Ordinal))
        {
            throw Invalid(TripPlanErrorCodes.InvalidOwner, "Ownership requires another active trip member.");
        }

        this.PrepareMutation();
        previousOwner.ChangeDelegatedRole(previousOwnerRole);
        newOwner.BecomeOwner();
        this.OwnerUserId = newOwner.UserId;
        this.IncrementChildMutationEpoch();
        this.CommitMutation(nowUtc);
    }

    public TripMember BeginMemberDeparture(string userId, DateTime nowUtc)
    {
        this.ValidateMutation(nowUtc);
        if (string.Equals(this.OwnerUserId, userId, StringComparison.Ordinal))
        {
            throw Invalid(TripPlanErrorCodes.InvalidOwner, "The trip owner must transfer ownership before leaving.");
        }

        TripMember member = this.members.SingleOrDefault(candidate =>
            candidate.State == TripMembershipState.Active
            && string.Equals(candidate.UserId, userId, StringComparison.Ordinal))
            ?? throw Invalid(TripPlanErrorCodes.InvalidState, "The active trip member was not found.");
        this.PrepareMutation();
        member.BeginLeaving();
        this.IncrementChildMutationEpoch();
        this.CommitMutation(nowUtc);
        return member;
    }

    public void RemoveLeavingMember(TripMemberId memberId, DateTime nowUtc)
    {
        this.ValidateMutation(nowUtc);
        TripMember member = this.members.SingleOrDefault(candidate => candidate.Id == memberId)
            ?? throw Invalid(TripPlanErrorCodes.InvalidState, "The departing trip member was not found.");
        if (member.State != TripMembershipState.Leaving)
        {
            throw Invalid(TripPlanErrorCodes.InvalidState, "Only a leaving trip member can be removed.");
        }

        this.members.Remove(member);
        this.UpdatedAtUtc = nowUtc;
    }

    public void Rename(string title, DateTime nowUtc)
    {
        string normalizedTitle = NormalizeTitle(title);
        this.ValidateMutation(nowUtc);
        if (string.Equals(this.Title, normalizedTitle, StringComparison.Ordinal))
        {
            return;
        }

        this.PrepareMutation();
        this.Title = normalizedTitle;
        this.CommitMutation(nowUtc);
    }

    public void SetDates(
        TripDateProposal dateProposal,
        string? destinationTimeZoneId,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(dateProposal);
        string? normalizedTimeZoneId = NormalizeTimeZoneId(destinationTimeZoneId, dateProposal.Kind);
        this.ValidateMutation(nowUtc);
        if (this.DateProposal.HasSameValueAs(dateProposal)
            && string.Equals(this.DestinationTimeZoneId, normalizedTimeZoneId, StringComparison.Ordinal))
        {
            return;
        }

        this.PrepareMutation();
        this.DateProposal = dateProposal;
        this.DestinationTimeZoneId = normalizedTimeZoneId;
        this.IncrementChildMutationEpoch();
        this.CommitMutation(nowUtc);
    }

    public void BeginDeletion(DateTime nowUtc)
    {
        this.ValidateMutation(nowUtc);
        if (this.DeletionState != TripDeletionState.None)
        {
            return;
        }

        this.PrepareMutation();
        this.AdmissionClosureState = TripAdmissionClosureState.Closing;
        this.DeletionState = TripDeletionState.Pending;
        this.IncrementChildMutationEpoch();
        this.CommitMutation(nowUtc);
    }

    private void ValidateMutation(DateTime nowUtc)
    {
        EnsureUtc(nowUtc);
        if (nowUtc < this.UpdatedAtUtc)
        {
            throw Invalid(TripPlanErrorCodes.InvalidTimestamp, "A trip mutation cannot predate its current state.");
        }
    }

    private TripMemberAdmissionFence RequireAdmissionFence(string operationId, long generation)
    {
        TripMemberAdmissionFence? fence = this.MemberAdmissionFence;
        if (fence is null
            || fence.Generation != generation
            || !string.Equals(fence.OperationId, operationId, StringComparison.Ordinal))
        {
            throw Invalid(TripPlanErrorCodes.InvalidState, "The trip admission fence does not match the operation.");
        }

        return fence;
    }

    private void ValidateOwner(string ownerUserId)
    {
        if (!string.Equals(this.OwnerUserId, ownerUserId?.Trim(), StringComparison.Ordinal))
        {
            throw Invalid(TripPlanErrorCodes.InvalidOwner, "Only the trip owner can perform this action.");
        }
    }

    private void PrepareMutation()
    {
        if (this.Version == long.MaxValue)
        {
            throw Invalid(TripPlanErrorCodes.InvalidVersion, "The trip plan version cannot be incremented further.");
        }
    }

    private void CommitMutation(DateTime nowUtc)
    {
        this.Version++;
        this.UpdatedAtUtc = nowUtc;
    }

    private void IncrementChildMutationEpoch()
    {
        if (this.ChildMutationEpoch == long.MaxValue)
        {
            throw Invalid(
                TripPlanErrorCodes.InvalidVersion,
                "The child mutation epoch cannot be incremented further.");
        }

        this.ChildMutationEpoch++;
    }

    private static string NormalizeTitle(string? title)
    {
        string normalized = title?.Trim() ?? string.Empty;
        if (normalized.Length is 0 or > MaximumTitleLength)
        {
            throw Invalid(
                TripPlanErrorCodes.InvalidTitle,
                $"A trip title is required and cannot exceed {MaximumTitleLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeTimeZoneId(string? timeZoneId, TripDateProposalKind kind)
    {
        string? normalized = string.IsNullOrWhiteSpace(timeZoneId) ? null : timeZoneId.Trim();
        if (kind != TripDateProposalKind.None && normalized is null)
        {
            throw Invalid(TripPlanErrorCodes.InvalidTimeZone, "A destination time zone is required when dates are set.");
        }

        if (normalized?.Length > MaximumTimeZoneIdLength)
        {
            throw Invalid(TripPlanErrorCodes.InvalidTimeZone, "The destination time zone identifier is too long.");
        }

        return normalized;
    }

    private static void ValidateEnums(
        TripPlanStatus status,
        TripPlanAccessScope accessScope,
        TripAdmissionClosureState closureState,
        TripDeletionState deletionState)
    {
        if (!Enum.IsDefined(status)
            || accessScope != TripPlanAccessScope.MembersOnly
            || !Enum.IsDefined(closureState)
            || !Enum.IsDefined(deletionState))
        {
            throw Invalid(TripPlanErrorCodes.InvalidState, "The trip plan state is invalid.");
        }
    }

    private static void ValidateTimestamps(DateTime createdAtUtc, DateTime updatedAtUtc)
    {
        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);
        if (updatedAtUtc < createdAtUtc)
        {
            throw Invalid(TripPlanErrorCodes.InvalidTimestamp, "The trip timestamps are not chronological.");
        }
    }

    private static void EnsureUtc(DateTime timestamp)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw Invalid(TripPlanErrorCodes.InvalidTimestamp, "Trip timestamps must be expressed in UTC.");
        }
    }

    private static TripPlanValidationException Invalid(string code, string message)
    {
        return new TripPlanValidationException(code, message);
    }
}
