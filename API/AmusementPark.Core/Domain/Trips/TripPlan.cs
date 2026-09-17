using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Trips;

public sealed class TripPlan
{
    public const int MaximumPlansPerOwner = 50;
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
        long version)
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
        this.Members = normalizedMembers;
        this.AdmissionClosureState = admissionClosureState;
        this.DeletionState = deletionState;
        this.ChildMutationEpoch = childMutationEpoch;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
        this.Version = version;
    }

    public TripPlanId Id { get; }
    public string OwnerUserId { get; }
    public string Title { get; private set; }
    public TripDateProposal DateProposal { get; private set; }
    public string? DestinationTimeZoneId { get; private set; }
    public TripPlanStatus Status { get; }
    public TripPlanAccessScope AccessScope { get; }
    public IReadOnlyCollection<TripMember> Members { get; }
    public TripAdmissionClosureState AdmissionClosureState { get; private set; }
    public TripDeletionState DeletionState { get; private set; }
    public long ChildMutationEpoch { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

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
            1);
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
        long version)
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
            version);
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
