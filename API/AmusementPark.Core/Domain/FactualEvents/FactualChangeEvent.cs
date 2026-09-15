namespace AmusementPark.Core.Domain.FactualEvents;

/// <summary>
/// Versioned, sourced factual change. Only Published events are distributable.
/// </summary>
public sealed class FactualChangeEvent
{
    public const int MaximumDeduplicationKeyLength = 300;
    public const int MaximumReasonCodeLength = 100;

    private FactualChangeEvent(
        FactualChangeEventId id,
        FactualEventType type,
        int definitionVersion,
        ChangeTarget target,
        FactValue? previousValue,
        FactValue? newValue,
        SourceReference source,
        DataConfidence confidence,
        DateTime occurredAtUtc,
        string deduplicationKey,
        long revision,
        FactualChangeStatus status,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime? verifiedAtUtc,
        DateTime? publishedAtUtc,
        DateTime? terminalAtUtc,
        FactualChangeEventId? supersededByEventId,
        string? reasonCode,
        long version)
    {
        _ = id.Value;
        FactualEventDefinition definition = FactualEventCatalog.Get(type, definitionVersion);
        ArgumentNullException.ThrowIfNull(target);
        if (!definition.Supports(target.Type))
        {
            throw Invalid(
                FactualEventErrorCodes.IncompatibleTarget,
                "The factual event type does not support the selected target type.");
        }

        ValidateFactChange(previousValue, newValue);
        ArgumentNullException.ThrowIfNull(source);
        ValidateConfidence(confidence);
        string normalizedDeduplicationKey = NormalizeKey(
            deduplicationKey,
            MaximumDeduplicationKeyLength,
            FactualEventErrorCodes.InvalidDeduplicationKey,
            "deduplication key");
        if (revision < 1)
        {
            throw Invalid(
                FactualEventErrorCodes.InvalidRevision,
                "The factual event revision must be positive.");
        }

        string? normalizedReasonCode = NormalizeOptionalReason(reasonCode);
        ValidateState(
            status,
            source,
            occurredAtUtc,
            createdAtUtc,
            updatedAtUtc,
            verifiedAtUtc,
            publishedAtUtc,
            terminalAtUtc,
            supersededByEventId,
            normalizedReasonCode);
        if (supersededByEventId == id)
        {
            throw Invalid(
                FactualEventErrorCodes.MissingSupersedingEvent,
                "A factual event cannot supersede itself.");
        }

        if (verifiedAtUtc is not null && confidence == DataConfidence.Low)
        {
            throw Invalid(
                FactualEventErrorCodes.InsufficientConfidence,
                "Low-confidence evidence cannot belong to a verified factual event.");
        }

        if (version < 1)
        {
            throw Invalid(
                FactualEventErrorCodes.InvalidVersion,
                "The factual event version must be positive.");
        }

        this.Id = id;
        this.Type = type;
        this.DefinitionVersion = definitionVersion;
        this.Target = target;
        this.PreviousValue = previousValue;
        this.NewValue = newValue;
        this.Source = source;
        this.Confidence = confidence;
        this.OccurredAtUtc = occurredAtUtc;
        this.DeduplicationKey = normalizedDeduplicationKey;
        this.Revision = revision;
        this.Status = status;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
        this.VerifiedAtUtc = verifiedAtUtc;
        this.PublishedAtUtc = publishedAtUtc;
        this.TerminalAtUtc = terminalAtUtc;
        this.SupersededByEventId = supersededByEventId;
        this.ReasonCode = normalizedReasonCode;
        this.Version = version;
    }

    public FactualChangeEventId Id { get; }

    public FactualEventType Type { get; }

    public int DefinitionVersion { get; }

    public ChangeTarget Target { get; }

    public FactValue? PreviousValue { get; }

    public FactValue? NewValue { get; }

    public SourceReference Source { get; private set; }

    public DataConfidence Confidence { get; private set; }

    public DateTime OccurredAtUtc { get; }

    public string DeduplicationKey { get; }

    public long Revision { get; }

    public FactualChangeStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? VerifiedAtUtc { get; private set; }

    public DateTime? PublishedAtUtc { get; private set; }

    public DateTime? TerminalAtUtc { get; private set; }

    public FactualChangeEventId? SupersededByEventId { get; private set; }

    public string? ReasonCode { get; private set; }

