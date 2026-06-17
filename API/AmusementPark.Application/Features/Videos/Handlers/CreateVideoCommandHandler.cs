using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Videos.Commands;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Handlers;

public sealed class CreateVideoCommandHandler : ICommandHandler<CreateVideoCommand, ApplicationResult<Video>>
{
    private readonly IVideoRepository videoRepository;
    private readonly IVideoMetadataProvider metadataProvider;
    private readonly IVideoThumbnailImporter thumbnailImporter;

    public CreateVideoCommandHandler(
        IVideoRepository videoRepository,
        IVideoMetadataProvider metadataProvider,
        IVideoThumbnailImporter thumbnailImporter)
    {
        this.videoRepository = videoRepository;
        this.metadataProvider = metadataProvider;
        this.thumbnailImporter = thumbnailImporter;
    }

    public async Task<ApplicationResult<Video>> HandleAsync(CreateVideoCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Video is null)
        {
            return ApplicationResult<Video>.Failure(ApplicationErrors.Required(nameof(command.Video)));
        }

        if (!VideoWriteModelMapper.HasValidOwner(command.Video))
        {
            return ApplicationResult<Video>.Failure(VideoApplicationErrors.InvalidOwner());
        }

        ResolvedVideoMetadata? metadata = await this.metadataProvider.ResolveAsync(command.Video.OriginalUrl, cancellationToken);
        if (metadata is null)
        {
            return ApplicationResult<Video>.Failure(VideoApplicationErrors.VideoUrlInvalid());
        }

        try
        {
            Video video = VideoWriteModelMapper.ToDomain(command.Video, metadata);
            Video created = await this.videoRepository.CreateAsync(video, cancellationToken);
            string? thumbnailImageId = await this.thumbnailImporter.ImportAsync(metadata, created.Id!, cancellationToken);
            if (!string.IsNullOrWhiteSpace(thumbnailImageId))
            {
                created = await this.videoRepository.SetThumbnailImageAsync(created.Id!, thumbnailImageId, cancellationToken) ?? created;
            }

            return ApplicationResult<Video>.Success(created);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<Video>.Failure(VideoApplicationErrors.VideoWriteFailed());
        }
    }
}
