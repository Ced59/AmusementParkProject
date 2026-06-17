using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Application.Features.Videos.Queries;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Handlers;

public sealed class GetVideoByIdQueryHandler : IQueryHandler<GetVideoByIdQuery, ApplicationResult<Video>>
{
    private readonly IVideoRepository videoRepository;

    public GetVideoByIdQueryHandler(IVideoRepository videoRepository)
    {
        this.videoRepository = videoRepository;
    }

    public async Task<ApplicationResult<Video>> HandleAsync(GetVideoByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.VideoId))
        {
            return ApplicationResult<Video>.Failure(VideoApplicationErrors.VideoNotFound());
        }

        Video? video = await this.videoRepository.GetByIdAsync(query.VideoId.Trim(), cancellationToken);
        return video is null
            ? ApplicationResult<Video>.Failure(VideoApplicationErrors.VideoNotFound())
            : ApplicationResult<Video>.Success(video);
    }
}
