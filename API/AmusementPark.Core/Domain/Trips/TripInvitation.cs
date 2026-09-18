namespace AmusementPark.Core.Domain.Trips;

public sealed class TripInvitation
{
    public const int MaximumActiveInvitationsPerTrip = 20;
    public const int MaximumDisplayNameLength = 80;
    public static readonly TimeSpan MinimumLifetime = TimeSpan.FromHours(1);
    public static readonly TimeSpan MaximumLifetime = TimeSpan.FromDays(30);
    public static readonly TimeSpan IdempotencyReplayRetention = TimeSpan.FromHours(24);

    private TripInvitation(
        TripInvitationId id,
        TripPlanId tripPlanId,
        string tripTitle,
        string tokenHash,
        string tokenHint,
        TripDelegatedRole proposedRole,
        TripMemberId inviterMemberId,
        string inviterDisplayName,
        string? targetEmailHmac,
        string? targetEmailHmacKeyVersion,
        TripInvitationStatus status,
        TripInvitationPreviewPolicy previewPolicy,
        TripInvitationPeriodPreview periodPreview,
        TripInvitationMemberCountBand memberCountBand,
        DateTime expiresAtUtc,
        DateTime? revokedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version)
    {
        _ = id.Value;
        _ = tripPlanId.Value;
        _ = inviterMemberId.Value;
        string normalizedTripTitle = tripTitle?.Trim() ?? string.Empty;
        if (normalizedTripTitle.Length is 0 or > TripPlan.MaximumTitleLength)
        {
            throw Invalid(TripInvitationErrorCodes.InvalidPreview, "The trip title preview is invalid.");
        }

        ValidateToken(tokenHash, tokenHint);
        if (!Enum.IsDefined(proposedRole)
            || !Enum.IsDefined(status)
            || previewPolicy != TripInvitationPreviewPolicy.ApproximatePeriod
            || !Enum.IsDefined(memberCountBand))
        {
            throw Invalid(TripInvitationErrorCodes.InvalidState, "The trip invitation state is invalid.");
        }

        string normalizedDisplayName = inviterDisplayName?.Trim() ?? string.Empty;
        if (normalizedDisplayName.Length is 0 or > MaximumDisplayNameLength)
        {
            throw Invalid(TripInvitationErrorCodes.InvalidPreview, "The inviter display name is invalid.");
        }

        bool hasTargetEmail = !string.IsNullOrWhiteSpace(targetEmailHmac);
        if (hasTargetEmail != !string.IsNullOrWhiteSpace(targetEmailHmacKeyVersion))
        {
            throw Invalid(TripInvitationErrorCodes.InvalidState, "The targeted invitation fingerprint is incomplete.");
        }

        ValidateUtc(createdAtUtc, nameof(createdAtUtc));
        ValidateUtc(updatedAtUtc, nameof(updatedAtUtc));
        ValidateUtc(expiresAtUtc, nameof(expiresAtUtc));
        if (revokedAtUtc.HasValue)
        {
            ValidateUtc(revokedAtUtc.Value, nameof(revokedAtUtc));
        }

        TimeSpan lifetime = expiresAtUtc - createdAtUtc;
        if (lifetime < MinimumLifetime || lifetime > MaximumLifetime
            || updatedAtUtc < createdAtUtc
            || version < 1
            || (status == TripInvitationStatus.Revoked) != revokedAtUtc.HasValue
            || (revokedAtUtc.HasValue && (revokedAtUtc.Value < createdAtUtc || updatedAtUtc < revokedAtUtc.Value)))
        {
            throw Invalid(TripInvitationErrorCodes.InvalidLifetime, "The trip invitation lifetime is invalid.");
        }

        this.Id = id;
        this.TripPlanId = tripPlanId;
        this.TripTitle = normalizedTripTitle;
        this.TokenHash = tokenHash.Trim();
        this.TokenHint = tokenHint.Trim();
        this.ProposedRole = proposedRole;
        this.InviterMemberId = inviterMemberId;
        this.InviterDisplayName = normalizedDisplayName;
        this.TargetEmailHmac = targetEmailHmac?.Trim();
        this.TargetEmailHmacKeyVersion = targetEmailHmacKeyVersion?.Trim();
        this.Status = status;
        this.PreviewPolicy = previewPolicy;
        this.PeriodPreview = periodPreview ?? throw new ArgumentNullException(nameof(periodPreview));
        this.MemberCountBand = memberCountBand;
        this.ExpiresAtUtc = expiresAtUtc;
        this.RevokedAtUtc = revokedAtUtc;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
        this.Version = version;
    }

