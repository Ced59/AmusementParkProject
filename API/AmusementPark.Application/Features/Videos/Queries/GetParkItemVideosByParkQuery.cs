using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Results;

namespace AmusementPark.Application.Features.Videos.Queries;

public sealed record GetParkItemVideosByParkQuery(
    PagedQuery Paging,
    string ParkId,
    VideoSearchCriteria Criteria,
    bool IncludeHidden = false) : IQuery<ApplicationResult<PagedResult<ParkItemVideoResult>>>;
