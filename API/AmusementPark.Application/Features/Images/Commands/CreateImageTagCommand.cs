using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Commands;

/// <summary>
/// Crée un tag d'image.
/// </summary>
/// <param name="Tag">Données du tag à créer.</param>
public sealed record CreateImageTagCommand(ImageTagWriteModel Tag) : ICommand<ApplicationResult<ImageTag>>;
