namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveQueueObservation
{
    public const int MaximumWaitTimeMinutes = 1440;

    public LiveQueueObservation(
        LiveQueueKind kind,
        int? waitTimeMinutes,
        bool isEstimated,
        LiveQueueAvailability availability = LiveQueueAvailability.Unspecified,
        DateTime? returnStartUtc = null,
        DateTime? returnEndUtc = null,
        int? currentGroupStart = null,
        int? currentGroupEnd = null,
        DateTime? nextAllocationUtc = null,
        long? priceMinorUnits = null,
        string? currencyCode = null)
    {
        ValidateEnum(kind, nameof(kind));
        ValidateEnum(availability, nameof(availability));
        ValidateWaitTime(waitTimeMinutes);
        EnsureOptionalUtc(returnStartUtc, nameof(returnStartUtc));
        EnsureOptionalUtc(returnEndUtc, nameof(returnEndUtc));
        EnsureOptionalUtc(nextAllocationUtc, nameof(nextAllocationUtc));
        if (returnStartUtc.HasValue
            && returnEndUtc.HasValue
            && returnEndUtc.Value < returnStartUtc.Value)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidQueue,
                "A live return window cannot end before it starts.",
                nameof(returnEndUtc));
        }

        ValidateGroupRange(currentGroupStart, currentGroupEnd);
        if (priceMinorUnits < 0)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidQueue,
                "A live queue price cannot be negative.",
                nameof(priceMinorUnits));
        }

        string? normalizedCurrencyCode = NormalizeCurrencyCode(currencyCode);
        if (priceMinorUnits.HasValue && normalizedCurrencyCode is null)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidQueue,
                "A priced live queue requires a currency code.",
                nameof(currencyCode));
        }

        this.Kind = kind;
        this.WaitTimeMinutes = waitTimeMinutes;
        this.IsEstimated = isEstimated;
        this.Availability = availability;
        this.ReturnStartUtc = returnStartUtc;
        this.ReturnEndUtc = returnEndUtc;
        this.CurrentGroupStart = currentGroupStart;
        this.CurrentGroupEnd = currentGroupEnd;
        this.NextAllocationUtc = nextAllocationUtc;
        this.PriceMinorUnits = priceMinorUnits;
        this.CurrencyCode = normalizedCurrencyCode;
    }

    public LiveQueueKind Kind { get; }

    public int? WaitTimeMinutes { get; }

    public bool IsEstimated { get; }

    public LiveQueueAvailability Availability { get; }

    public DateTime? ReturnStartUtc { get; }

    public DateTime? ReturnEndUtc { get; }

    public int? CurrentGroupStart { get; }

    public int? CurrentGroupEnd { get; }

    public DateTime? NextAllocationUtc { get; }

    public long? PriceMinorUnits { get; }

    public string? CurrencyCode { get; }

    private static void ValidateWaitTime(int? waitTimeMinutes)
    {
        if (waitTimeMinutes is < 0 or > MaximumWaitTimeMinutes)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidWaitTime,
                $"A live wait time must be between 0 and {MaximumWaitTimeMinutes} minutes.",
                nameof(waitTimeMinutes));
        }
    }

    private static void ValidateGroupRange(int? currentGroupStart, int? currentGroupEnd)
    {
        if (currentGroupStart < 0
            || currentGroupEnd < 0
            || (currentGroupStart.HasValue
                && currentGroupEnd.HasValue
                && currentGroupEnd.Value < currentGroupStart.Value))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidQueue,
                "A live boarding group range is invalid.");
        }
    }

    private static string? NormalizeCurrencyCode(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return null;
        }

        string normalizedCurrencyCode = currencyCode.Trim().ToUpperInvariant();
        if (normalizedCurrencyCode.Length != 3
            || normalizedCurrencyCode.Any(static character => character is < 'A' or > 'Z'))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidQueue,
                "A live queue currency must use an ISO alpha-3 code.",
                nameof(currencyCode));
        }

        return normalizedCurrencyCode;
    }

    private static void EnsureOptionalUtc(DateTime? value, string parameterName)
    {
        if (value.HasValue && value.Value.Kind != DateTimeKind.Utc)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidTimestamp,
                "Live queue timestamps must be expressed in UTC.",
                parameterName);
        }
    }

    private static void ValidateEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidEnum,
                "A live queue contains an invalid enum value.",
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
