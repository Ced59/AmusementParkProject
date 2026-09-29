using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveQualityIncident
{
    private const int MaximumDiagnosticLength = 100;

    public LiveQualityIncident(
        Guid id,
        LiveDataSourceId sourceId,
        ExternalLiveObservation? observation,
        LiveQualityIncidentReason reason,
        string? diagnosticCode,
        string? diagnosticExternalTargetId,
        string? diagnosticField,
        DateTime receivedAtUtc,
        DateTime detectedAtUtc,
        DateTime expiresAtUtc,
        string correlationId,
        string adapterVersion,
        string usagePolicyVersion,
        string transformationVersion,
        LiveDataConfidence confidence,
        LiveFreshnessPolicy freshnessPolicy,
        string? payloadSha256,
        LiveQualityIncidentStatus status = LiveQualityIncidentStatus.Pending,
        DateTime? resolvedAtUtc = null,
        string? resolvedByUserId = null,
        int replayAttemptCount = 0,
        DateTime? lastReplayAttemptAtUtc = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A live quality incident requires an identifier.", nameof(id));
        }

        _ = sourceId.Value;
        ArgumentNullException.ThrowIfNull(freshnessPolicy);
        EnsureUtc(receivedAtUtc, nameof(receivedAtUtc));
        EnsureUtc(detectedAtUtc, nameof(detectedAtUtc));
        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));
        if (detectedAtUtc < receivedAtUtc || expiresAtUtc <= detectedAtUtc)
        {
            throw new ArgumentException("A live quality incident timeline is inconsistent.");
        }

        if (!Enum.IsDefined(reason) || !Enum.IsDefined(status) || !Enum.IsDefined(confidence))
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        string? normalizedDiagnosticCode = NormalizeOptional(
            diagnosticCode,
            MaximumDiagnosticLength,
            nameof(diagnosticCode));
        string? normalizedDiagnosticField = NormalizeOptional(
            diagnosticField,
            MaximumDiagnosticLength,
            nameof(diagnosticField));
        string? normalizedDiagnosticExternalTargetId = NormalizeOptional(
            diagnosticExternalTargetId,
            IdentifierRules.MaximumLength,
            nameof(diagnosticExternalTargetId));
        if (reason == LiveQualityIncidentReason.ProviderDiagnostic
            && normalizedDiagnosticCode is null)
        {
            throw new ArgumentException(
                "A provider diagnostic incident requires a diagnostic code.",
                nameof(diagnosticCode));
        }

        if (observation is null && reason != LiveQualityIncidentReason.ProviderDiagnostic)
        {
            throw new ArgumentException(
                "A replayable live quality incident requires its normalized observation.",
                nameof(observation));
        }

        string? normalizedResolvedBy = NormalizeOptional(
            resolvedByUserId,
            IdentifierRules.MaximumLength,
            nameof(resolvedByUserId));
        if (status == LiveQualityIncidentStatus.Resolved)
        {
            if (!resolvedAtUtc.HasValue || normalizedResolvedBy is null)
            {
                throw new ArgumentException("A resolved live quality incident requires its audit metadata.");
            }

            EnsureUtc(resolvedAtUtc.Value, nameof(resolvedAtUtc));
            if (resolvedAtUtc.Value < detectedAtUtc)
            {
                throw new ArgumentException("A live quality incident cannot be resolved before detection.");
            }
        }
        else if (resolvedAtUtc.HasValue || normalizedResolvedBy is not null)
        {
            throw new ArgumentException("A pending live quality incident cannot contain resolution metadata.");
        }

        bool hasReplayAttempts = replayAttemptCount > 0;
        if (replayAttemptCount < 0
            || hasReplayAttempts != lastReplayAttemptAtUtc.HasValue)
        {
            throw new ArgumentException("Live quality replay attempt metadata is inconsistent.");
        }

        if (lastReplayAttemptAtUtc.HasValue)
        {
            EnsureUtc(lastReplayAttemptAtUtc.Value, nameof(lastReplayAttemptAtUtc));
            if (lastReplayAttemptAtUtc.Value < detectedAtUtc)
            {
                throw new ArgumentException("A live quality incident cannot be replayed before detection.");
            }
        }

        this.Id = id;
        this.SourceId = sourceId;
        this.Observation = observation;
        this.Reason = reason;
        this.DiagnosticCode = normalizedDiagnosticCode;
        this.DiagnosticExternalTargetId = normalizedDiagnosticExternalTargetId;
        this.DiagnosticField = normalizedDiagnosticField;
        this.ReceivedAtUtc = receivedAtUtc;
        this.DetectedAtUtc = detectedAtUtc;
        this.ExpiresAtUtc = expiresAtUtc;
        this.CorrelationId = NormalizeRequired(correlationId, nameof(correlationId));
        this.AdapterVersion = NormalizeRequired(adapterVersion, nameof(adapterVersion));
        this.UsagePolicyVersion = NormalizeRequired(usagePolicyVersion, nameof(usagePolicyVersion));
        this.TransformationVersion = NormalizeRequired(
            transformationVersion,
            nameof(transformationVersion));
        this.Confidence = confidence;
        this.FreshnessPolicy = freshnessPolicy;
        this.PayloadSha256 = NormalizeHash(payloadSha256);
        this.Status = status;
        this.ResolvedAtUtc = resolvedAtUtc;
        this.ResolvedByUserId = normalizedResolvedBy;
        this.ReplayAttemptCount = replayAttemptCount;
        this.LastReplayAttemptAtUtc = lastReplayAttemptAtUtc;
    }

    public Guid Id { get; }

    public LiveDataSourceId SourceId { get; }

    public ExternalLiveObservation? Observation { get; }

    public LiveQualityIncidentReason Reason { get; }

    public string? DiagnosticCode { get; }

    public string? DiagnosticExternalTargetId { get; }

    public string? DiagnosticField { get; }

    public DateTime ReceivedAtUtc { get; }

    public DateTime DetectedAtUtc { get; }

    public DateTime ExpiresAtUtc { get; }

    public string CorrelationId { get; }

    public string AdapterVersion { get; }

    public string UsagePolicyVersion { get; }

    public string TransformationVersion { get; }

    public LiveDataConfidence Confidence { get; }

    public LiveFreshnessPolicy FreshnessPolicy { get; }

    public string? PayloadSha256 { get; }

    public LiveQualityIncidentStatus Status { get; }

    public DateTime? ResolvedAtUtc { get; }

    public string? ResolvedByUserId { get; }

    public int ReplayAttemptCount { get; }

    public DateTime? LastReplayAttemptAtUtc { get; }

    public bool IsReplayable => this.Status == LiveQualityIncidentStatus.Pending
        && this.Observation is not null
        && this.Reason is LiveQualityIncidentReason.UnmappedTarget
            or LiveQualityIncidentReason.IneligibleMapping
            or LiveQualityIncidentReason.InvalidFreshness;

    public LiveQualityIncident Resolve(DateTime resolvedAtUtc, string resolvedByUserId)
    {
        if (this.Status != LiveQualityIncidentStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending live quality incident can be resolved.");
        }

        return new LiveQualityIncident(
            this.Id,
            this.SourceId,
            this.Observation,
            this.Reason,
            this.DiagnosticCode,
            this.DiagnosticExternalTargetId,
            this.DiagnosticField,
            this.ReceivedAtUtc,
            this.DetectedAtUtc,
            this.ExpiresAtUtc,
            this.CorrelationId,
            this.AdapterVersion,
            this.UsagePolicyVersion,
            this.TransformationVersion,
            this.Confidence,
            this.FreshnessPolicy,
            this.PayloadSha256,
            LiveQualityIncidentStatus.Resolved,
            resolvedAtUtc,
            resolvedByUserId,
            this.ReplayAttemptCount,
            this.LastReplayAttemptAtUtc);
    }

    private static string NormalizeRequired(string? value, string parameterName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is 0 or > 100 || normalized.Any(char.IsControl))
        {
            throw new ArgumentException("The value is required and cannot exceed 100 characters.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim();
        if (normalized.Length > maximumLength || normalized.Any(char.IsControl))
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeHash(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(static character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("The payload hash must be a SHA-256 hexadecimal value.", nameof(value));
        }

        return normalized;
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Live quality incident timestamps must be UTC.", parameterName);
        }
    }
}
