using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Videos.Commands;
using AmusementPark.Application.Features.Videos.Ports;

namespace AmusementPark.Application.Features.Videos.Handlers;

public sealed class DeleteVideoCommandHandler : ICommandHandler<DeleteVideoCommand, ApplicationResult>
{
    private readonly IVideoRepository videoRepository;

    public DeleteVideoCommandHandler(IVideoRepository videoRepository)
    {
        this.videoRepository = videoRepository;
    }

    public async Task<ApplicationResult> HandleAsync(DeleteVideoCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.VideoId))
        {
            return ApplicationResult.Failure(VideoApplicationErrors.VideoNotFound());
        }

        bool deleted = await this.videoRepository.DeleteAsync(command.VideoId.Trim(), cancellationToken);
        return deleted ? ApplicationResult.Success() : ApplicationResult.Failure(VideoApplicationErrors.VideoNotFound());
    }
}
