using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFounders.Queries;

/// <summary>
/// Récupère un park founder par identifiant.
/// </summary>
/// <param name="Id">Identifiant métier.</param>
public sealed record GetParkFounderByIdQuery(string Id) : IQuery<ApplicationResult<ParkFounder>>;
