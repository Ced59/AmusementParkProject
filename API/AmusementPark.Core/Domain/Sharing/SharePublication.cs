using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Sharing;

/// <summary>
/// Autorité métier commune du cycle de vie d'une publication personnelle révocable.
/// </summary>
public sealed class SharePublication
{
    private SharePublication(
        SharePublicationId id,
        string ownerUserId,
        SharePublicationType type,
        string sourceScopeKey,
        ShareToken? shareToken,
        SharePublicationStatus status,
        ShareVisibility visibility,
        ShareContentPolicy contentPolicy,
        long sourceVersion,
        long publicationVersion,
        long version,
        DateTime? publishedAtUtc,
        DateTime? revokedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        _ = id.Value;
        ValidatePublicationType(type);
        ValidateStatus(status);
        ValidateVisibility(visibility);
        ArgumentNullException.ThrowIfNull(contentPolicy);
        ValidatePolicyType(type, contentPolicy);
        ValidateSourceVersion(sourceVersion);
        ValidatePublicationVersion(publicationVersion);
        ValidateVersion(version);
        ValidateTimestamps(createdAtUtc, updatedAtUtc, publishedAtUtc, revokedAtUtc);

        string normalizedOwnerUserId = IdentifierRules.NormalizeRequired(ownerUserId, nameof(ownerUserId));
        string normalizedSourceScopeKey = IdentifierRules.NormalizeRequired(sourceScopeKey, nameof(sourceScopeKey));
        if (shareToken.HasValue)
        {
            _ = shareToken.Value.Value;
        }

        ValidateRestoredState(
            status,
            visibility,
            shareToken,
            publicationVersion,
            version,
            publishedAtUtc,
            revokedAtUtc);

        this.Id = id;
        this.OwnerUserId = normalizedOwnerUserId;
        this.Type = type;
        this.SourceScopeKey = normalizedSourceScopeKey;
        this.ShareToken = shareToken;
        this.Status = status;
        this.Visibility = visibility;
        this.ContentPolicy = contentPolicy;
        this.SourceVersion = sourceVersion;
        this.PublicationVersion = publicationVersion;
        this.Version = version;
        this.PublishedAtUtc = publishedAtUtc;
        this.RevokedAtUtc = revokedAtUtc;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
    }

    public SharePublicationId Id { get; }

    public string OwnerUserId { get; }

    public SharePublicationType Type { get; }

    public string SourceScopeKey { get; }

    public ShareToken? ShareToken { get; private set; }

    public SharePublicationStatus Status { get; private set; }

    public ShareVisibility Visibility { get; private set; }

    public ShareContentPolicy ContentPolicy { get; private set; }

    public long SourceVersion { get; private set; }

    public long PublicationVersion { get; private set; }

    public long Version { get; private set; }

    public DateTime? PublishedAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public bool IsResolvable => this.Status == SharePublicationStatus.Published
        && this.Visibility is ShareVisibility.Unlisted or ShareVisibility.Public
        && this.ShareToken is not null;

    public static SharePublication Create(
        SharePublicationId id,
        string ownerUserId,
        SharePublicationType type,
        string sourceScopeKey,
        ShareContentPolicy contentPolicy,
        long sourceVersion,
        DateTime nowUtc)
    {
        return new SharePublication(
            id,
            ownerUserId,
            type,
            sourceScopeKey,
            null,
            SharePublicationStatus.Draft,
            ShareVisibility.Private,
            contentPolicy,
            sourceVersion,
            0,
            0,
            null,
            null,
            nowUtc,
            nowUtc);
    }

    public static SharePublication Restore(
        SharePublicationId id,
        string ownerUserId,
        SharePublicationType type,
        string sourceScopeKey,
        ShareToken? shareToken,
        SharePublicationStatus status,
        ShareVisibility visibility,
        ShareContentPolicy contentPolicy,
        long sourceVersion,
        long publicationVersion,
        long version,
        DateTime? publishedAtUtc,
        DateTime? revokedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        return new SharePublication(
            id,
            ownerUserId,
            type,
            sourceScopeKey,
            shareToken,
            status,
            visibility,
            contentPolicy,
            sourceVersion,
            publicationVersion,
            version,
            publishedAtUtc,
            revokedAtUtc,
            createdAtUtc,
            updatedAtUtc);
    }

    public void ReplaceContentPolicy(
        ShareContentPolicy contentPolicy,
        long expectedPublicationVersion,
        DateTime nowUtc)
    {
        this.EnsureNotRevoked();
        ArgumentNullException.ThrowIfNull(contentPolicy);
        ValidatePolicyType(this.Type, contentPolicy);
        this.ValidateExpectedPublicationVersion(expectedPublicationVersion);
        this.ValidateMutationTimestamp(nowUtc);
        if (this.ContentPolicy.HasSameSelectionAs(contentPolicy))
        {
            return;
        }

        if (this.Status == SharePublicationStatus.Published)
        {
            this.EnsurePublicationVersionCanIncrement();
        }

        this.EnsureVersionCanIncrement();

        this.ContentPolicy = contentPolicy;
        if (this.Status == SharePublicationStatus.Published)
        {
            this.PublicationVersion++;
            this.Status = SharePublicationStatus.NeedsReview;
            this.Visibility = ShareVisibility.Private;
        }

        this.Version++;
        this.UpdatedAtUtc = nowUtc;
    }