    public long Version { get; private set; }

    public bool CanBeDistributed => this.Status == FactualChangeStatus.Published;

    public static FactualChangeEvent CreateDraft(
        FactualChangeEventId id,
        FactualEventType type,
        ChangeTarget target,
        FactValue? previousValue,
        FactValue? newValue,
        SourceReference source,
        DataConfidence confidence,
        DateTime occurredAtUtc,
        string deduplicationKey,
        long revision,
        DateTime nowUtc)
    {
        FactualEventDefinition definition = FactualEventCatalog.Get(type);
        return new FactualChangeEvent(
            id,
            type,
            definition.SchemaVersion,
            target,
            previousValue,
            newValue,
            source,
            confidence,
            occurredAtUtc,
            deduplicationKey,
            revision,
            FactualChangeStatus.Draft,
            nowUtc,
            nowUtc,
            null,
            null,
            null,
            null,
            null,
            1);
    }

    public static FactualChangeEvent Restore(
        FactualChangeEventId id,
        FactualEventType type,
        int definitionVersion,
        ChangeTarget target,
        FactValue? previousValue,
        FactValue? newValue,
        SourceReference source,
        DataConfidence confidence,
        DateTime occurredAtUtc,
        string deduplicationKey,
        long revision,
        FactualChangeStatus status,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime? verifiedAtUtc,
        DateTime? publishedAtUtc,
        DateTime? terminalAtUtc,
        FactualChangeEventId? supersededByEventId,
        string? reasonCode,
        long version)
    {
        return new FactualChangeEvent(
            id,
            type,
            definitionVersion,
            target,
            previousValue,
            newValue,
            source,
            confidence,
            occurredAtUtc,
            deduplicationKey,
            revision,
            status,
            createdAtUtc,
            updatedAtUtc,
            verifiedAtUtc,
            publishedAtUtc,
            terminalAtUtc,
            supersededByEventId,
            reasonCode,
            version);
    }

    public void ReplaceEvidence(
        SourceReference source,
        DataConfidence confidence,
        DateTime nowUtc)
    {
        if (this.Status != FactualChangeStatus.Draft)
        {
            throw InvalidTransition("Evidence can only be replaced while the event is a draft.");
        }

        ArgumentNullException.ThrowIfNull(source);
        ValidateConfidence(confidence);
        this.ValidateMutationTimestamp(nowUtc);
        if (source.PublishedAtUtc > nowUtc)
        {
            throw Invalid(
                FactualEventErrorCodes.InvalidTimestamp,
                "Factual evidence cannot be published after its update timestamp.");
        }

        if (this.Source == source && this.Confidence == confidence)
        {
            return;
        }

        this.PrepareMutation();
        this.Source = source;
        this.Confidence = confidence;
        this.CommitMutation(nowUtc);
    }

    public void Verify(DateTime verifiedAtUtc)
    {
        if (this.Status == FactualChangeStatus.Verified)
        {
            this.ValidateMutationTimestamp(verifiedAtUtc);
            return;
        }

        if (this.Status != FactualChangeStatus.Draft)
        {
            throw InvalidTransition("Only a draft factual event can be verified.");
        }

        if (this.Confidence == DataConfidence.Low)
        {
            throw Invalid(
                FactualEventErrorCodes.InsufficientConfidence,
                "Low-confidence evidence cannot verify a factual event.");
        }

        this.ValidateMutationTimestamp(verifiedAtUtc);
        this.PrepareMutation();
        this.Status = FactualChangeStatus.Verified;
        this.VerifiedAtUtc = verifiedAtUtc;
        this.CommitMutation(verifiedAtUtc);
    }

    public void Publish(DateTime publishedAtUtc)
    {
        if (this.Status == FactualChangeStatus.Published)
        {
            this.ValidateMutationTimestamp(publishedAtUtc);
            return;
        }

        if (this.Status != FactualChangeStatus.Verified)
        {
            throw InvalidTransition("Only a verified factual event can be published.");
        }

        this.ValidateMutationTimestamp(publishedAtUtc);
        this.PrepareMutation();
        this.Status = FactualChangeStatus.Published;
        this.PublishedAtUtc = publishedAtUtc;
        this.CommitMutation(publishedAtUtc);
    }

