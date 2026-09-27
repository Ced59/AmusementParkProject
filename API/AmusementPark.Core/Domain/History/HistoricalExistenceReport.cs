using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Core.Domain.History;

public sealed class HistoricalExistenceReport
{
    public const int MaximumClaimedNameLength = 250;
    public const int MaximumSourceUrlLength = 1000;
    public const int MaximumSourceReferenceLength = 1000;
    public const int MaximumDetailsLength = 1000;
    public const int MaximumDecisionNoteLength = 500;

    private HistoricalExistenceReport(
        HistoricalExistenceReportId id,
        string ownerUserId,
        VisitId visitId,
        string parkId,
        string parkName,
        VisitDate visitDate,
        string claimedName,
        string? sourceUrl,
        string? sourceReference,
        string? details,
        HistoricalExistenceReportStatus status,
        DateTime submittedAtUtc,
        string? reviewedByUserId,
        DateTime? reviewedAtUtc,
        string? decisionNote,
        long revision)
    {
        _ = id.Value;
        _ = visitId.Value;
        ArgumentNullException.ThrowIfNull(visitDate);

        string normalizedOwner = IdentifierRules.NormalizeRequired(ownerUserId, nameof(ownerUserId));
        string normalizedParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        string normalizedParkName = NormalizeRequired(parkName, 250);
        string normalizedClaimedName = NormalizeRequired(claimedName, MaximumClaimedNameLength);
        string? normalizedSourceUrl = NormalizeOptional(sourceUrl, MaximumSourceUrlLength);
        string? normalizedSourceReference = NormalizeOptional(
            sourceReference,
            MaximumSourceReferenceLength);
        string? normalizedDetails = NormalizeOptional(details, MaximumDetailsLength);
        string? normalizedReviewer = NormalizeOptional(reviewedByUserId, 200);
        string? normalizedDecisionNote = NormalizeOptional(decisionNote, MaximumDecisionNoteLength);

        if (!Enum.IsDefined(status)
            || revision < 0
            || submittedAtUtc.Kind != DateTimeKind.Utc
            || !IsSafePlainText(normalizedClaimedName)
            || !IsSafePlainText(normalizedParkName)
            || !IsSafePlainText(normalizedSourceReference)
            || !IsSafePlainText(normalizedDetails)
            || !IsSafePlainText(normalizedDecisionNote)
            || normalizedSourceUrl is not null && !IsSafeHttpsUrl(normalizedSourceUrl))
        {
            throw new ArgumentException("The historical existence report is invalid.");
        }

        bool isPending = status == HistoricalExistenceReportStatus.Pending;
        if (isPending != (normalizedReviewer is null
                && !reviewedAtUtc.HasValue
                && normalizedDecisionNote is null)
            || !isPending && (normalizedReviewer is null
                || !reviewedAtUtc.HasValue
                || reviewedAtUtc.Value.Kind != DateTimeKind.Utc
                || reviewedAtUtc.Value < submittedAtUtc))
        {
            throw new ArgumentException("The historical existence report review state is invalid.");
        }

        this.Id = id;
        this.OwnerUserId = normalizedOwner;
        this.VisitId = visitId;
        this.ParkId = normalizedParkId;
        this.ParkName = normalizedParkName;
        this.VisitDate = visitDate;
        this.ClaimedName = normalizedClaimedName;
        this.NormalizedClaimedName = normalizedClaimedName.ToUpperInvariant();
        this.SourceUrl = normalizedSourceUrl;
        this.SourceReference = normalizedSourceReference;
        this.Details = normalizedDetails;
        this.Status = status;
        this.SubmittedAtUtc = submittedAtUtc;
        this.ReviewedByUserId = normalizedReviewer;
        this.ReviewedAtUtc = reviewedAtUtc;
        this.DecisionNote = normalizedDecisionNote;
        this.Revision = revision;
    }

    public HistoricalExistenceReportId Id { get; }
    public string OwnerUserId { get; }
    public VisitId VisitId { get; }
    public string ParkId { get; }
    public string ParkName { get; }
    public VisitDate VisitDate { get; }
    public string ClaimedName { get; }
    public string NormalizedClaimedName { get; }
    public string? SourceUrl { get; }
    public string? SourceReference { get; }
    public string? Details { get; }
    public HistoricalExistenceReportStatus Status { get; private set; }
    public DateTime SubmittedAtUtc { get; }
    public string? ReviewedByUserId { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? DecisionNote { get; private set; }
    public long Revision { get; private set; }

    public static HistoricalExistenceReport Create(
        HistoricalExistenceReportId id,
        string ownerUserId,
        VisitId visitId,
        string parkId,
        string parkName,
        VisitDate visitDate,
        string claimedName,
        string? sourceUrl,
        string? sourceReference,
        string? details,
        DateTime submittedAtUtc)
    {
        return new HistoricalExistenceReport(
            id,
            ownerUserId,
            visitId,
            parkId,
            parkName,
            visitDate,
            claimedName,
            sourceUrl,
            sourceReference,
            details,
            HistoricalExistenceReportStatus.Pending,
            submittedAtUtc,
            null,
            null,
            null,
            0);
    }

    public static HistoricalExistenceReport Restore(
        HistoricalExistenceReportId id,
        string ownerUserId,
        VisitId visitId,
        string parkId,
        string parkName,
        VisitDate visitDate,
        string claimedName,
        string? sourceUrl,
        string? sourceReference,
        string? details,
        HistoricalExistenceReportStatus status,
        DateTime submittedAtUtc,
        string? reviewedByUserId,
        DateTime? reviewedAtUtc,
        string? decisionNote,
        long revision)
    {
        return new HistoricalExistenceReport(
            id,
            ownerUserId,
            visitId,
            parkId,
            parkName,
            visitDate,
            claimedName,
            sourceUrl,
            sourceReference,
            details,
            status,
            submittedAtUtc,
            reviewedByUserId,
            reviewedAtUtc,
            decisionNote,
            revision);
    }

    public void AcceptForResearch(
        string reviewerUserId,
        string? decisionNote,
        DateTime reviewedAtUtc)
    {
        this.Review(
            HistoricalExistenceReportStatus.AcceptedForResearch,
            reviewerUserId,
            decisionNote,
            reviewedAtUtc);
    }

    public void Dismiss(string reviewerUserId, string? decisionNote, DateTime reviewedAtUtc)
    {
        this.Review(
            HistoricalExistenceReportStatus.Dismissed,
            reviewerUserId,
            decisionNote,
            reviewedAtUtc);
    }

    private void Review(
        HistoricalExistenceReportStatus targetStatus,
        string reviewerUserId,
        string? decisionNote,
        DateTime reviewedAtUtc)
    {
        if (this.Status != HistoricalExistenceReportStatus.Pending || this.Revision == long.MaxValue)
        {
            throw new InvalidOperationException("This historical existence report was reviewed.");
        }

        string normalizedReviewer = IdentifierRules.NormalizeRequired(
            reviewerUserId,
            nameof(reviewerUserId));
        string? normalizedDecisionNote = NormalizeOptional(decisionNote, MaximumDecisionNoteLength);
        if (reviewedAtUtc.Kind != DateTimeKind.Utc
            || reviewedAtUtc < this.SubmittedAtUtc
            || !IsSafePlainText(normalizedDecisionNote))
        {
            throw new ArgumentException("The historical existence report decision is invalid.");
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
            throw new ArgumentException("A required historical report value is invalid.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maximumLength)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException("An optional historical report value is too long.");
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
