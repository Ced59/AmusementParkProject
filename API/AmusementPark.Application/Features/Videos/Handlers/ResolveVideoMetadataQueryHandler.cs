using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Application.Features.Videos.Queries;

namespace AmusementPark.Application.Features.Videos.Handlers;

public sealed class ResolveVideoMetadataQueryHandler : IQueryHandler<ResolveVideoMetadataQuery, ApplicationResult<ResolvedVideoMetadata>>
{
    private readonly IVideoMetadataProvider metadataProvider;

    public ResolveVideoMetadataQueryHandler(IVideoMetadataProvider metadataProvider)
    {
        this.metadataProvider = metadataProvider;
    }

    public async Task<ApplicationResult<ResolvedVideoMetadata>> HandleAsync(ResolveVideoMetadataQuery query, CancellationToken cancellationToken = default)
    {
        ResolvedVideoMetadata? metadata = await this.metadataProvider.ResolveAsync(query.Url, cancellationToken);
        return metadata is null
            ? ApplicationResult<ResolvedVideoMetadata>.Failure(VideoApplicationErrors.VideoUrlInvalid())
            : ApplicationResult<ResolvedVideoMetadata>.Success(metadata);
    }
}
