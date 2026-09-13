using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Sharing;

public sealed class ShareModerationReport
{
    public const int MaximumDetailsLength = 500;
    public const int MaximumDecisionNoteLength = 500;

    private ShareModerationReport(
        ShareModerationReportId id,
        ShareModerationTargetType targetType,
        string targetRecordId,
        ShareModerationReason reason,
        string? details,
        ShareModerationReportStatus status,
        DateTime submittedAtUtc,
        string? reviewedByUserId,
        DateTime? reviewedAtUtc,
        string? decisionNote,
        long version)
    {
        _ = id.Value;
        string normalizedTargetRecordId = IdentifierRules.NormalizeRequired(
            targetRecordId,
            nameof(targetRecordId));
        string? normalizedDetails = NormalizeOptional(details, MaximumDetailsLength);
        string? normalizedReviewer = NormalizeOptionalIdentifier(reviewedByUserId);
        string? normalizedDecisionNote = NormalizeOptional(
            decisionNote,
            MaximumDecisionNoteLength);
        ValidateUtc(submittedAtUtc);
        if (!Enum.IsDefined(targetType)
            || !Enum.IsDefined(reason)
            || !Enum.IsDefined(status)
            || version < 0
            || reason == ShareModerationReason.Other && normalizedDetails is null
            || !PublicShareTextSafetyPolicy.IsSafePlainText(normalizedDetails)
            || !PublicShareTextSafetyPolicy.IsSafePlainText(normalizedDecisionNote))
        {
            throw InvalidState();
        }

        if (reviewedAtUtc.HasValue)
        {
            ValidateUtc(reviewedAtUtc.Value);
        }

        bool isPending = status == ShareModerationReportStatus.Pending;
        if (isPending != (normalizedReviewer is null
                && !reviewedAtUtc.HasValue
                && normalizedDecisionNote is null)
            || !isPending && (normalizedReviewer is null
                || !reviewedAtUtc.HasValue
                || reviewedAtUtc.Value < submittedAtUtc))
        {
            throw InvalidState();
        }

        this.Id = id;
        this.TargetType = targetType;
        this.TargetRecordId = normalizedTargetRecordId;
        this.Reason = reason;
        this.Details = normalizedDetails;
        this.Status = status;
        this.SubmittedAtUtc = submittedAtUtc;
        this.ReviewedByUserId = normalizedReviewer;
        this.ReviewedAtUtc = reviewedAtUtc;
        this.DecisionNote = normalizedDecisionNote;
        this.Version = version;
    }

    public ShareModerationReportId Id { get; }

    public ShareModerationTargetType TargetType { get; }

    public string TargetRecordId { get; }

    public ShareModerationReason Reason { get; }

    public string? Details { get; }

    public ShareModerationReportStatus Status { get; private set; }

    public DateTime SubmittedAtUtc { get; }

    public string? ReviewedByUserId { get; private set; }

    public DateTime? ReviewedAtUtc { get; private set; }

    public string? DecisionNote { get; private set; }

    public long Version { get; private set; }

    public static ShareModerationReport Create(
        ShareModerationReportId id,
        ShareModerationTargetType targetType,
        string targetRecordId,
        ShareModerationReason reason,
        string? details,
        DateTime submittedAtUtc)
    {
        return new ShareModerationReport(
            id,
            targetType,
            targetRecordId,
            reason,
            details,
            ShareModerationReportStatus.Pending,
            submittedAtUtc,
            null,
            null,
            null,
            0);
    }

    public static ShareModerationReport Restore(
        ShareModerationReportId id,
        ShareModerationTargetType targetType,
        string targetRecordId,
        ShareModerationReason reason,
        string? details,
        ShareModerationReportStatus status,
        DateTime submittedAtUtc,
        string? reviewedByUserId,
        DateTime? reviewedAtUtc,
        string? decisionNote,
        long version)
    {
        return new ShareModerationReport(
            id,
            targetType,
            targetRecordId,
            reason,
            details,
            status,
            submittedAtUtc,
            reviewedByUserId,
            reviewedAtUtc,
            decisionNote,
            version);
    }

    public void Dismiss(string reviewerUserId, string? note, DateTime reviewedAtUtc)
    {
        this.CompleteReview(
            ShareModerationReportStatus.Dismissed,
            reviewerUserId,
            note,
            reviewedAtUtc,
            ShareModerationReportStatus.Pending);
    }

    public void MarkPublicationSuspended(
        string reviewerUserId,
        string? note,
        DateTime reviewedAtUtc)
    {
        this.CompleteReview(
            ShareModerationReportStatus.PublicationSuspended,
            reviewerUserId,
            note,
            reviewedAtUtc,
            ShareModerationReportStatus.Pending);
    }

    public void MarkPublicationRestored(
        string reviewerUserId,
        string? note,
        DateTime reviewedAtUtc)
    {
        this.CompleteReview(
            ShareModerationReportStatus.PublicationRestored,
            reviewerUserId,
            note,
            reviewedAtUtc,
            ShareModerationReportStatus.PublicationSuspended);
    }

    private void CompleteReview(
        ShareModerationReportStatus targetStatus,
        string reviewerUserId,
        string? note,
        DateTime reviewedAtUtc,
        ShareModerationReportStatus requiredCurrentStatus)
    {
        if (this.Status != requiredCurrentStatus || this.Version == long.MaxValue)
        {
            throw new ShareModerationValidationException(
                ShareModerationErrorCodes.InvalidTransition,
                "The moderation report cannot perform this transition.");
        }

        string normalizedReviewer = IdentifierRules.NormalizeRequired(
            reviewerUserId,
            nameof(reviewerUserId));
        string? normalizedNote = NormalizeOptional(note, MaximumDecisionNoteLength);
        ValidateUtc(reviewedAtUtc);
        if (reviewedAtUtc < this.SubmittedAtUtc
            || !PublicShareTextSafetyPolicy.IsSafePlainText(normalizedNote))
        {
            throw InvalidState();
        }

        this.Status = targetStatus;
        this.ReviewedByUserId = normalizedReviewer;
        this.ReviewedAtUtc = reviewedAtUtc;
        this.DecisionNote = normalizedNote;
        this.Version++;
    }

    private static string? NormalizeOptional(string? value, int maximumLength)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length > maximumLength)
        {
            throw InvalidState();
        }

        return normalized.Length == 0 ? null : normalized;
    }

    private static string? NormalizeOptionalIdentifier(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : IdentifierRules.NormalizeRequired(value, nameof(value));
    }

    private static void ValidateUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw InvalidState();
        }
    }

    private static ShareModerationValidationException InvalidState()
    {
        return new ShareModerationValidationException(
            ShareModerationErrorCodes.InvalidState,
            "The moderation report state is invalid.");
    }
}
