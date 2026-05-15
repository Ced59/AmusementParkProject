using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Results;

namespace AmusementPark.Application.Features.Parks.Queries;

/// <summary>
/// Requête de statistiques publiques pour la home.
/// </summary>
public sealed record GetPublicHomeStatsQuery : IQuery<ApplicationResult<PublicHomeStatsResult>>;
