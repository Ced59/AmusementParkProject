using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record VisitRecapShareDateResult(
    int Year,
    int? Month,
    int? Day,
    ShareDatePrecision Precision,
    bool IsApproximate);
