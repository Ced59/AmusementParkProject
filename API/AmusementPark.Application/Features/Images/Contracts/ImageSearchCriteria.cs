using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Contracts;

/// <summary>
/// Critères de recherche serveur pour l'administration d'un volume important d'images.
/// </summary>
public sealed record ImageSearchCriteria(
    string? Search = null,
    ImageCategory? Category = null,
    ImageOwnerType? OwnerType = null,
    string? OwnerId = null,
    string? TagId = null,
    bool? IsPublished = null,
    bool? HasOwner = null,
    bool? HasGeoLocation = null,
    string? SortBy = null,
    string? SortDirection = null,
    IReadOnlyCollection<string>? OwnerIds = null);
