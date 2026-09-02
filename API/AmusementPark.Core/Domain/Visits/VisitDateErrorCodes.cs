namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Codes métier stables associés à la validation d'une date de visite.
/// </summary>
public static class VisitDateErrorCodes
{
    public const string InvalidPrecision = "visit-date.invalid-precision";

    public const string InvalidYear = "visit-date.invalid-year";

    public const string MonthRequired = "visit-date.month-required";

    public const string MonthForbidden = "visit-date.month-forbidden";

    public const string InvalidMonth = "visit-date.invalid-month";

    public const string DayRequired = "visit-date.day-required";

    public const string DayForbidden = "visit-date.day-forbidden";

    public const string InvalidDay = "visit-date.invalid-day";
}
