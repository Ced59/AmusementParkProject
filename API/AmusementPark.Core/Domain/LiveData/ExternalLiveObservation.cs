using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.LiveData;

public sealed class ExternalLiveObservation
{
    private const int MaximumDisplayNameLength = 300;

    public ExternalLiveObservation(
        string externalTargetId,
        string displayName,
        LiveTargetType targetType,
        LiveOperationalStatus status,
        DateTime sourceUpdatedAtUtc,
        IReadOnlyCollection<LiveQueueObservation>? queues = null)
    {
        ValidateEnum(targetType, nameof(targetType));
        ValidateEnum(status, nameof(status));
        if (sourceUpdatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidTimestamp,
                "A live source timestamp must be expressed in UTC.",
                nameof(sourceUpdatedAtUtc));
        }

        List<LiveQueueObservation> normalizedQueues = queues?.ToList()
            ?? new List<LiveQueueObservation>();
        if (normalizedQueues.Any(static queue => queue is null)
            || normalizedQueues.GroupBy(static queue => queue.Kind).Any(static group => group.Count() > 1))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidQueue,
                "A live observation cannot contain duplicate or empty queues.",
                nameof(queues));
        }

        this.ExternalTargetId = IdentifierRules.NormalizeRequired(
            externalTargetId,
            nameof(externalTargetId));
        this.DisplayName = NormalizeDisplayName(displayName);
        this.TargetType = targetType;
        this.Status = status;
        this.SourceUpdatedAtUtc = sourceUpdatedAtUtc;
        this.Queues = normalizedQueues.AsReadOnly();
    }

    public string ExternalTargetId { get; }

    public string DisplayName { get; }

    public LiveTargetType TargetType { get; }

    public LiveOperationalStatus Status { get; }

    public DateTime SourceUpdatedAtUtc { get; }

    public IReadOnlyCollection<LiveQueueObservation> Queues { get; }

    public bool HasStatusQueueConflict =>
        this.Status == LiveOperationalStatus.Closed
        && this.Queues.Any(static queue => queue.WaitTimeMinutes.HasValue);

    private static string NormalizeDisplayName(string? value)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length is 0 or > MaximumDisplayNameLength
            || normalizedValue.Any(char.IsControl))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidText,
                "A live observation display name is required and cannot exceed 300 characters.",
                nameof(value));
        }

        return normalizedValue;
    }

    private static void ValidateEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidEnum,
                "A live observation contains an invalid enum value.",
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
