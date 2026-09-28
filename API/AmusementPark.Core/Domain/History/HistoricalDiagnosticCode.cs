namespace AmusementPark.Core.Domain.History;

public enum HistoricalDiagnosticCode
{
    MissingStartDate = 0,
    MissingEndDate = 1,
    AmbiguousInterval = 2,
    OpeningAfterClosure = 3,
    IncompatibleLineageCycle = 4,
    OverlappingNames = 5,
    MissingZone = 6,
    MissingSource = 7,
}
