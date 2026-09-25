using System.Globalization;

namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Date historique civile dont les parties absentes ne sont jamais complétées artificiellement.
/// </summary>
public sealed record HistoricalDate
{
    public HistoricalDate(
        int year,
        int? month,
        int? day,
        HistoryDatePrecision precision,
        bool isApproximate,
        DateQualifier? qualifier)
    {
        Validate(year, month, day, precision, isApproximate, qualifier);

        this.Year = year;
        this.Month = month;
        this.Day = day;
        this.Precision = precision;
        this.IsApproximate = isApproximate;
        this.Qualifier = qualifier;
    }

    public int Year { get; }

    public int? Month { get; }

    public int? Day { get; }

    public HistoryDatePrecision Precision { get; }

    public bool IsApproximate { get; }

    public DateQualifier? Qualifier { get; }

    public bool HasUncertainPosition => this.Precision != HistoryDatePrecision.Day
        || this.IsApproximate
        || this.Qualifier.HasValue;

    public bool HasUncertainBoundary => this.IsApproximate
        || this.Qualifier is DateQualifier.Early
            or DateQualifier.Mid
            or DateQualifier.Late
            or DateQualifier.Circa
        || (this.Precision != HistoryDatePrecision.Day
            && this.Qualifier is not DateQualifier.Before and not DateQualifier.After);

    public static HistoricalDate ForYear(
        int year,
        bool isApproximate = false,
        DateQualifier? qualifier = null)
    {
        return new HistoricalDate(
            year,
            null,
            null,
            HistoryDatePrecision.Year,
            isApproximate,
            qualifier);
    }

    public static HistoricalDate ForMonth(
        int year,
        int month,
        bool isApproximate = false,
        DateQualifier? qualifier = null)
    {
        return new HistoricalDate(
            year,
            month,
            null,
            HistoryDatePrecision.Month,
            isApproximate,
            qualifier);
    }

    public static HistoricalDate ForDay(
        int year,
        int month,
        int day,
        bool isApproximate = false,
        DateQualifier? qualifier = null)
    {
        return new HistoricalDate(
            year,
            month,
            day,
            HistoryDatePrecision.Day,
            isApproximate,
            qualifier);
    }

    public HistoricalDateEnvelope GetEnvelope()
    {
        DateOnly baseStart = this.GetBaseStart();
        DateOnly baseEnd = this.GetBaseEnd();

        if (this.Qualifier == DateQualifier.Before)
        {
            return new HistoricalDateEnvelope(null, baseStart.AddDays(-1), true);
        }

        if (this.Qualifier == DateQualifier.After)
        {
            return new HistoricalDateEnvelope(baseEnd.AddDays(1), null, true);
        }

        return new HistoricalDateEnvelope(baseStart, baseEnd, this.HasUncertainPosition);
    }

    public override string ToString()
    {
        string value = this.Precision switch
        {
            HistoryDatePrecision.Year => this.Year.ToString("D4", CultureInfo.InvariantCulture),
            HistoryDatePrecision.Month => string.Create(
                CultureInfo.InvariantCulture,
                $"{this.Year:D4}-{this.Month!.Value:D2}"),
            HistoryDatePrecision.Day => string.Create(
                CultureInfo.InvariantCulture,
                $"{this.Year:D4}-{this.Month!.Value:D2}-{this.Day!.Value:D2}"),
            _ => throw new InvalidOperationException("The historical date precision is invalid."),
        };

        string qualifier = this.Qualifier?.ToString().ToLowerInvariant() ?? string.Empty;
        string qualifiedValue = string.IsNullOrEmpty(qualifier)
            ? value
            : string.Concat(qualifier, ":", value);

        return this.IsApproximate && this.Qualifier != DateQualifier.Circa
            ? string.Concat("~", qualifiedValue)
            : qualifiedValue;
    }

    private DateOnly GetBaseStart()
    {
        int month = this.Month ?? 1;
        int day = this.Day ?? 1;
        return new DateOnly(this.Year, month, day);
    }

    private DateOnly GetBaseEnd()
    {
        int month = this.Month ?? 12;
        int day = this.Day ?? DateTime.DaysInMonth(this.Year, month);
        return new DateOnly(this.Year, month, day);
    }

