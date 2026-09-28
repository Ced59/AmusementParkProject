using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Results;

namespace AmusementPark.Application.Features.LiveData.Queries;

public sealed record GetAdminLiveTargetMappingsQuery(LiveTargetMappingSearchCriteria Criteria)
    : IQuery<ApplicationResult<PagedResult<LiveTargetMappingResult>>>;
