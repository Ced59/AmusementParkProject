using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Contracts;

public sealed record HistoricalPeriodInput(
    HistoricalDateInput? Start,
    HistoricalDateInput? End,
    PeriodBoundaryConfidence StartConfidence,
    PeriodBoundaryConfidence EndConfidence);
