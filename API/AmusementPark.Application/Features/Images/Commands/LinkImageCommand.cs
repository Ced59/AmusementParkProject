using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Commands;

/// <summary>
/// Lie une image à un propriétaire fonctionnel.
/// </summary>
/// <param name="ImageId">Identifiant de l'image.</param>
/// <param name="OwnerType">Type de propriétaire.</param>
/// <param name="OwnerId">Identifiant du propriétaire.</param>
/// <param name="Description">Description à appliquer si fournie.</param>
/// <param name="SetAsCurrent">Indique si l'image doit devenir courante.</param>
public sealed record LinkImageCommand(
    string ImageId,
    ImageOwnerType OwnerType,
    string OwnerId,
    string? Description = null,
    bool SetAsCurrent = true) : ICommand<ApplicationResult<Image>>;
