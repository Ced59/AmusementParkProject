using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Application.Features.Videos.Queries;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Handlers;

public sealed class GetVideosPageQueryHandler : IQueryHandler<GetVideosPageQuery, ApplicationResult<PagedResult<Video>>>
{
    private readonly IVideoRepository videoRepository;

    public GetVideosPageQueryHandler(IVideoRepository videoRepository)
    {
        this.videoRepository = videoRepository;
    }

    public async Task<ApplicationResult<PagedResult<Video>>> HandleAsync(GetVideosPageQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Paging.Page <= 0 || query.Paging.PageSize <= 0)
        {
            return ApplicationResult<PagedResult<Video>>.Failure(ApplicationErrors.InvalidPagination());
        }

        PagedResult<Video> page = await this.videoRepository.GetPageAsync(query.Paging.Page, query.Paging.PageSize, query.Criteria, cancellationToken);
        return ApplicationResult<PagedResult<Video>>.Success(page);
    }
}
