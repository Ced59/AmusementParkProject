using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Sharing;

public sealed class ProfileComparison
{
    private ProfileComparison(
        ProfileComparisonId id,
        ProfileComparisonInvitationId invitationId,
        ShareToken shareToken,
        string creatorUserId,
        string acceptorUserId,
        SharePublicationId creatorPassportPublicationId,
        long creatorPassportPublicationVersion,
        SharePublicationId acceptorPassportPublicationId,
        long acceptorPassportPublicationVersion,
        ProfileComparisonCalculation calculation,
        ProfileComparisonStatus status,
        string? revokedByUserId,
        DateTime? revokedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version,
        bool isModerationSuspended)
    {
        _ = id.Value;
        _ = invitationId.Value;
        _ = shareToken.Value;
        _ = creatorPassportPublicationId.Value;
        _ = acceptorPassportPublicationId.Value;
        string normalizedCreatorUserId = IdentifierRules.NormalizeRequired(
            creatorUserId,
            nameof(creatorUserId));
        string normalizedAcceptorUserId = IdentifierRules.NormalizeRequired(
            acceptorUserId,
            nameof(acceptorUserId));
        if (string.Equals(normalizedCreatorUserId, normalizedAcceptorUserId, StringComparison.Ordinal)
            || creatorPassportPublicationVersion <= 0
            || acceptorPassportPublicationVersion <= 0
            || !IsValidCalculation(calculation)
            || version < 0)
        {
            throw InvalidState();
        }

        ValidateUtc(createdAtUtc, nameof(createdAtUtc));
        ValidateUtc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < createdAtUtc || !Enum.IsDefined(status))
        {
            throw InvalidState();
        }

        if (revokedAtUtc.HasValue)
        {
            ValidateUtc(revokedAtUtc.Value, nameof(revokedAtUtc));
        }

        bool hasRevocation = !string.IsNullOrWhiteSpace(revokedByUserId) && revokedAtUtc.HasValue;
        if ((status == ProfileComparisonStatus.Revoked) != hasRevocation
            || isModerationSuspended && status == ProfileComparisonStatus.Revoked
            || hasRevocation && (!IsParticipant(
                normalizedCreatorUserId,
                normalizedAcceptorUserId,
                revokedByUserId!)
                || revokedAtUtc!.Value < createdAtUtc
                || updatedAtUtc < revokedAtUtc.Value))
        {
            throw InvalidState();
        }

