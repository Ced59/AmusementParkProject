using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Commands;

/// <summary>
/// Met à jour un tag d'image.
/// </summary>
/// <param name="TagId">Identifiant du tag.</param>
/// <param name="Tag">Données cibles du tag.</param>
public sealed record UpdateImageTagCommand(string TagId, ImageTagWriteModel Tag) : ICommand<ApplicationResult<ImageTag>>;
