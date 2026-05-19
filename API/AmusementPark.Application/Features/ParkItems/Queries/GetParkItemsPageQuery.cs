using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Queries;

/// <summary>
/// Récupère une page d'éléments de parc côté administration.
/// </summary>
/// <param name="Paging">Paramètres de pagination.</param>
/// <param name="ParkId">Filtre par parc éventuel.</param>
/// <param name="Search">Texte de recherche éventuel.</param>
/// <param name="IncludeHidden">Indique si les éléments non visibles doivent être inclus.</param>
public sealed record GetParkItemsPageQuery(
    PagedQuery Paging,
    string? ParkId,
    string? Search,
    bool IncludeHidden = true,
    bool? IsVisible = null,
    AdminReviewStatus? AdminReviewStatus = null,
    ParkItemCategory? Category = null,
    ParkItemType? Type = null) : IQuery<ApplicationResult<PagedResult<ParkItemAdminListResult>>>;