    public void MarkSourceChanged(long sourceVersion, DateTime nowUtc)
    {
        this.EnsureNotRevoked();
        ValidateSourceVersion(sourceVersion);
        this.ValidateMutationTimestamp(nowUtc);
        if (sourceVersion < this.SourceVersion)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.InvalidSourceVersion,
                "A share source version cannot move backwards.");
        }

        if (sourceVersion == this.SourceVersion)
        {
            return;
        }

        if (this.Status == SharePublicationStatus.Published)
        {
            this.EnsurePublicationVersionCanIncrement();
        }

        this.EnsureVersionCanIncrement();

        this.SourceVersion = sourceVersion;
        if (this.Status == SharePublicationStatus.Published)
        {
            this.PublicationVersion++;
            this.Status = SharePublicationStatus.NeedsReview;
            this.Visibility = ShareVisibility.Private;
        }

        this.Version++;
        this.UpdatedAtUtc = nowUtc;
    }

    public void Publish(
        ShareToken shareToken,
        ShareVisibility visibility,
        long approvedSourceVersion,
        ShareContentPolicy approvedContentPolicy,
        long expectedPublicationVersion,
        DateTime nowUtc)
    {
        if (this.Status is not SharePublicationStatus.Draft and not SharePublicationStatus.NeedsReview)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.InvalidTransition,
                "Only a draft or suspended share publication can be published.");
        }

        ValidateVisibility(visibility);
        if (visibility == ShareVisibility.Private)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.PublicVisibilityRequired,
                "Publishing requires an unlisted or public visibility.");
        }

        this.ValidateExpectedPublicationVersion(expectedPublicationVersion);
        this.ValidateMutationTimestamp(nowUtc);
        ArgumentNullException.ThrowIfNull(approvedContentPolicy);
        ValidatePolicyType(this.Type, approvedContentPolicy);
        if (approvedSourceVersion != this.SourceVersion)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.PreviewSourceVersionMismatch,
                "The approved preview does not match the current source version.");
        }

        if (!this.ContentPolicy.HasSameSelectionAs(approvedContentPolicy))
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.PreviewPolicyMismatch,
                "The approved preview does not match the current content policy.");
        }

        _ = shareToken.Value;
        this.EnsurePublicationVersionCanIncrement();
        this.EnsureVersionCanIncrement();
        this.PublicationVersion++;
        this.Version++;
        this.ShareToken = shareToken;
        this.Status = SharePublicationStatus.Published;
        this.Visibility = visibility;
        this.PublishedAtUtc = nowUtc;
        this.RevokedAtUtc = null;
        this.UpdatedAtUtc = nowUtc;
    }

    public void RotateToken(
        ShareToken shareToken,
        long expectedPublicationVersion,
        DateTime nowUtc)
    {
        if (this.Status != SharePublicationStatus.Published)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.InvalidTransition,
                "Only a published share publication can rotate its link.");
        }

        this.ValidateExpectedPublicationVersion(expectedPublicationVersion);
        this.ValidateMutationTimestamp(nowUtc);
        _ = shareToken.Value;
        if (this.ShareToken == shareToken)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.ShareTokenUnchanged,
                "A rotated share link must use a new token.");
        }

        this.EnsurePublicationVersionCanIncrement();
        this.EnsureVersionCanIncrement();
        this.PublicationVersion++;
        this.Version++;
        this.ShareToken = shareToken;
        this.UpdatedAtUtc = nowUtc;
    }

    public void Revoke(long expectedPublicationVersion, DateTime nowUtc)
    {
        if (this.Status == SharePublicationStatus.Draft)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.InvalidTransition,
                "A draft share publication has no public link to revoke.");
        }

        this.ValidateExpectedPublicationVersion(expectedPublicationVersion);
        this.ValidateMutationTimestamp(nowUtc);
        if (this.Status == SharePublicationStatus.Revoked)
        {
            return;
        }

        this.EnsurePublicationVersionCanIncrement();
        this.EnsureVersionCanIncrement();
        this.PublicationVersion++;
        this.Version++;
        this.Status = SharePublicationStatus.Revoked;
        this.Visibility = ShareVisibility.Private;
        this.ShareToken = null;
        this.RevokedAtUtc = nowUtc;
        this.UpdatedAtUtc = nowUtc;
    }

    private static void ValidatePublicationType(SharePublicationType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.InvalidPublicationType,
                "The share publication type is invalid.");
        }
    }

    private static void ValidateStatus(SharePublicationStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.InvalidStatus,
                "The share publication status is invalid.");
        }
    }

    private static void ValidateVisibility(ShareVisibility visibility)
    {
        if (!Enum.IsDefined(visibility))
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.InvalidVisibility,
                "The share publication visibility is invalid.");
        }
    }

    private static void ValidatePolicyType(
        SharePublicationType type,
        ShareContentPolicy contentPolicy)
    {
        if (contentPolicy.PublicationType != type)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.PolicyTypeMismatch,
                "The content policy must target the same publication type.");
        }
    }

    private static void ValidateSourceVersion(long sourceVersion)
    {
        if (sourceVersion < 0)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.InvalidSourceVersion,
                "The share source version cannot be negative.");
        }
    }

    private static void ValidatePublicationVersion(long publicationVersion)
    {
        if (publicationVersion < 0)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.InvalidPublicationVersion,
                "The share publication version cannot be negative.");
        }
    }

    private static void ValidateVersion(long version)
    {
        if (version < 0)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.InvalidVersion,
                "The share publication persistence version cannot be negative.");
        }
    }

    private static void ValidateTimestamps(
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime? publishedAtUtc,
        DateTime? revokedAtUtc)
    {
        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);
        if (publishedAtUtc.HasValue)
        {
            EnsureUtc(publishedAtUtc.Value);
        }

        if (revokedAtUtc.HasValue)
        {
            EnsureUtc(revokedAtUtc.Value);
        }

        if (updatedAtUtc < createdAtUtc
            || publishedAtUtc.HasValue
            && (publishedAtUtc.Value < createdAtUtc || publishedAtUtc.Value > updatedAtUtc)
            || revokedAtUtc.HasValue
            && (!publishedAtUtc.HasValue
                || revokedAtUtc.Value < publishedAtUtc.Value
                || revokedAtUtc.Value > updatedAtUtc))
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.InvalidTimestampOrder,
                "The share publication timestamps are not chronologically consistent.");
        }
    }

    private static void ValidateRestoredState(
        SharePublicationStatus status,
        ShareVisibility visibility,
        ShareToken? shareToken,
        long publicationVersion,
        long version,
        DateTime? publishedAtUtc,
        DateTime? revokedAtUtc)
    {
        bool isValid = publicationVersion <= version
            && (status switch
            {
                SharePublicationStatus.Draft => visibility == ShareVisibility.Private
                    && shareToken is null
                    && publicationVersion == 0
                    && !publishedAtUtc.HasValue
                    && !revokedAtUtc.HasValue,
                SharePublicationStatus.Published => visibility is ShareVisibility.Unlisted or ShareVisibility.Public
                    && shareToken is not null
                    && publicationVersion > 0
                    && publishedAtUtc.HasValue
                    && !revokedAtUtc.HasValue,
                SharePublicationStatus.NeedsReview => visibility == ShareVisibility.Private
                    && shareToken is not null
                    && publicationVersion > 0
                    && publishedAtUtc.HasValue
                    && !revokedAtUtc.HasValue,
                SharePublicationStatus.Revoked => visibility == ShareVisibility.Private
                    && shareToken is null
                    && publicationVersion > 0
                    && publishedAtUtc.HasValue
                    && revokedAtUtc.HasValue,
                _ => false,
            });
        if (!isValid)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.InvalidRestoredState,
                "The restored share publication state is inconsistent.");
        }
    }

    private static void EnsureUtc(DateTime timestamp)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.TimestampNotUtc,
                "Share publication timestamps must be expressed in UTC.");
        }
    }

    private static SharePublicationValidationException CreateValidationException(
        string errorCode,
        string message)
    {
        return new SharePublicationValidationException(errorCode, message);
    }

    private void EnsureNotRevoked()
    {
        if (this.Status == SharePublicationStatus.Revoked)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.InvalidTransition,
                "A revoked share publication is terminal.");
        }
    }

    private void ValidateExpectedPublicationVersion(long expectedPublicationVersion)
    {
        if (expectedPublicationVersion != this.PublicationVersion)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.PublicationVersionConflict,
                "The share publication changed since it was read.");
        }
    }

    private void ValidateMutationTimestamp(DateTime nowUtc)
    {
        EnsureUtc(nowUtc);
        if (nowUtc < this.UpdatedAtUtc)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.InvalidTimestampOrder,
                "A share publication mutation cannot predate the current state.");
        }
    }

    private void EnsurePublicationVersionCanIncrement()
    {
        if (this.PublicationVersion == long.MaxValue)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.PublicationVersionOverflow,
                "The share publication version cannot be incremented further.");
        }
    }

    private void EnsureVersionCanIncrement()
    {
        if (this.Version == long.MaxValue)
        {
            throw CreateValidationException(
                SharePublicationErrorCodes.VersionOverflow,
                "The share publication persistence version cannot be incremented further.");
        }
    }
}
