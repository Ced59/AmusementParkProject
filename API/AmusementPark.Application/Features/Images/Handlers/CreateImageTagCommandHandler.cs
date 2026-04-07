using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler de création de tag d'image.
/// </summary>
public sealed class CreateImageTagCommandHandler : ICommandHandler<CreateImageTagCommand, ApplicationResult<ImageTag>>
{
    private readonly IImageTagRepository imageTagRepository;

    public CreateImageTagCommandHandler(IImageTagRepository imageTagRepository)
    {
        this.imageTagRepository = imageTagRepository;
    }

    public async Task<ApplicationResult<ImageTag>> HandleAsync(CreateImageTagCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Tag is null)
        {
            return ApplicationResult<ImageTag>.Failure(ApplicationErrors.Required(nameof(command.Tag)));
        }

        string slug = (command.Tag.Slug ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug))
        {
            return ApplicationResult<ImageTag>.Failure(ApplicationErrors.Required(nameof(command.Tag.Slug)));
        }

        ImageTag? existing = await this.imageTagRepository.GetBySlugAsync(slug, cancellationToken);
        if (existing is not null)
        {
            return ApplicationResult<ImageTag>.Failure(ImageApplicationErrors.ImageTagAlreadyExists(slug));
        }

        ImageTagWriteModel normalizedTag = new ImageTagWriteModel
        {
            Slug = slug,
            Labels = command.Tag.Labels,
            Descriptions = command.Tag.Descriptions,
            IsActive = command.Tag.IsActive,
        };

        ImageTag created = await this.imageTagRepository.CreateAsync(normalizedTag, cancellationToken);
        return ApplicationResult<ImageTag>.Success(created);
    }
}
