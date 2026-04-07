using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler de mise à jour de tag d'image.
/// </summary>
public sealed class UpdateImageTagCommandHandler : ICommandHandler<UpdateImageTagCommand, ApplicationResult<ImageTag>>
{
    private readonly IImageTagRepository imageTagRepository;

    public UpdateImageTagCommandHandler(IImageTagRepository imageTagRepository)
    {
        this.imageTagRepository = imageTagRepository;
    }

    public async Task<ApplicationResult<ImageTag>> HandleAsync(UpdateImageTagCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.TagId))
        {
            return ApplicationResult<ImageTag>.Failure(ImageApplicationErrors.ImageTagNotExists());
        }

        if (command.Tag is null)
        {
            return ApplicationResult<ImageTag>.Failure(ApplicationErrors.Required(nameof(command.Tag)));
        }

        string slug = (command.Tag.Slug ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug))
        {
            return ApplicationResult<ImageTag>.Failure(ApplicationErrors.Required(nameof(command.Tag.Slug)));
        }

        ImageTag? existing = await this.imageTagRepository.GetByIdAsync(command.TagId.Trim(), cancellationToken);
        if (existing is null)
        {
            return ApplicationResult<ImageTag>.Failure(ImageApplicationErrors.ImageTagNotExists());
        }

        ImageTagWriteModel normalizedTag = new ImageTagWriteModel
        {
            Slug = slug,
            Labels = command.Tag.Labels,
            Descriptions = command.Tag.Descriptions,
            IsActive = command.Tag.IsActive,
        };

        ImageTag? updated = await this.imageTagRepository.UpdateAsync(command.TagId.Trim(), normalizedTag, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<ImageTag>.Failure(ImageApplicationErrors.ImageTagNotExists());
        }

        return ApplicationResult<ImageTag>.Success(updated);
    }
}
