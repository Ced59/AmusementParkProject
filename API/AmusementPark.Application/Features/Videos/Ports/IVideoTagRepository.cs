using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Ports;

public interface IVideoTagRepository
{
    Task<IReadOnlyCollection<VideoTag>> GetAllAsync(CancellationToken cancellationToken);

    Task<VideoTag?> GetByIdAsync(string tagId, CancellationToken cancellationToken);

    Task<VideoTag?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<VideoTag> CreateAsync(VideoTagWriteModel tag, CancellationToken cancellationToken);

    Task<VideoTag?> UpdateAsync(string tagId, VideoTagWriteModel tag, CancellationToken cancellationToken);
}
