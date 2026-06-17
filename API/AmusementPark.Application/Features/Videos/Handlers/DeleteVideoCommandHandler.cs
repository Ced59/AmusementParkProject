using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Videos.Commands;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Handlers;

public sealed class DeleteVideoCommandHandler : ICommandHandler<DeleteVideoCommand, ApplicationResult>
{
    private readonly IVideoRepository videoRepository;
    private readonly IPublicSeoUpdateNotifier publicSeoUpdateNotifier;

    public DeleteVideoCommandHandler(
        IVideoRepository videoRepository,
        IPublicSeoUpdateNotifier publicSeoUpdateNotifier)
    {
        this.videoRepository = videoRepository;
        this.publicSeoUpdateNotifier = publicSeoUpdateNotifier;
    }

    public async Task<ApplicationResult> HandleAsync(DeleteVideoCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.VideoId))
        {
            return ApplicationResult.Failure(VideoApplicationErrors.VideoNotFound());
        }

        string videoId = command.VideoId.Trim();
        Video? existing = await this.videoRepository.GetByIdAsync(videoId, cancellationToken);
        if (existing is null)
        {
            return ApplicationResult.Failure(VideoApplicationErrors.VideoNotFound());
        }

        bool deleted = await this.videoRepository.DeleteAsync(videoId, cancellationToken);
        if (!deleted)
        {
            return ApplicationResult.Failure(VideoApplicationErrors.VideoNotFound());
        }

        await this.publicSeoUpdateNotifier.NotifyAsync(
            new PublicSeoUpdate
            {
                PreviousVideos = PublicSeoVideoSnapshot.FromVideos(new[] { existing }),
            },
            cancellationToken);

        return ApplicationResult.Success();
    }
}
