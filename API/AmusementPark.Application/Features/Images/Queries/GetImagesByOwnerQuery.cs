using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Queries;

/// <summary>
/// Retourne les images d'un propriétaire pour une catégorie donnée.
/// </summary>
public sealed record GetImagesByOwnerQuery(string OwnerId, ImageOwnerType OwnerType, ImageCategory Category)
    : IQuery<ApplicationResult<IReadOnlyCollection<Image>>>;
