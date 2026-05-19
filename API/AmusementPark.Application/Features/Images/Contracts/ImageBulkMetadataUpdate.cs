using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Contracts;

/// <summary>
/// Patch de métadonnées appliqué à une sélection d'images depuis l'administration.
/// </summary>
public sealed record ImageBulkMetadataUpdate(
    bool? IsPublished = null,
    ImageCategory? Category = null,
    IReadOnlyCollection<string>? AddTagIds = null,
    IReadOnlyCollection<string>? RemoveTagIds = null);
