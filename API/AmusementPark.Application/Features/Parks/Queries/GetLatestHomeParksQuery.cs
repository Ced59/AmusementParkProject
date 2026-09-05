using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Results;

namespace AmusementPark.Application.Features.Parks.Queries;

/// <summary>
/// Récupère les parcs récemment créés ou mis à jour pour la home publique.
/// </summary>
/// <param name="Limit">Nombre maximum de parcs à retourner.</param>
public sealed record GetLatestHomeParksQuery(int Limit) : IQuery<ApplicationResult<IReadOnlyCollection<HomeFeaturedParkResult>>>;
