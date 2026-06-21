using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Queries;

/// <summary>
/// Récupère une sélection aléatoire de parcs visibles publiquement.
/// </summary>
/// <param name="Limit">Nombre maximum de parcs à retourner.</param>
public sealed record GetRandomVisibleParksQuery(
    int Limit,
    ClosedEntityFilter ClosedFilter = ClosedEntityFilter.OpenOnly) : IQuery<ApplicationResult<IReadOnlyCollection<Park>>>;
