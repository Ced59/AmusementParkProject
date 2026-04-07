using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Queries;

/// <summary>
/// Retourne l'image courante d'un propriétaire pour une catégorie donnée.
/// </summary>
public sealed record GetCurrentImageQuery(string OwnerId, ImageOwnerType OwnerType, ImageCategory Category)
    : IQuery<ApplicationResult<Image>>;
