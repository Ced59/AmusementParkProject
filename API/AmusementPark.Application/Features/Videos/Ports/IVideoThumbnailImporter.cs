using AmusementPark.Application.Features.Videos.Contracts;

namespace AmusementPark.Application.Features.Videos.Ports;

public interface IVideoThumbnailImporter
{
    Task<string?> ImportAsync(ResolvedVideoMetadata metadata, string videoId, CancellationToken cancellationToken);
}
