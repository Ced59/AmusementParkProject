using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Contracts;

public sealed record HistoricalDateInput(
    int Year,
    int? Month,
    int? Day,
    HistoryDatePrecision Precision,
    bool IsApproximate,
    DateQualifier? Qualifier);
