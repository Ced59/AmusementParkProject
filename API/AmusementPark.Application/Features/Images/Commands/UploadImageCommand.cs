using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Results;

namespace AmusementPark.Application.Features.Images.Commands;

/// <summary>
/// Upload une nouvelle image.
/// </summary>
/// <param name="Request">Données d'upload applicatives.</param>
/// <param name="AllowManagedCommentLifecycle">
/// Autorise le flux dédié des images de commentaire.
/// </param>
public sealed record UploadImageCommand(
    ImageUploadRequest Request,
    bool AllowManagedCommentLifecycle = false) : ICommand<ApplicationResult<UploadedImageResult>>;
