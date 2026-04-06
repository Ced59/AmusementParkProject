using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Queries;

/// <summary>
/// Récupère une image par identifiant.
/// </summary>
/// <param name="ImageId">Identifiant de l'image.</param>
public sealed record GetImageByIdQuery(string ImageId) : IQuery<ApplicationResult<Image>>;
