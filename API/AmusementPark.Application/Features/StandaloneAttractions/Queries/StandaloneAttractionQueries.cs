using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.StandaloneAttractions.Queries;

public sealed record GetStandaloneAttractionByIdQuery(string Id, bool IncludeHidden)
    : IQuery<ApplicationResult<StandaloneAttraction>>;

public sealed record GetStandaloneAttractionsPageQuery(
    PagedQuery Paging,
    string? Search,
    bool IncludeHidden,
    bool? IsVisible,
    AdminReviewStatus? AdminReviewStatus,
    ParkItemType? Type,
    string? CountryCode,
    string? ManufacturerId,
    StandaloneAttractionAdminSortField SortField,
    bool SortDescending)
    : IQuery<ApplicationResult<PagedResult<StandaloneAttraction>>>;

