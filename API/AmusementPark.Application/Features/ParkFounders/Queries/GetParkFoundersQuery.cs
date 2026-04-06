using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFounders.Queries;

/// <summary>
/// Récupère la liste des park founders.
/// </summary>
public sealed record GetParkFoundersQuery : IQuery<ApplicationResult<IReadOnlyCollection<ParkFounder>>>;