    private static void Validate(
        int year,
        int? month,
        int? day,
        HistoryDatePrecision precision,
        bool isApproximate,
        DateQualifier? qualifier)
    {
        if (!Enum.IsDefined(precision))
        {
            throw Invalid(
                HistoricalTemporalErrorCodes.InvalidPrecision,
                "The historical date precision is invalid.",
                nameof(precision));
        }

        if (qualifier.HasValue && !Enum.IsDefined(qualifier.Value))
        {
            throw Invalid(
                HistoricalTemporalErrorCodes.InvalidQualifier,
                "The historical date qualifier is invalid.",
                nameof(qualifier));
        }

        if (year < DateOnly.MinValue.Year || year > DateOnly.MaxValue.Year)
        {
            throw Invalid(
                HistoricalTemporalErrorCodes.InvalidYear,
                "The historical year is outside the supported calendar range.",
                nameof(year));
        }

        ValidateCalendarParts(year, month, day, precision);

        if (qualifier == DateQualifier.Circa && !isApproximate)
        {
            throw Invalid(
                HistoricalTemporalErrorCodes.CircaRequiresApproximation,
                "A circa historical date must be approximate.",
                nameof(isApproximate));
        }

        DateOnly baseStart = new DateOnly(year, month ?? 1, day ?? 1);
        int endMonth = month ?? 12;
        DateOnly baseEnd = new DateOnly(
            year,
            endMonth,
            day ?? DateTime.DaysInMonth(year, endMonth));

        if (qualifier == DateQualifier.Before && baseStart == DateOnly.MinValue)
        {
            throw Invalid(
                HistoricalTemporalErrorCodes.EmptyQualifiedRange,
                "A before qualifier cannot precede the supported calendar.",
                nameof(qualifier));
        }

        if (qualifier == DateQualifier.After && baseEnd == DateOnly.MaxValue)
        {
            throw Invalid(
                HistoricalTemporalErrorCodes.EmptyQualifiedRange,
                "An after qualifier cannot exceed the supported calendar.",
                nameof(qualifier));
        }
    }

    private static void ValidateCalendarParts(
        int year,
        int? month,
        int? day,
        HistoryDatePrecision precision)
    {
        if (precision == HistoryDatePrecision.Year)
        {
            if (month.HasValue)
            {
                throw Invalid(
                    HistoricalTemporalErrorCodes.MonthForbidden,
                    "A year-precision historical date cannot contain a month.",
                    nameof(month));
            }

            if (day.HasValue)
            {
                throw Invalid(
                    HistoricalTemporalErrorCodes.DayForbidden,
                    "A year-precision historical date cannot contain a day.",
                    nameof(day));
            }

            return;
        }

        if (!month.HasValue)
        {
            throw Invalid(
                HistoricalTemporalErrorCodes.MonthRequired,
                "The selected historical date precision requires a month.",
                nameof(month));
        }

        if (month.Value is < 1 or > 12)
        {
            throw Invalid(
                HistoricalTemporalErrorCodes.InvalidMonth,
                "The historical month must be between 1 and 12.",
                nameof(month));
        }

        if (precision == HistoryDatePrecision.Month)
        {
            if (day.HasValue)
            {
                throw Invalid(
                    HistoricalTemporalErrorCodes.DayForbidden,
                    "A month-precision historical date cannot contain a day.",
                    nameof(day));
            }

            return;
        }

        if (!day.HasValue)
        {
            throw Invalid(
                HistoricalTemporalErrorCodes.DayRequired,
                "A day-precision historical date requires a day.",
                nameof(day));
        }

        int maximumDay = DateTime.DaysInMonth(year, month.Value);
        if (day.Value < 1 || day.Value > maximumDay)
        {
            throw Invalid(
                HistoricalTemporalErrorCodes.InvalidDay,
                "The historical day does not exist in the selected month and year.",
                nameof(day));
        }
    }

    private static HistoricalTemporalValidationException Invalid(
        string errorCode,
        string message,
        string parameterName)
    {
        return new HistoricalTemporalValidationException(errorCode, message, parameterName);
    }
}
