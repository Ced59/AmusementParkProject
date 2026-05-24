using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Results;

namespace AmusementPark.Application.Features.Parks.Queries;

/// <summary>
/// Retourne les parcs les plus proches d'un parc source.
/// </summary>
public sealed record GetNearestParksQuery(
    string SourceParkId,
    int Limit,
    double? MaxDistanceKilometers = null,
    bool IncludeHidden = false) : IQuery<ApplicationResult<ParkDistanceResult>>;
