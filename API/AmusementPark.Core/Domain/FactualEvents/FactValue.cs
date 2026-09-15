using System.Globalization;

namespace AmusementPark.Core.Domain.FactualEvents;

public sealed record FactValue
{
    public const int MaximumTextLength = 4000;
    public const int MaximumCodeLength = 200;
    public const int MaximumUnitCodeLength = 16;

    private FactValue(FactValueKind kind, string canonicalValue, string? unitCode)
    {
        this.Kind = kind;
        this.CanonicalValue = canonicalValue;
        this.UnitCode = unitCode;
    }

    public FactValueKind Kind { get; }

    public string CanonicalValue { get; }

    public string? UnitCode { get; }

    public static FactValue FromText(string value)
    {
        return new FactValue(
            FactValueKind.Text,
            NormalizeBounded(value, MaximumTextLength),
            null);
    }

    public static FactValue FromCode(string value)
    {
        return new FactValue(
            FactValueKind.Code,
            NormalizeBounded(value, MaximumCodeLength),
            null);
    }

    public static FactValue FromInteger(long value)
    {
        return new FactValue(
            FactValueKind.Integer,
            value.ToString(CultureInfo.InvariantCulture),
            null);
    }

    public static FactValue FromDecimal(decimal value, string? unitCode = null)
    {
        return new FactValue(
            FactValueKind.Decimal,
            value.ToString("G29", CultureInfo.InvariantCulture),
            NormalizeUnitCode(unitCode, false));
    }

    public static FactValue FromBoolean(bool value)
    {
        return new FactValue(
            FactValueKind.Boolean,
            value ? "true" : "false",
            null);
    }

    public static FactValue FromDate(DateOnly value)
    {
        return new FactValue(
            FactValueKind.Date,
            value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            null);
    }

    public static FactValue FromDateTime(DateTime valueUtc)
    {
        if (valueUtc.Kind != DateTimeKind.Utc)
        {
            throw InvalidValue("A factual date-time value must be UTC.");
        }

        return new FactValue(
            FactValueKind.DateTime,
            valueUtc.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture),
            null);
    }

    public static FactValue FromMoney(decimal amount, string currencyCode)
    {
        if (amount < 0)
        {
            throw InvalidValue("A factual monetary value cannot be negative.");
        }

        return new FactValue(
            FactValueKind.Money,
            amount.ToString("G29", CultureInfo.InvariantCulture),
            NormalizeUnitCode(currencyCode, true));
    }

    public static FactValue Restore(
        FactValueKind kind,
        string canonicalValue,
        string? unitCode)
    {
        if (!Enum.IsDefined(kind))
        {
            throw InvalidValue("The factual value kind is invalid.");
        }

        string normalizedValue = NormalizeBounded(canonicalValue, MaximumTextLength);
        return kind switch
        {
            FactValueKind.Text => FromText(WithoutUnit(normalizedValue, unitCode)),
            FactValueKind.Code => FromCode(WithoutUnit(normalizedValue, unitCode)),
            FactValueKind.Integer when long.TryParse(
                WithoutUnit(normalizedValue, unitCode),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long integerValue) => FromInteger(integerValue),
            FactValueKind.Decimal when decimal.TryParse(
                normalizedValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out decimal decimalValue) => FromDecimal(decimalValue, unitCode),
            FactValueKind.Boolean when bool.TryParse(
                WithoutUnit(normalizedValue, unitCode),
                out bool booleanValue) =>
                FromBoolean(booleanValue),
            FactValueKind.Date when DateOnly.TryParseExact(
                WithoutUnit(normalizedValue, unitCode),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly dateValue) => FromDate(dateValue),
            FactValueKind.DateTime when DateTime.TryParseExact(
                WithoutUnit(normalizedValue, unitCode),
                "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime dateTimeValue) => FromDateTime(dateTimeValue),
            FactValueKind.Money when decimal.TryParse(
                normalizedValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out decimal moneyValue) => FromMoney(moneyValue, unitCode ?? string.Empty),
            _ => throw InvalidValue("The canonical factual value does not match its kind."),
        };
    }

    private static string NormalizeBounded(string value, int maximumLength)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0
            || normalizedValue.Length > maximumLength
            || (value is not null && value.Any(char.IsControl)))
        {
            throw InvalidValue(
                $"A factual value must contain between 1 and {maximumLength} valid characters.");
        }

        return normalizedValue;
    }

    private static string WithoutUnit(string normalizedValue, string? unitCode)
    {
        if (!string.IsNullOrWhiteSpace(unitCode))
        {
            throw InvalidValue("This factual value kind cannot declare a unit code.");
        }

        return normalizedValue;
    }

    private static string? NormalizeUnitCode(string? unitCode, bool requireCurrency)
    {
        string normalizedUnitCode = unitCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalizedUnitCode.Length == 0)
        {
            if (requireCurrency)
            {
                throw InvalidValue("A factual monetary value requires a currency code.");
            }

            return null;
        }

        if (normalizedUnitCode.Length > MaximumUnitCodeLength
            || !normalizedUnitCode.All(char.IsAsciiLetterUpper))
        {
            throw InvalidValue("The factual value unit code is invalid.");
        }

        if (requireCurrency && normalizedUnitCode.Length != 3)
        {
            throw InvalidValue("A factual monetary value requires a three-letter currency code.");
        }

        return normalizedUnitCode;
    }

    private static FactualEventValidationException InvalidValue(string message)
    {
        return new FactualEventValidationException(
            FactualEventErrorCodes.InvalidFactValue,
            message);
    }
}