    public TripInvitationId Id { get; }
    public TripPlanId TripPlanId { get; }
    public string TripTitle { get; }
    public string TokenHash { get; }
    public string TokenHint { get; }
    public TripDelegatedRole ProposedRole { get; }
    public TripMemberId InviterMemberId { get; }
    public string InviterDisplayName { get; }
    public string? TargetEmailHmac { get; }
    public string? TargetEmailHmacKeyVersion { get; }
    public TripInvitationStatus Status { get; private set; }
    public TripInvitationPreviewPolicy PreviewPolicy { get; }
    public TripInvitationPeriodPreview PeriodPreview { get; }
    public TripInvitationMemberCountBand MemberCountBand { get; }
    public DateTime ExpiresAtUtc { get; }
    public DateTime? RevokedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public bool IsTargeted => this.TargetEmailHmac is not null;

    public static TripInvitation Create(
        TripInvitationId id,
        TripPlanId tripPlanId,
        string tripTitle,
        string tokenHash,
        string tokenHint,
        TripDelegatedRole proposedRole,
        TripMemberId inviterMemberId,
        string inviterDisplayName,
        string? targetEmailHmac,
        string? targetEmailHmacKeyVersion,
        TripInvitationPeriodPreview periodPreview,
        TripInvitationMemberCountBand memberCountBand,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        return new TripInvitation(
            id,
            tripPlanId,
            tripTitle,
            tokenHash,
            tokenHint,
            proposedRole,
            inviterMemberId,
            inviterDisplayName,
            targetEmailHmac,
            targetEmailHmacKeyVersion,
            TripInvitationStatus.Active,
            TripInvitationPreviewPolicy.ApproximatePeriod,
            periodPreview,
            memberCountBand,
            expiresAtUtc,
            null,
            createdAtUtc,
            createdAtUtc,
            1);
    }

    public static TripInvitation Restore(
        TripInvitationId id,
        TripPlanId tripPlanId,
        string tripTitle,
        string tokenHash,
        string tokenHint,
        TripDelegatedRole proposedRole,
        TripMemberId inviterMemberId,
        string inviterDisplayName,
        string? targetEmailHmac,
        string? targetEmailHmacKeyVersion,
        TripInvitationStatus status,
        TripInvitationPreviewPolicy previewPolicy,
        TripInvitationPeriodPreview periodPreview,
        TripInvitationMemberCountBand memberCountBand,
        DateTime expiresAtUtc,
        DateTime? revokedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version)
    {
        return new TripInvitation(
            id,
            tripPlanId,
            tripTitle,
            tokenHash,
            tokenHint,
            proposedRole,
            inviterMemberId,
            inviterDisplayName,
            targetEmailHmac,
            targetEmailHmacKeyVersion,
            status,
            previewPolicy,
            periodPreview,
            memberCountBand,
            expiresAtUtc,
            revokedAtUtc,
            createdAtUtc,
            updatedAtUtc,
            version);
    }

    public bool IsPubliclyResolvable(DateTime nowUtc)
    {
        ValidateUtc(nowUtc, nameof(nowUtc));
        return this.Status == TripInvitationStatus.Active && nowUtc < this.ExpiresAtUtc;
    }

    public void Revoke(DateTime nowUtc)
    {
        ValidateUtc(nowUtc, nameof(nowUtc));
        if (this.Status == TripInvitationStatus.Revoked)
        {
            return;
        }

        if (this.Status != TripInvitationStatus.Active || nowUtc < this.CreatedAtUtc || this.Version == long.MaxValue)
        {
            throw Invalid(TripInvitationErrorCodes.InvalidState, "The trip invitation cannot be revoked.");
        }

        this.Status = TripInvitationStatus.Revoked;
        this.RevokedAtUtc = nowUtc;
        this.UpdatedAtUtc = nowUtc;
        this.Version++;
    }

    public static TripInvitationMemberCountBand ResolveMemberCountBand(int activeMemberCount)
    {
        return activeMemberCount switch
        {
            1 => TripInvitationMemberCountBand.One,
            >= 2 and <= 5 => TripInvitationMemberCountBand.TwoToFive,
            >= 6 and <= 10 => TripInvitationMemberCountBand.SixToTen,
            >= 11 and <= 50 => TripInvitationMemberCountBand.ElevenToFifty,
            _ => throw Invalid(TripInvitationErrorCodes.InvalidPreview, "The member count is outside the supported range."),
        };
    }

    private static void ValidateToken(string tokenHash, string tokenHint)
    {
        if (string.IsNullOrWhiteSpace(tokenHash)
            || tokenHash.Trim().Length > 128
            || string.IsNullOrWhiteSpace(tokenHint)
            || tokenHint.Trim().Length > 12)
        {
            throw Invalid(TripInvitationErrorCodes.InvalidToken, "The trip invitation token material is invalid.");
        }
    }

    private static void ValidateUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The timestamp must use UTC.", parameterName);
        }
    }

    private static TripInvitationValidationException Invalid(string code, string message)
    {
        return new TripInvitationValidationException(code, message);
    }
}
