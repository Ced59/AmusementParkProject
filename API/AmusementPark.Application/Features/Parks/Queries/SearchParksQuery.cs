using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Countries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Queries;

/// <summary>
/// Recherche publique unifiée des parcs : nom, ville, code pays, nom localisé du pays et région.
/// </summary>
public sealed record SearchParksQuery(
    string? SearchTerm,
    WorldRegionFilter? Region,
    PagedQuery Paging,
    bool IncludeHidden = false,
    bool? IsVisible = null,
    AdminReviewStatus? AdminReviewStatus = null,
    ParkType? Type = null,
    string? CountryCode = null,
    bool? HasValidCoordinates = null,
    ClosedEntityFilter ClosedFilter = ClosedEntityFilter.OpenOnly,
    ParkAdminSortField SortField = ParkAdminSortField.Default,
    bool SortDescending = false) : IQuery<ApplicationResult<PagedResult<ParkListResult>>>;
