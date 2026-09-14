namespace AmusementPark.Core.Domain.ParkFit;

public sealed class ParkFitSourceReport
{
    public const int MaximumDetailsLength = 500;
    public const int MaximumDecisionNoteLength = 500;
    public const int MaximumSourceValueLength = 1000;

    private ParkFitSourceReport(
        ParkFitSourceReportId id,
        string parkId,
        string parkName,
        ParkFitEvidenceKind evidenceKind,
        string? sourceUrl,
        string? sourceReference,
        ParkFitSourceReportReason reason,
        string? details,
        ParkFitSourceReportStatus status,
        DateTime submittedAtUtc,
        string? reviewedByUserId,
        DateTime? reviewedAtUtc,
        string? decisionNote,
        long revision)
    {
        _ = id.Value;
        string normalizedParkId = NormalizeRequired(parkId, 200);
        string normalizedParkName = NormalizeRequired(parkName, 250);
        string? normalizedSourceUrl = NormalizeOptional(sourceUrl, MaximumSourceValueLength);
        string? normalizedSourceReference = NormalizeOptional(sourceReference, MaximumSourceValueLength);
        string? normalizedDetails = NormalizeOptional(details, MaximumDetailsLength);
        string? normalizedReviewer = NormalizeOptional(reviewedByUserId, 200);
        string? normalizedDecisionNote = NormalizeOptional(decisionNote, MaximumDecisionNoteLength);
        if (!Enum.IsDefined(evidenceKind)
            || !Enum.IsDefined(reason)
            || !Enum.IsDefined(status)
            || revision < 0
            || submittedAtUtc.Kind != DateTimeKind.Utc
            || reason == ParkFitSourceReportReason.Other && normalizedDetails is null
            || !IsSafePlainText(normalizedSourceReference)
            || !IsSafePlainText(normalizedDetails)
            || !IsSafePlainText(normalizedDecisionNote)
            || normalizedSourceUrl is not null && !IsSafeHttpsUrl(normalizedSourceUrl))
        {
            throw new ArgumentException("The Park Fit source report is invalid.");
        }

        bool isPending = status == ParkFitSourceReportStatus.Pending;
        if (isPending != (normalizedReviewer is null
                && !reviewedAtUtc.HasValue
                && normalizedDecisionNote is null)
            || !isPending && (normalizedReviewer is null
                || !reviewedAtUtc.HasValue
                || reviewedAtUtc.Value.Kind != DateTimeKind.Utc
                || reviewedAtUtc.Value < submittedAtUtc))
        {
            throw new ArgumentException("The Park Fit source report review state is invalid.");
        }

        this.Id = id;
        this.ParkId = normalizedParkId;
        this.ParkName = normalizedParkName;
        this.EvidenceKind = evidenceKind;
        this.SourceUrl = normalizedSourceUrl;
        this.SourceReference = normalizedSourceReference;
        this.Reason = reason;
        this.Details = normalizedDetails;
        this.Status = status;
        this.SubmittedAtUtc = submittedAtUtc;
        this.ReviewedByUserId = normalizedReviewer;
        this.ReviewedAtUtc = reviewedAtUtc;
        this.DecisionNote = normalizedDecisionNote;
        this.Revision = revision;
    }

    public ParkFitSourceReportId Id { get; }
    public string ParkId { get; }
    public string ParkName { get; }
    public ParkFitEvidenceKind EvidenceKind { get; }
    public string? SourceUrl { get; }
    public string? SourceReference { get; }
    public ParkFitSourceReportReason Reason { get; }
    public string? Details { get; }
    public ParkFitSourceReportStatus Status { get; private set; }
    public DateTime SubmittedAtUtc { get; }
    public string? ReviewedByUserId { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? DecisionNote { get; private set; }
    public long Revision { get; private set; }

    public static ParkFitSourceReport Create(
        ParkFitSourceReportId id,
        string parkId,
        string parkName,
        ParkFitEvidenceKind evidenceKind,
        string? sourceUrl,
        string? sourceReference,
        ParkFitSourceReportReason reason,
        string? details,
        DateTime submittedAtUtc)
    {
        return new ParkFitSourceReport(
            id,
            parkId,
            parkName,
            evidenceKind,
            sourceUrl,
            sourceReference,
            reason,
            details,
            ParkFitSourceReportStatus.Pending,
            submittedAtUtc,
            null,
            null,
            null,
            0);
    }

    public static ParkFitSourceReport Restore(
        ParkFitSourceReportId id,
        string parkId,
        string parkName,
        ParkFitEvidenceKind evidenceKind,
        string? sourceUrl,
        string? sourceReference,
        ParkFitSourceReportReason reason,
        string? details,
        ParkFitSourceReportStatus status,
        DateTime submittedAtUtc,
        string? reviewedByUserId,
        DateTime? reviewedAtUtc,
        string? decisionNote,
        long revision)
    {
        return new ParkFitSourceReport(
            id,
            parkId,
            parkName,
            evidenceKind,
            sourceUrl,
            sourceReference,
            reason,
            details,
            status,
            submittedAtUtc,
            reviewedByUserId,
            reviewedAtUtc,
            decisionNote,
            revision);
    }

    public void Resolve(string reviewerUserId, string? decisionNote, DateTime reviewedAtUtc)
    {
        this.Review(ParkFitSourceReportStatus.Resolved, reviewerUserId, decisionNote, reviewedAtUtc);
    }

    public void Dismiss(string reviewerUserId, string? decisionNote, DateTime reviewedAtUtc)
    {
        this.Review(ParkFitSourceReportStatus.Dismissed, reviewerUserId, decisionNote, reviewedAtUtc);
    }

    private void Review(
        ParkFitSourceReportStatus targetStatus,
        string reviewerUserId,
        string? decisionNote,
        DateTime reviewedAtUtc)
    {
        if (this.Status != ParkFitSourceReportStatus.Pending || this.Revision == long.MaxValue)
        {
            throw new InvalidOperationException("This Park Fit source report has already been reviewed.");
        }

        string normalizedReviewer = NormalizeRequired(reviewerUserId, 200);
        string? normalizedDecisionNote = NormalizeOptional(decisionNote, MaximumDecisionNoteLength);
        if (reviewedAtUtc.Kind != DateTimeKind.Utc
            || reviewedAtUtc < this.SubmittedAtUtc
            || !IsSafePlainText(normalizedDecisionNote))
        {
            throw new ArgumentException("The Park Fit source report decision is invalid.");
        }

        this.Status = targetStatus;
        this.ReviewedByUserId = normalizedReviewer;
        this.ReviewedAtUtc = reviewedAtUtc;
        this.DecisionNote = normalizedDecisionNote;
        this.Revision++;
    }

    private static string NormalizeRequired(string? value, int maximumLength)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > maximumLength)
        {
            throw new ArgumentException("A required Park Fit report value is invalid.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maximumLength)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException("An optional Park Fit report value is too long.");
        }

        return normalized.Length == 0 ? null : normalized;
    }

    private static bool IsSafePlainText(string? value)
    {
        return string.IsNullOrEmpty(value)
            || !value.Any(static character => char.IsControl(character)
                && character is not '\r' and not '\n' and not '\t')
                && !value.Contains('<', StringComparison.Ordinal)
                && !value.Contains('>', StringComparison.Ordinal);
    }

    private static bool IsSafeHttpsUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(uri.Host)
            && string.IsNullOrEmpty(uri.UserInfo);
    }
}
