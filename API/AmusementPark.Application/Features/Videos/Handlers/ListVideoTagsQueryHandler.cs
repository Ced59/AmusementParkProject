using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Application.Features.Videos.Queries;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Handlers;

public sealed class ListVideoTagsQueryHandler : IQueryHandler<ListVideoTagsQuery, ApplicationResult<IReadOnlyCollection<VideoTag>>>
{
    private readonly IVideoTagRepository videoTagRepository;

    public ListVideoTagsQueryHandler(IVideoTagRepository videoTagRepository)
    {
        this.videoTagRepository = videoTagRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<VideoTag>>> HandleAsync(ListVideoTagsQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<VideoTag> tags = await this.videoTagRepository.GetAllAsync(cancellationToken);
        return ApplicationResult<IReadOnlyCollection<VideoTag>>.Success(tags);
    }
}
