using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler de mise à jour des métadonnées d'image.
/// </summary>
public sealed class UpdateImageMetadataCommandHandler : ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>>
{
    private readonly IImageRepository imageRepository;

    public UpdateImageMetadataCommandHandler(IImageRepository imageRepository)
    {
        this.imageRepository = imageRepository;
    }

    public async Task<ApplicationResult<Image>> HandleAsync(UpdateImageMetadataCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ImageId))
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotExists());
        }

        if (command.Metadata is null)
        {
            return ApplicationResult<Image>.Failure(ApplicationErrors.Required(nameof(command.Metadata)));
        }

        try
        {
            Image? updated = await this.imageRepository.UpdateMetadataAsync(command.ImageId.Trim(), command.Metadata, cancellationToken);
            if (updated is null)
            {
                return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotExists());
            }

            return ApplicationResult<Image>.Success(updated);
        }
        catch
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageProcessingFailed());
        }
    }
}
