using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Queries;

public sealed record GetVideosPageQuery(PagedQuery Paging, VideoSearchCriteria Criteria) : IQuery<ApplicationResult<PagedResult<Video>>>;