    public void Correct(FactualChangeEventId supersedingEventId, DateTime correctedAtUtc)
    {
        _ = supersedingEventId.Value;
        if (supersedingEventId == this.Id)
        {
            throw Invalid(
                FactualEventErrorCodes.MissingSupersedingEvent,
                "A factual event cannot supersede itself.");
        }

        if (this.Status == FactualChangeStatus.Corrected
            && this.SupersededByEventId == supersedingEventId)
        {
            this.ValidateMutationTimestamp(correctedAtUtc);
            return;
        }

        this.EnsurePublishedTransition("corrected");
        this.ValidateMutationTimestamp(correctedAtUtc);
        this.PrepareMutation();
        this.Status = FactualChangeStatus.Corrected;
        this.SupersededByEventId = supersedingEventId;
        this.TerminalAtUtc = correctedAtUtc;
        this.CommitMutation(correctedAtUtc);
    }

    public void Retract(string reasonCode, DateTime retractedAtUtc)
    {
        string normalizedReasonCode = NormalizeRequiredReason(reasonCode);
        if (this.Status == FactualChangeStatus.Retracted
            && string.Equals(this.ReasonCode, normalizedReasonCode, StringComparison.Ordinal))
        {
            this.ValidateMutationTimestamp(retractedAtUtc);
            return;
        }

        this.EnsurePublishedTransition("retracted");
        this.ValidateMutationTimestamp(retractedAtUtc);
        this.PrepareMutation();
        this.Status = FactualChangeStatus.Retracted;
        this.ReasonCode = normalizedReasonCode;
        this.TerminalAtUtc = retractedAtUtc;
        this.CommitMutation(retractedAtUtc);
    }

    public void Expire(DateTime expiredAtUtc)
    {
        if (this.Status == FactualChangeStatus.Expired)
        {
            this.ValidateMutationTimestamp(expiredAtUtc);
            return;
        }

        if (this.Status is FactualChangeStatus.Corrected or FactualChangeStatus.Retracted)
        {
            throw InvalidTransition("A terminal factual event cannot expire again.");
        }

        this.ValidateMutationTimestamp(expiredAtUtc);
        this.PrepareMutation();
        this.Status = FactualChangeStatus.Expired;
        this.TerminalAtUtc = expiredAtUtc;
        this.CommitMutation(expiredAtUtc);
    }

    public bool HasSameLogicalRevisionAs(FactualChangeEvent other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return string.Equals(
                this.DeduplicationKey,
                other.DeduplicationKey,
                StringComparison.Ordinal)
            && this.Revision == other.Revision;
    }

    private static void ValidateFactChange(FactValue? previousValue, FactValue? newValue)
    {
        if (previousValue is null && newValue is null)
        {
            throw Invalid(
                FactualEventErrorCodes.InvalidFactValue,
                "A factual event must contain a previous or a new structured value.");
        }

        if (previousValue is not null && previousValue == newValue)
        {
            throw Invalid(
                FactualEventErrorCodes.UnchangedFact,
                "A factual event cannot represent an unchanged value.");
        }
    }

    private static void ValidateConfidence(DataConfidence confidence)
    {
        if (!Enum.IsDefined(confidence))
        {
            throw Invalid(
                FactualEventErrorCodes.InvalidConfidence,
                "The factual event confidence is invalid.");
        }
    }

    private static void ValidateState(
        FactualChangeStatus status,
        SourceReference source,
        DateTime occurredAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime? verifiedAtUtc,
        DateTime? publishedAtUtc,
        DateTime? terminalAtUtc,
        FactualChangeEventId? supersededByEventId,
        string? reasonCode)
    {
        if (!Enum.IsDefined(status))
        {
            throw Invalid(FactualEventErrorCodes.InvalidState, "The factual event status is invalid.");
        }

        EnsureUtc(occurredAtUtc);
        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);
        EnsureOptionalUtc(verifiedAtUtc);
        EnsureOptionalUtc(publishedAtUtc);
        EnsureOptionalUtc(terminalAtUtc);
        DateTime evidenceDeadlineUtc = verifiedAtUtc ?? updatedAtUtc;
        if (source.PublishedAtUtc > evidenceDeadlineUtc
            || occurredAtUtc > createdAtUtc
            || updatedAtUtc < createdAtUtc
            || verifiedAtUtc < createdAtUtc
            || verifiedAtUtc > updatedAtUtc
            || publishedAtUtc < verifiedAtUtc
            || publishedAtUtc > updatedAtUtc
            || terminalAtUtc > updatedAtUtc
            || terminalAtUtc < (publishedAtUtc ?? verifiedAtUtc ?? createdAtUtc))
        {
            throw Invalid(
                FactualEventErrorCodes.InvalidTimestamp,
                "The factual event timestamps are not chronologically consistent.");
        }

