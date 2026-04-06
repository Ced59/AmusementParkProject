using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Queries;

/// <summary>
/// Retourne la liste des tags d'image.
/// </summary>
public sealed record ListImageTagsQuery : IQuery<ApplicationResult<IReadOnlyCollection<ImageTag>>>;
