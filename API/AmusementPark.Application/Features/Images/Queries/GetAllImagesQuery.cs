using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Queries;

/// <summary>
/// Retourne la liste complète des images.
/// </summary>
public sealed record GetAllImagesQuery : IQuery<ApplicationResult<IReadOnlyCollection<Image>>>;
