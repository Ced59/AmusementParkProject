using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Results;

namespace AmusementPark.Application.Features.Parks.Queries;

/// <summary>
/// Récupère les parcs mis en avant pour la home publique.
/// </summary>
/// <param name="Limit">Nombre maximum de parcs à retourner.</param>
/// <param name="ExcludedParkIds">Parcs déjà affichés ailleurs sur la home et à exclure.</param>
public sealed record GetHomeFeaturedParksQuery(int Limit, IReadOnlyCollection<string> ExcludedParkIds) : IQuery<ApplicationResult<IReadOnlyCollection<HomeFeaturedParkResult>>>;
