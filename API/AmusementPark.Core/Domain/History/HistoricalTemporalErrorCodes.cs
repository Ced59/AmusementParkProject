namespace AmusementPark.Core.Domain.History;

public static class HistoricalTemporalErrorCodes
{
    public const string InvalidPrecision = "history.temporal.invalid-precision";

    public const string InvalidQualifier = "history.temporal.invalid-qualifier";

    public const string InvalidYear = "history.temporal.invalid-year";

    public const string MonthForbidden = "history.temporal.month-forbidden";

    public const string MonthRequired = "history.temporal.month-required";

    public const string InvalidMonth = "history.temporal.invalid-month";

    public const string DayForbidden = "history.temporal.day-forbidden";

    public const string DayRequired = "history.temporal.day-required";

    public const string InvalidDay = "history.temporal.invalid-day";

    public const string CircaRequiresApproximation = "history.temporal.circa-requires-approximation";

    public const string EmptyQualifiedRange = "history.temporal.empty-qualified-range";

    public const string InvalidEnvelope = "history.temporal.invalid-envelope";

    public const string PeriodRequiresBoundary = "history.temporal.period-requires-boundary";

    public const string InvalidBoundaryConfidence = "history.temporal.invalid-boundary-confidence";

    public const string OpenBoundaryConfidenceMustBeConfirmed =
        "history.temporal.open-boundary-confidence-must-be-confirmed";

    public const string PeriodEndBeforeStart = "history.temporal.period-end-before-start";
}