        this.Id = id;
        this.InvitationId = invitationId;
        this.ShareToken = shareToken;
        this.CreatorUserId = normalizedCreatorUserId;
        this.AcceptorUserId = normalizedAcceptorUserId;
        this.CreatorPassportPublicationId = creatorPassportPublicationId;
        this.CreatorPassportPublicationVersion = creatorPassportPublicationVersion;
        this.AcceptorPassportPublicationId = acceptorPassportPublicationId;
        this.AcceptorPassportPublicationVersion = acceptorPassportPublicationVersion;
        this.Calculation = calculation;
        this.Status = status;
        this.RevokedByUserId = revokedByUserId?.Trim();
        this.RevokedAtUtc = revokedAtUtc;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
        this.Version = version;
        this.IsModerationSuspended = isModerationSuspended;
    }

    public ProfileComparisonId Id { get; }

    public ProfileComparisonInvitationId InvitationId { get; }

    public ShareToken ShareToken { get; }

    public string CreatorUserId { get; }

    public string AcceptorUserId { get; }

    public SharePublicationId CreatorPassportPublicationId { get; }

    public long CreatorPassportPublicationVersion { get; }

    public SharePublicationId AcceptorPassportPublicationId { get; }

    public long AcceptorPassportPublicationVersion { get; }

    public ProfileComparisonCalculation Calculation { get; }

    public ProfileComparisonStatus Status { get; private set; }

    public string? RevokedByUserId { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public bool IsModerationSuspended { get; private set; }

    public bool IsActive => this.Status == ProfileComparisonStatus.Active;

    public bool IsPubliclyResolvable => this.IsActive && !this.IsModerationSuspended;

    public static ProfileComparison Create(
        ProfileComparisonId id,
        ProfileComparisonInvitationId invitationId,
        ShareToken shareToken,
        string creatorUserId,
        string acceptorUserId,
        SharePublicationId creatorPassportPublicationId,
        long creatorPassportPublicationVersion,
        SharePublicationId acceptorPassportPublicationId,
        long acceptorPassportPublicationVersion,
        ProfileComparisonCalculation calculation,
        DateTime createdAtUtc)
    {
        return new ProfileComparison(
            id,
            invitationId,
            shareToken,
            creatorUserId,
            acceptorUserId,
            creatorPassportPublicationId,
            creatorPassportPublicationVersion,
            acceptorPassportPublicationId,
            acceptorPassportPublicationVersion,
            calculation,
            ProfileComparisonStatus.Active,
            null,
            null,
            createdAtUtc,
            createdAtUtc,
            0,
            false);
    }

    public static ProfileComparison Restore(
        ProfileComparisonId id,
        ProfileComparisonInvitationId invitationId,
        ShareToken shareToken,
        string creatorUserId,
        string acceptorUserId,
        SharePublicationId creatorPassportPublicationId,
        long creatorPassportPublicationVersion,
        SharePublicationId acceptorPassportPublicationId,
        long acceptorPassportPublicationVersion,
        ProfileComparisonCalculation calculation,
        ProfileComparisonStatus status,
        string? revokedByUserId,
        DateTime? revokedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version,
        bool isModerationSuspended = false)
    {
        return new ProfileComparison(
            id,
            invitationId,
            shareToken,
            creatorUserId,
            acceptorUserId,
            creatorPassportPublicationId,
            creatorPassportPublicationVersion,
            acceptorPassportPublicationId,
            acceptorPassportPublicationVersion,
            calculation,
            status,
            revokedByUserId,
            revokedAtUtc,
            createdAtUtc,
            updatedAtUtc,
            version,
            isModerationSuspended);
    }

    public bool HasParticipant(string userId)
    {
        string normalizedUserId = userId?.Trim() ?? string.Empty;
        return IsParticipant(this.CreatorUserId, this.AcceptorUserId, normalizedUserId);
    }

    public void Revoke(string userId, DateTime revokedAtUtc)
    {
        string normalizedUserId = userId?.Trim() ?? string.Empty;
        ValidateUtc(revokedAtUtc, nameof(revokedAtUtc));
        if (!this.HasParticipant(normalizedUserId))
        {
            throw new ProfileComparisonValidationException(
                ProfileComparisonErrorCodes.NotParticipant,
                "Only a comparison participant can revoke it.");
        }

        if (!this.IsActive)
        {
            throw new ProfileComparisonValidationException(
                ProfileComparisonErrorCodes.AlreadyRevoked,
                "The profile comparison is already revoked.");
        }

        if (revokedAtUtc < this.CreatedAtUtc || this.Version == long.MaxValue)
        {
            throw InvalidState();
        }

        this.Status = ProfileComparisonStatus.Revoked;
        this.IsModerationSuspended = false;
        this.RevokedByUserId = normalizedUserId;
        this.RevokedAtUtc = revokedAtUtc;
        this.UpdatedAtUtc = revokedAtUtc;
        this.Version++;
    }

    public void SuspendByModeration(DateTime suspendedAtUtc)
    {
        ValidateUtc(suspendedAtUtc, nameof(suspendedAtUtc));
        if (!this.IsActive || this.IsModerationSuspended)
        {
            throw new ProfileComparisonValidationException(
                ProfileComparisonErrorCodes.InvalidState,
                "Only an active public comparison can be suspended by moderation.");
        }

        this.AdvanceModerationState(suspendedAtUtc, true);
    }

    public void RestoreAfterModeration(DateTime restoredAtUtc)
    {
        ValidateUtc(restoredAtUtc, nameof(restoredAtUtc));
        if (!this.IsActive || !this.IsModerationSuspended)
        {
            throw new ProfileComparisonValidationException(
                ProfileComparisonErrorCodes.InvalidState,
                "Only a moderation-suspended comparison can be restored.");
        }

        this.AdvanceModerationState(restoredAtUtc, false);
    }

    private void AdvanceModerationState(DateTime changedAtUtc, bool isSuspended)
    {
        if (changedAtUtc < this.UpdatedAtUtc || this.Version == long.MaxValue)
        {
            throw InvalidState();
        }

        this.IsModerationSuspended = isSuspended;
        this.UpdatedAtUtc = changedAtUtc;
        this.Version++;
    }

    private static bool IsParticipant(string creatorUserId, string acceptorUserId, string userId)
    {
        return string.Equals(creatorUserId, userId, StringComparison.Ordinal)
            || string.Equals(acceptorUserId, userId, StringComparison.Ordinal);
    }

    private static bool IsValidCalculation(ProfileComparisonCalculation? calculation)
    {
        if (calculation is null
            || calculation.Categories is null
            || calculation.Categories.Count == 0
            || calculation.Categories.Count > ProfileComparisonInvitation.MaximumCategories
            || calculation.Categories.Any(static category => !Enum.IsDefined(category))
            || calculation.Categories.Distinct().Count() != calculation.Categories.Count
            || calculation.Parks is null
            || calculation.Ratings is null
            || calculation.Years is null
            || calculation.MissedItems is null
            || calculation.CommonRatingCount != calculation.Ratings.Count
            || calculation.MinimumRatingsForCorrelation < 1
            || string.IsNullOrWhiteSpace(calculation.CalculationVersion)
            || calculation.RatingCorrelation.HasValue
                && (calculation.CommonRatingCount < calculation.MinimumRatingsForCorrelation
                    || !double.IsFinite(calculation.RatingCorrelation.Value)
                    || calculation.RatingCorrelation.Value is < -1d or > 1d))
        {
            return false;
        }

        return calculation.Parks.All(static park =>
                !string.IsNullOrWhiteSpace(park.Name)
                && (!park.CreatorVisitCount.HasValue || park.CreatorVisitCount.Value >= 0)
                && (!park.AcceptorVisitCount.HasValue || park.AcceptorVisitCount.Value >= 0))
            && calculation.Ratings.All(static rating =>
                !string.IsNullOrWhiteSpace(rating.TargetType)
                && !string.IsNullOrWhiteSpace(rating.Name)
                && double.IsFinite(rating.CreatorRating)
                && double.IsFinite(rating.AcceptorRating)
                && double.IsFinite(rating.AbsoluteDifference)
                && rating.AbsoluteDifference >= 0
                && Enum.IsDefined(rating.Affinity))
            && calculation.Years.All(static year =>
                year.Year > 0
                && year.CreatorVisitCount >= 0
                && year.AcceptorVisitCount >= 0
                && (!year.CreatorRideCount.HasValue || year.CreatorRideCount.Value >= 0)
                && (!year.AcceptorRideCount.HasValue || year.AcceptorRideCount.Value >= 0))
            && calculation.MissedItems.All(static item =>
                !string.IsNullOrWhiteSpace(item.Name)
                && !string.IsNullOrWhiteSpace(item.Status)
                && (!item.CreatorOccurrenceCount.HasValue
                    || item.CreatorOccurrenceCount.Value >= 0)
                && (!item.AcceptorOccurrenceCount.HasValue
                    || item.AcceptorOccurrenceCount.Value >= 0));
    }

    private static void ValidateUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The timestamp must use UTC.", parameterName);
        }
    }

    private static ProfileComparisonValidationException InvalidState()
    {
        return new ProfileComparisonValidationException(
            ProfileComparisonErrorCodes.InvalidState,
            "The profile comparison state is invalid.");
    }
}
