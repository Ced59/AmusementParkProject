using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Queries;

/// <summary>
/// Récupère les éléments d'un parc.
/// </summary>
/// <param name="ParkId">Identifiant du parc.</param>
/// <param name="Paging">Pagination demandée.</param>
/// <param name="IncludeHidden">Indique si les éléments non visibles doivent être inclus.</param>
/// <param name="ClosedFilter">Filtre appliqué aux éléments fermés définitivement.</param>
/// <param name="Search">Recherche textuelle optionnelle.</param>
/// <param name="Category">Catégorie optionnelle.</param>
/// <param name="Type">Type optionnel.</param>
/// <param name="ZoneId">Zone optionnelle.</param>
public sealed record GetParkItemsByParkIdQuery(
    string ParkId,
    PagedQuery Paging,
    bool IncludeHidden = true,
    ClosedEntityFilter ClosedFilter = ClosedEntityFilter.OpenOnly,
    string? Search = null,
    ParkItemCategory? Category = null,
    ParkItemType? Type = null,
    string? ZoneId = null) : IQuery<ApplicationResult<PagedResult<ParkItemListResult>>>;
