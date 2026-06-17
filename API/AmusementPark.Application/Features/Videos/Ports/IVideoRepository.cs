using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Ports;

public interface IVideoRepository
{
    Task<PagedResult<Video>> GetPageAsync(int page, int pageSize, VideoSearchCriteria criteria, CancellationToken cancellationToken);

    Task<Video?> GetByIdAsync(string videoId, CancellationToken cancellationToken);

    Task<Video> CreateAsync(Video video, CancellationToken cancellationToken);

    Task<Video?> UpdateAsync(string videoId, Video video, CancellationToken cancellationToken);

    Task<Video?> SetThumbnailImageAsync(string videoId, string thumbnailImageId, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string videoId, CancellationToken cancellationToken);
}
