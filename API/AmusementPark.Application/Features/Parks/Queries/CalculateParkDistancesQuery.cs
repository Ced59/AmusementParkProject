using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Results;

namespace AmusementPark.Application.Features.Parks.Queries;

/// <summary>
/// Calcule les distances entre un parc source et un ou plusieurs parcs cibles.
/// </summary>
public sealed record CalculateParkDistancesQuery(
    string SourceParkId,
    IReadOnlyCollection<string> TargetParkIds,
    bool IncludeHidden = false) : IQuery<ApplicationResult<ParkDistanceResult>>;
