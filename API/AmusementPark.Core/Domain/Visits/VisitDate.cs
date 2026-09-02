using System.Globalization;

namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Date locale d'une visite, sans précision calendaire inventée.
/// </summary>
public sealed record VisitDate
{
    public VisitDate(
        int year,
        int? month,
        int? day,
        VisitDatePrecision precision,
        bool isApproximate)
    {
        Validate(year, month, day, precision);

        this.Year = year;
        this.Month = month;
        this.Day = day;
        this.Precision = precision;
        this.IsApproximate = isApproximate;
    }

    public int Year { get; }

    public int? Month { get; }

    public int? Day { get; }

    public VisitDatePrecision Precision { get; }

    public bool IsApproximate { get; }

    public static VisitDate ForYear(int year, bool isApproximate = false)
    {
        return new VisitDate(year, null, null, VisitDatePrecision.Year, isApproximate);
    }

    public static VisitDate ForMonth(int year, int month, bool isApproximate = false)
    {
        return new VisitDate(year, month, null, VisitDatePrecision.Month, isApproximate);
    }

    public static VisitDate ForDay(int year, int month, int day, bool isApproximate = false)
    {
        return new VisitDate(year, month, day, VisitDatePrecision.Day, isApproximate);
    }

    /// <summary>
    /// Première date calendaire compatible avec les informations connues.
    /// Cette borne n'ajoute pas de précision à la valeur métier.
    /// </summary>
    public DateOnly GetEarliestPossibleDate()
    {
        int month = this.Month ?? 1;
        int day = this.Day ?? 1;
        return new DateOnly(this.Year, month, day);
    }

    /// <summary>
    /// Dernière date calendaire compatible avec les informations connues.
    /// Cette borne n'ajoute pas de précision à la valeur métier.
    /// </summary>
    public DateOnly GetLatestPossibleDate()
    {
        int month = this.Month ?? 12;
        int day = this.Day ?? DateTime.DaysInMonth(this.Year, month);
        return new DateOnly(this.Year, month, day);
    }

    public override string ToString()
    {
        string prefix = this.IsApproximate ? "~" : string.Empty;
        string value = this.Precision switch
        {
            VisitDatePrecision.Year => this.Year.ToString("D4", CultureInfo.InvariantCulture),
            VisitDatePrecision.Month => string.Create(
                CultureInfo.InvariantCulture,
                $"{this.Year:D4}-{this.Month!.Value:D2}"),
            VisitDatePrecision.Day => string.Create(
                CultureInfo.InvariantCulture,
                $"{this.Year:D4}-{this.Month!.Value:D2}-{this.Day!.Value:D2}"),
            _ => throw new InvalidOperationException("The visit date precision is invalid."),
        };

        return string.Concat(prefix, value);
    }

    private static void Validate(
        int year,
        int? month,
        int? day,
        VisitDatePrecision precision)
    {
        if (!Enum.IsDefined(precision))
        {
            throw CreateValidationException(
                VisitDateErrorCodes.InvalidPrecision,
                "The visit date precision is invalid.",
                nameof(precision));
        }

        if (year < DateOnly.MinValue.Year || year > DateOnly.MaxValue.Year)
        {
            throw CreateValidationException(
                VisitDateErrorCodes.InvalidYear,
                "The visit year is outside the supported calendar range.",
                nameof(year));
        }

        if (precision == VisitDatePrecision.Year)
        {
            if (month.HasValue)
            {
                throw CreateValidationException(
                    VisitDateErrorCodes.MonthForbidden,
                    "A year-precision visit date cannot contain a month.",
                    nameof(month));
            }

            if (day.HasValue)
            {
                throw CreateValidationException(
                    VisitDateErrorCodes.DayForbidden,
                    "A year-precision visit date cannot contain a day.",
                    nameof(day));
            }

            return;
        }

        if (!month.HasValue)
        {
            throw CreateValidationException(
                VisitDateErrorCodes.MonthRequired,
                "The selected visit date precision requires a month.",
                nameof(month));
        }

        if (month.Value is < 1 or > 12)
        {
            throw CreateValidationException(
                VisitDateErrorCodes.InvalidMonth,
                "The visit month must be between 1 and 12.",
                nameof(month));
        }

        if (precision == VisitDatePrecision.Month)
        {
            if (day.HasValue)
            {
                throw CreateValidationException(
                    VisitDateErrorCodes.DayForbidden,
                    "A month-precision visit date cannot contain a day.",
                    nameof(day));
            }

            return;
        }

        if (!day.HasValue)
        {
            throw CreateValidationException(
                VisitDateErrorCodes.DayRequired,
                "A day-precision visit date requires a day.",
                nameof(day));
        }

        int maximumDay = DateTime.DaysInMonth(year, month.Value);
        if (day.Value < 1 || day.Value > maximumDay)
        {
            throw CreateValidationException(
                VisitDateErrorCodes.InvalidDay,
                "The visit day does not exist in the selected month and year.",
                nameof(day));
        }
    }

    private static VisitDateValidationException CreateValidationException(
        string errorCode,
        string message,
        string parameterName)
    {
        return new VisitDateValidationException(errorCode, message, parameterName);
    }
}
