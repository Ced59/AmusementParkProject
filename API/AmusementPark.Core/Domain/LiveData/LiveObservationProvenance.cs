using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveObservationProvenance
{
    public LiveObservationProvenance(
        LiveDataSourceId sourceId,
        string externalTargetId,
        DateTime observedAtUtc,
        DateTime receivedAtUtc,
        DateTime normalizedAtUtc,
        string correlationId,
        string adapterVersion,
        string mappingVersion,
        LiveDataConfidence confidence,
        string usagePolicyVersion,
        string transformationVersion)
    {
        _ = sourceId.Value;
        ValidateConfidence(confidence);
        EnsureUtc(observedAtUtc, nameof(observedAtUtc));
        EnsureUtc(receivedAtUtc, nameof(receivedAtUtc));
        EnsureUtc(normalizedAtUtc, nameof(normalizedAtUtc));
        if (normalizedAtUtc < receivedAtUtc)
        {
            throw Invalid(
                LiveDataErrorCodes.InconsistentTimeline,
                "A live observation cannot be normalized before it is received.",
                nameof(normalizedAtUtc));
        }

        this.SourceId = sourceId;
        this.ExternalTargetId = IdentifierRules.NormalizeRequired(
            externalTargetId,
            nameof(externalTargetId));
        this.ObservedAtUtc = observedAtUtc;
        this.ReceivedAtUtc = receivedAtUtc;
        this.NormalizedAtUtc = normalizedAtUtc;
        this.CorrelationId = IdentifierRules.NormalizeRequired(
            correlationId,
            nameof(correlationId));
        this.AdapterVersion = NormalizeVersion(adapterVersion, nameof(adapterVersion));
        this.MappingVersion = NormalizeVersion(mappingVersion, nameof(mappingVersion));
        this.Confidence = confidence;
        this.UsagePolicyVersion = NormalizeVersion(
            usagePolicyVersion,
            nameof(usagePolicyVersion));
        this.TransformationVersion = NormalizeVersion(
            transformationVersion,
            nameof(transformationVersion));
    }

    public LiveDataSourceId SourceId { get; }

    public string ExternalTargetId { get; }

    public DateTime ObservedAtUtc { get; }

    public DateTime ReceivedAtUtc { get; }

    public DateTime NormalizedAtUtc { get; }

    public string CorrelationId { get; }

    public string AdapterVersion { get; }

    public string MappingVersion { get; }

    public LiveDataConfidence Confidence { get; }

    public string UsagePolicyVersion { get; }

    public string TransformationVersion { get; }

    private static string NormalizeVersion(string? value, string parameterName)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length is 0 or > 100 || normalizedValue.Any(char.IsControl))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidText,
                "A live observation version is required and cannot exceed 100 characters.",
                parameterName);
        }

        return normalizedValue;
    }

    private static void ValidateConfidence(LiveDataConfidence confidence)
    {
        if (!Enum.IsDefined(confidence))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidEnum,
                "A live observation contains an invalid confidence level.",
                nameof(confidence));
        }
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidTimestamp,
                "Live observation timestamps must be expressed in UTC.",
                parameterName);
        }
    }

    private static LiveDataValidationException Invalid(
        string code,
        string message,
        string? parameterName = null)
    {
        return new LiveDataValidationException(code, message, parameterName);
    }
}
