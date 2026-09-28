using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.LiveData;

public sealed class ExternalLiveTargetMapping
{
    private const int MaximumReviewNoteLength = 2000;

    public ExternalLiveTargetMapping(
        Guid id,
        LiveDataSourceId sourceId,
        ExternalLiveTargetDescriptor externalTarget,
        LiveTargetReference? target,
        LiveMappingStatus status,
        LiveMappingConfidence confidence,
        DateTime validFromUtc,
        DateTime? validToUtc,
        int revision,
        int? supersedesRevision,
        string? reviewedByUserId,
        string? reviewNote,
        DateTime recordedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidMapping,
                "A live target mapping requires an identifier.",
                nameof(id));
        }

        _ = sourceId.Value;
        ArgumentNullException.ThrowIfNull(externalTarget);
        ValidateEnum(status, nameof(status));
        ValidateEnum(confidence, nameof(confidence));
        EnsureUtc(validFromUtc, nameof(validFromUtc));
        EnsureOptionalUtc(validToUtc, nameof(validToUtc));
        EnsureUtc(recordedAtUtc, nameof(recordedAtUtc));
        ValidateRevision(revision, supersedesRevision);
        string? normalizedReviewer = NormalizeOptionalIdentifier(
            reviewedByUserId,
            nameof(reviewedByUserId));
        string? normalizedReviewNote = NormalizeReviewNote(reviewNote);
        ValidateLifecycle(
            externalTarget,
            target,
            status,
            confidence,
            validFromUtc,
            validToUtc,
            normalizedReviewer,
            normalizedReviewNote,
            recordedAtUtc);

        this.Id = id;
        this.SourceId = sourceId;
        this.ExternalTarget = externalTarget;
        this.Target = target;
        this.Status = status;
        this.Confidence = confidence;
        this.ValidFromUtc = validFromUtc;
        this.ValidToUtc = validToUtc;
        this.Revision = revision;
        this.SupersedesRevision = supersedesRevision;
        this.ReviewedByUserId = normalizedReviewer;
        this.ReviewNote = normalizedReviewNote;
        this.RecordedAtUtc = recordedAtUtc;
    }

    public Guid Id { get; }

    public LiveDataSourceId SourceId { get; }

    public ExternalLiveTargetDescriptor ExternalTarget { get; }

    public LiveTargetReference? Target { get; }

    public LiveMappingStatus Status { get; }

    public LiveMappingConfidence Confidence { get; }

    public DateTime ValidFromUtc { get; }

    public DateTime? ValidToUtc { get; }

    public int Revision { get; }

    public int? SupersedesRevision { get; }

    public string? ReviewedByUserId { get; }

    public string? ReviewNote { get; }

    public DateTime RecordedAtUtc { get; }

    public string Version => $"{this.Id:N}:{this.Revision}";

    public bool IsEligibleForLiveUse => this.Status == LiveMappingStatus.Verified
        && this.Target is not null
        && !this.ValidToUtc.HasValue;

    public static ExternalLiveTargetMapping CreateCandidate(
        Guid id,
        LiveDataSourceId sourceId,
        ExternalLiveTargetDescriptor externalTarget,
        LiveTargetReference? suggestedTarget,
        LiveMappingConfidence confidence,
        DateTime discoveredAtUtc)
    {
        return new ExternalLiveTargetMapping(
            id,
            sourceId,
            externalTarget,
            suggestedTarget,
            LiveMappingStatus.Candidate,
            confidence,
            discoveredAtUtc,
            null,
            1,
            null,
            null,
            null,
            discoveredAtUtc);
    }

    public ExternalLiveTargetMapping Verify(
        LiveTargetReference target,
        string reviewedByUserId,
        string? reviewNote,
        DateTime reviewedAtUtc)
    {
        if (this.Status is not LiveMappingStatus.Candidate and not LiveMappingStatus.Suspended)
        {
            throw InvalidTransition("Only a candidate or suspended mapping can be verified.");
        }

        return this.NextRevision(
            target,
            LiveMappingStatus.Verified,
            LiveMappingConfidence.High,
            reviewedAtUtc,
            null,
            reviewedByUserId,
            reviewNote,
            reviewedAtUtc);
    }

    public ExternalLiveTargetMapping Correct(
        LiveTargetReference correctedTarget,
        string reviewedByUserId,
        string reviewNote,
        DateTime reviewedAtUtc)
    {
        if (this.Status != LiveMappingStatus.Verified || this.Target is null)
        {
            throw InvalidTransition("Only a verified mapping can be corrected.");
        }

        if (this.Target.Equals(correctedTarget))
        {
            throw InvalidTransition("A mapping correction must change its internal target.");
        }

        if (string.IsNullOrWhiteSpace(reviewNote))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidMapping,
                "A mapping correction requires a review note.",
                nameof(reviewNote));
        }

        return this.NextRevision(
            correctedTarget,
            LiveMappingStatus.Verified,
            LiveMappingConfidence.High,
            reviewedAtUtc,
            null,
            reviewedByUserId,
            reviewNote,
            reviewedAtUtc);
    }

    public ExternalLiveTargetMapping Suspend(
        string reviewedByUserId,
        string reviewNote,
        DateTime reviewedAtUtc)
    {
        if (this.Status != LiveMappingStatus.Verified)
        {
            throw InvalidTransition("Only a verified mapping can be suspended.");
        }

        return this.NextRevision(
            this.Target,
            LiveMappingStatus.Suspended,
            this.Confidence,
            this.ValidFromUtc,
            reviewedAtUtc,
            reviewedByUserId,
            reviewNote,
            reviewedAtUtc);
    }

    public ExternalLiveTargetMapping Reject(
        string reviewedByUserId,
        string reviewNote,
        DateTime reviewedAtUtc)
    {
        if (this.Status != LiveMappingStatus.Candidate)
        {
            throw InvalidTransition("Only a candidate mapping can be rejected.");
        }

        return this.NextRevision(
            this.Target,
            LiveMappingStatus.Rejected,
            this.Confidence,
            this.ValidFromUtc,
            reviewedAtUtc,
            reviewedByUserId,
            reviewNote,
            reviewedAtUtc);
    }

    public ExternalLiveTargetMapping Supersede(
        string reviewedByUserId,
        string reviewNote,
        DateTime reviewedAtUtc)
    {
        if (this.Status is not LiveMappingStatus.Verified and not LiveMappingStatus.Suspended)
        {
            throw InvalidTransition("Only a verified or suspended mapping can be superseded.");
        }

        return this.NextRevision(
            this.Target,
            LiveMappingStatus.Superseded,
            this.Confidence,
            this.ValidFromUtc,
            reviewedAtUtc,
            reviewedByUserId,
            reviewNote,
            reviewedAtUtc);
    }

    private ExternalLiveTargetMapping NextRevision(
        LiveTargetReference? target,
        LiveMappingStatus status,
        LiveMappingConfidence confidence,
        DateTime validFromUtc,
        DateTime? validToUtc,
        string reviewedByUserId,
        string? reviewNote,
        DateTime recordedAtUtc)
    {
        if (recordedAtUtc < this.RecordedAtUtc)
        {
            throw InvalidTransition("A mapping revision cannot predate the previous revision.");
        }

        return new ExternalLiveTargetMapping(
            this.Id,
            this.SourceId,
            this.ExternalTarget,
            target,
            status,
            confidence,
            validFromUtc,
            validToUtc,
            this.Revision + 1,
            this.Revision,
            reviewedByUserId,
            reviewNote,
            recordedAtUtc);
    }

    private static void ValidateLifecycle(
        ExternalLiveTargetDescriptor externalTarget,
        LiveTargetReference? target,
        LiveMappingStatus status,
        LiveMappingConfidence confidence,
        DateTime validFromUtc,
        DateTime? validToUtc,
        string? reviewer,
        string? reviewNote,
        DateTime recordedAtUtc)
    {
        bool timelineIsValid = validFromUtc <= recordedAtUtc
            && (!validToUtc.HasValue
                || validToUtc.Value >= validFromUtc && validToUtc.Value <= recordedAtUtc);
        bool lifecycleIsValid = status switch
        {
            LiveMappingStatus.Candidate => !validToUtc.HasValue
                && reviewer is null
                && confidence is LiveMappingConfidence.Low or LiveMappingConfidence.Medium,
            LiveMappingStatus.Verified => target is not null
                && confidence == LiveMappingConfidence.High
                && !validToUtc.HasValue
                && reviewer is not null,
            LiveMappingStatus.Suspended or LiveMappingStatus.Superseded or LiveMappingStatus.Rejected =>
                validToUtc.HasValue && reviewer is not null && reviewNote is not null,
            _ => false,
        };
        if (!timelineIsValid || !lifecycleIsValid)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidMapping,
                "A live target mapping lifecycle is inconsistent.");
        }

        if (target is null)
        {
            return;
        }

        if (target.Type != externalTarget.Type)
        {
            throw Invalid(
                LiveDataErrorCodes.MappingTargetTypeMismatch,
                "The external and internal live targets must have the same type.");
        }

        if (!string.Equals(
            target.CountryCode,
            externalTarget.CountryCode,
            StringComparison.OrdinalIgnoreCase))
        {
            throw Invalid(
                LiveDataErrorCodes.MappingCountryMismatch,
                "The external and internal live targets must belong to the same country.");
        }
    }

    private static void ValidateRevision(int revision, int? supersedesRevision)
    {
        bool isValid = revision >= 1
            && (revision == 1
                ? !supersedesRevision.HasValue
                : supersedesRevision == revision - 1);
        if (!isValid)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidRevision,
                "A live target mapping revision must form a contiguous chain.");
        }
    }

    private static string? NormalizeOptionalIdentifier(string? value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : IdentifierRules.NormalizeRequired(value, parameterName);
    }

    private static string? NormalizeReviewNote(string? value)
    {
        string? normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalizedValue is not null
            && (normalizedValue.Length > MaximumReviewNoteLength
                || normalizedValue.Any(char.IsControl)))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidText,
                "A live mapping review note is invalid.",
                nameof(value));
        }

        return normalizedValue;
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidTimestamp,
                "Live mapping timestamps must be expressed in UTC.",
                parameterName);
        }
    }

    private static void EnsureOptionalUtc(DateTime? value, string parameterName)
    {
        if (value.HasValue)
        {
            EnsureUtc(value.Value, parameterName);
        }
    }

    private static void ValidateEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidEnum,
                "A live target mapping contains an invalid state.",
                parameterName);
        }
    }

    private static LiveDataValidationException InvalidTransition(string message)
    {
        return Invalid(LiveDataErrorCodes.InvalidMappingTransition, message);
    }

    private static LiveDataValidationException Invalid(
        string code,
        string message,
        string? parameterName = null)
    {
        return new LiveDataValidationException(code, message, parameterName);
    }
}
