using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record SharedProfileComparisonResult(
    DateTime CreatedAtUtc,
    ProfileComparisonCalculation Calculation);
