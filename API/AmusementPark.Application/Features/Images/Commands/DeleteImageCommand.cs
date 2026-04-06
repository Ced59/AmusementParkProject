using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Images.Commands;

/// <summary>
/// Supprime une image.
/// </summary>
/// <param name="ImageId">Identifiant de l'image.</param>
public sealed record DeleteImageCommand(string ImageId) : ICommand<ApplicationResult>;
