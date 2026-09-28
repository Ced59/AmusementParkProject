namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveDataSource
{
    public static readonly TimeSpan MaximumPollingInterval = TimeSpan.FromDays(1);
    public static readonly TimeSpan MaximumDefaultTtl = TimeSpan.FromDays(1);

    public LiveDataSource(
        LiveDataSourceId id,
        LiveDataSourceType type,
        string displayName,
        SourceUsagePolicy usagePolicy,
        TimeSpan minimumPollingInterval,
        TimeSpan defaultTtl,
        LiveDataSourceStatus status)
    {
        _ = id.Value;
        ValidateEnum(type, nameof(type));
        ValidateEnum(status, nameof(status));
        ArgumentNullException.ThrowIfNull(usagePolicy);
        string normalizedDisplayName = NormalizeDisplayName(displayName, nameof(displayName));
        ValidateDurations(minimumPollingInterval, defaultTtl);

        this.Id = id;
        this.Type = type;
        this.DisplayName = normalizedDisplayName;
        this.UsagePolicy = usagePolicy;
        this.MinimumPollingInterval = minimumPollingInterval;
        this.DefaultTtl = defaultTtl;
        this.Status = status;
    }

    public LiveDataSourceId Id { get; }

    public LiveDataSourceType Type { get; }

    public string DisplayName { get; }

    public SourceUsagePolicy UsagePolicy { get; }

    public TimeSpan MinimumPollingInterval { get; }

    public TimeSpan DefaultTtl { get; }

    public LiveDataSourceStatus Status { get; }

    public bool CanPoll => this.Status == LiveDataSourceStatus.Active;

    private static string NormalizeDisplayName(string? value, string parameterName)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length is 0 or > 200 || normalizedValue.Any(char.IsControl))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidText,
                "A live data source display name is required and cannot exceed 200 characters.",
                parameterName);
        }

        return normalizedValue;
    }

    private static void ValidateDurations(
        TimeSpan minimumPollingInterval,
        TimeSpan defaultTtl)
    {
        if (minimumPollingInterval <= TimeSpan.Zero
            || minimumPollingInterval > MaximumPollingInterval)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidDuration,
                "The minimum polling interval must be positive and cannot exceed one day.",
                nameof(minimumPollingInterval));
        }

        if (defaultTtl < minimumPollingInterval || defaultTtl > MaximumDefaultTtl)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidDuration,
                "The default TTL must cover at least one polling interval and cannot exceed one day.",
                nameof(defaultTtl));
        }
    }

    private static void ValidateEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidEnum,
                "A live data source contains an invalid state.",
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
