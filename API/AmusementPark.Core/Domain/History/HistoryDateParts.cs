namespace AmusementPark.Core.Domain.History;

internal sealed record HistoryDateParts(
    int Year,
    int? Month,
    int? Day,
    HistoryDatePrecision Precision);
