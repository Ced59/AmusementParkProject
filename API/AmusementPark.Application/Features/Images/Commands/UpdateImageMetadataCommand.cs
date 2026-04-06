using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Commands;

/// <summary>
/// Met à jour les métadonnées d'une image.
/// </summary>
/// <param name="ImageId">Identifiant de l'image.</param>
/// <param name="Metadata">Métadonnées cibles.</param>
public sealed record UpdateImageMetadataCommand(string ImageId, ImageMetadataUpdate Metadata) : ICommand<ApplicationResult<Image>>;
