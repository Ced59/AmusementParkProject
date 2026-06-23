using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Handlers;

public sealed class ApplyImageWatermarkCommandHandler : ICommandHandler<ApplyImageWatermarkCommand, ApplicationResult<Image>>
{
    private readonly IImageRepository imageRepository;
    private readonly IImageBinaryStorage imageBinaryStorage;

    public ApplyImageWatermarkCommandHandler(
        IImageRepository imageRepository,
        IImageBinaryStorage imageBinaryStorage)
    {
        this.imageRepository = imageRepository;
        this.imageBinaryStorage = imageBinaryStorage;
    }

    public async Task<ApplicationResult<Image>> HandleAsync(ApplyImageWatermarkCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ImageId))
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotExists());
        }

        try
        {
            string imageId = command.ImageId.Trim();
            Image? image = await this.imageRepository.GetByIdAsync(imageId, cancellationToken);
            if (image is null)
            {
                return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotExists());
            }

            if (image.Category == ImageCategory.Logo)
            {
                return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageWatermarkNotAllowed());
            }

            if (image.IsWatermarked)
            {
                return ApplicationResult<Image>.Success(image);
            }

            if (string.IsNullOrWhiteSpace(image.Path))
            {
                return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageBinaryNotFound());
            }

            bool watermarked = await this.imageBinaryStorage.ApplyWatermarkAsync(image.Path, cancellationToken);
            if (!watermarked)
            {
                return ApplicationResult<Image>.Failure(ImageApplicationErrors.ErrorApplyingWatermark());
            }

            Image? updated = await this.imageRepository.MarkWatermarkedAsync(image.Id, cancellationToken);
            if (updated is null)
            {
                return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotExists());
            }

            return ApplicationResult<Image>.Success(updated);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.ErrorApplyingWatermark());
        }
    }
}
