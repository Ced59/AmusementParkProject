using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Videos.Commands;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Handlers;

public sealed class CreateVideoTagCommandHandler : ICommandHandler<CreateVideoTagCommand, ApplicationResult<VideoTag>>
{
    private readonly IVideoTagRepository videoTagRepository;

    public CreateVideoTagCommandHandler(IVideoTagRepository videoTagRepository)
    {
        this.videoTagRepository = videoTagRepository;
    }

    public async Task<ApplicationResult<VideoTag>> HandleAsync(CreateVideoTagCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Tag is null)
        {
            return ApplicationResult<VideoTag>.Failure(ApplicationErrors.Required(nameof(command.Tag)));
        }

        string slug = (command.Tag.Slug ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug))
        {
            return ApplicationResult<VideoTag>.Failure(ApplicationErrors.Required(nameof(command.Tag.Slug)));
        }

        VideoTag? existing = await this.videoTagRepository.GetBySlugAsync(slug, cancellationToken);
        if (existing is not null)
        {
            return ApplicationResult<VideoTag>.Failure(VideoApplicationErrors.VideoTagAlreadyExists(slug));
        }

        VideoTagWriteModel normalizedTag = new VideoTagWriteModel
        {
            Slug = slug,
            Labels = command.Tag.Labels,
            Descriptions = command.Tag.Descriptions,
            IsActive = command.Tag.IsActive,
        };

        VideoTag created = await this.videoTagRepository.CreateAsync(normalizedTag, cancellationToken);
        return ApplicationResult<VideoTag>.Success(created);
    }
}
