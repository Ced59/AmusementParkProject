using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Queries;

/// <summary>
/// Recherche les parcs dans un rayon donné.
/// </summary>
/// <param name="Latitude">Latitude du centre.</param>
/// <param name="Longitude">Longitude du centre.</param>
/// <param name="RadiusInKilometers">Rayon de recherche en kilomètres.</param>
/// <param name="IncludeHidden">Indique si les parcs non visibles doivent être inclus.</param>
public sealed record SearchParksByLocationQuery(double Latitude, double Longitude, double RadiusInKilometers, bool IncludeHidden = false) : IQuery<ApplicationResult<IReadOnlyCollection<Park>>>;
