using AmusementPark.Application.Features.Videos.Contracts;

namespace AmusementPark.Application.Features.Videos.Ports;

public interface IVideoMetadataProvider
{
    Task<ResolvedVideoMetadata?> ResolveAsync(string url, CancellationToken cancellationToken);
}
