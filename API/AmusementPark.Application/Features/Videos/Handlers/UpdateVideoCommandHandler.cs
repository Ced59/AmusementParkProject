using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Videos.Commands;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Handlers;

public sealed class UpdateVideoCommandHandler : ICommandHandler<UpdateVideoCommand, ApplicationResult<Video>>
{
    private readonly IVideoRepository videoRepository;
    private readonly IVideoMetadataProvider metadataProvider;
    private readonly IVideoThumbnailImporter thumbnailImporter;

    public UpdateVideoCommandHandler(
        IVideoRepository videoRepository,
        IVideoMetadataProvider metadataProvider,
        IVideoThumbnailImporter thumbnailImporter)
    {
        this.videoRepository = videoRepository;
        this.metadataProvider = metadataProvider;
        this.thumbnailImporter = thumbnailImporter;
    }

    public async Task<ApplicationResult<Video>> HandleAsync(UpdateVideoCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.VideoId))
        {
            return ApplicationResult<Video>.Failure(VideoApplicationErrors.VideoNotFound());
        }

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
            Video? existing = await this.videoRepository.GetByIdAsync(command.VideoId.Trim(), cancellationToken);
            if (existing is null)
            {
                return ApplicationResult<Video>.Failure(VideoApplicationErrors.VideoNotFound());
            }

            Video video = VideoWriteModelMapper.ToDomain(command.Video, metadata);
            video.ThumbnailImageId = existing.ThumbnailImageId;
            Video? updated = await this.videoRepository.UpdateAsync(command.VideoId.Trim(), video, cancellationToken);
            if (updated is null)
            {
                return ApplicationResult<Video>.Failure(VideoApplicationErrors.VideoNotFound());
            }

            bool shouldImportThumbnail = string.IsNullOrWhiteSpace(existing.ThumbnailImageId) ||
                                         !string.Equals(existing.ThumbnailUrl, metadata.ThumbnailUrl, StringComparison.Ordinal);
            if (shouldImportThumbnail)
            {
                string? thumbnailImageId = await this.thumbnailImporter.ImportAsync(metadata, updated.Id!, cancellationToken);
                if (!string.IsNullOrWhiteSpace(thumbnailImageId))
                {
                    updated = await this.videoRepository.SetThumbnailImageAsync(updated.Id!, thumbnailImageId, cancellationToken) ?? updated;
                }
            }

            return ApplicationResult<Video>.Success(updated);
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