        bool validState = status switch
        {
            FactualChangeStatus.Draft => verifiedAtUtc is null
                && publishedAtUtc is null
                && terminalAtUtc is null
                && supersededByEventId is null
                && reasonCode is null,
            FactualChangeStatus.Verified => verifiedAtUtc is not null
                && publishedAtUtc is null
                && terminalAtUtc is null
                && supersededByEventId is null
                && reasonCode is null,
            FactualChangeStatus.Published => verifiedAtUtc is not null
                && publishedAtUtc is not null
                && terminalAtUtc is null
                && supersededByEventId is null
                && reasonCode is null,
            FactualChangeStatus.Corrected => verifiedAtUtc is not null
                && publishedAtUtc is not null
                && terminalAtUtc is not null
                && supersededByEventId is not null
                && reasonCode is null,
            FactualChangeStatus.Retracted => verifiedAtUtc is not null
                && publishedAtUtc is not null
                && terminalAtUtc is not null
                && supersededByEventId is null
                && reasonCode is not null,
            FactualChangeStatus.Expired => terminalAtUtc is not null
                && (publishedAtUtc is null || verifiedAtUtc is not null)
                && supersededByEventId is null
                && reasonCode is null,
            _ => false,
        };
        if (!validState)
        {
            throw Invalid(
                FactualEventErrorCodes.InvalidState,
                "The factual event status and lifecycle metadata are inconsistent.");
        }

        if (supersededByEventId.HasValue)
        {
            _ = supersededByEventId.Value.Value;
        }
    }

    private static string NormalizeKey(
        string value,
        int maximumLength,
        string errorCode,
        string label)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0
            || normalizedValue.Length > maximumLength
            || (value is not null && value.Any(char.IsControl))
            || normalizedValue.Any(char.IsWhiteSpace))
        {
            throw Invalid(errorCode, $"The factual event {label} is invalid.");
        }

        return normalizedValue;
    }

    private static string NormalizeRequiredReason(string reasonCode)
    {
        return NormalizeKey(
            reasonCode,
            MaximumReasonCodeLength,
            FactualEventErrorCodes.InvalidReasonCode,
            "reason code");
    }

    private static string? NormalizeOptionalReason(string? reasonCode)
    {
        return string.IsNullOrWhiteSpace(reasonCode)
            ? null
            : NormalizeRequiredReason(reasonCode);
    }

    private static void EnsureUtc(DateTime timestamp)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw Invalid(
                FactualEventErrorCodes.InvalidTimestamp,
                "Factual event timestamps must be expressed in UTC.");
        }
    }

    private static void EnsureOptionalUtc(DateTime? timestamp)
    {
        if (timestamp.HasValue)
        {
            EnsureUtc(timestamp.Value);
        }
    }

    private void EnsurePublishedTransition(string targetStatus)
    {
        if (this.Status != FactualChangeStatus.Published)
        {
            throw InvalidTransition($"Only a published factual event can be {targetStatus}.");
        }
    }

    private void ValidateMutationTimestamp(DateTime nowUtc)
    {
        EnsureUtc(nowUtc);
        if (nowUtc < this.UpdatedAtUtc)
        {
            throw Invalid(
                FactualEventErrorCodes.InvalidTimestamp,
                "A factual event mutation cannot predate its current state.");
        }
    }

    private void PrepareMutation()
    {
        if (this.Version == long.MaxValue)
        {
            throw Invalid(
                FactualEventErrorCodes.InvalidVersion,
                "The factual event version cannot be incremented further.");
        }
    }

    private void CommitMutation(DateTime nowUtc)
    {
        this.Version++;
        this.UpdatedAtUtc = nowUtc;
    }

    private static FactualEventValidationException InvalidTransition(string message)
    {
        return Invalid(FactualEventErrorCodes.InvalidTransition, message);
    }

    private static FactualEventValidationException Invalid(string code, string message)
    {
        return new FactualEventValidationException(code, message);
    }
}
