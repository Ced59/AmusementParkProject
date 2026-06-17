using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Results;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler d'upload d'image.
/// </summary>
public sealed class UploadImageCommandHandler : ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>
{
    private readonly IImageRepository imageRepository;
    private readonly IImageProcessingPipeline imageProcessingPipeline;
    private readonly IImageBinaryStorage imageBinaryStorage;

    public UploadImageCommandHandler(
        IImageRepository imageRepository,
        IImageProcessingPipeline imageProcessingPipeline,
        IImageBinaryStorage imageBinaryStorage)
    {
        this.imageRepository = imageRepository;
        this.imageProcessingPipeline = imageProcessingPipeline;
        this.imageBinaryStorage = imageBinaryStorage;
    }

    public async Task<ApplicationResult<UploadedImageResult>> HandleAsync(UploadImageCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Request is null)
        {
            return ApplicationResult<UploadedImageResult>.Failure(ApplicationErrors.Required(nameof(command.Request)));
        }

        if (command.Request.File is null || command.Request.File.Content == Stream.Null || string.IsNullOrWhiteSpace(command.Request.File.FileName))
        {
            return ApplicationResult<UploadedImageResult>.Failure(ImageApplicationErrors.NoImageFileProvided());
        }

        try
        {
            ImageProcessingMetadata? metadata = await this.imageProcessingPipeline.ExtractMetadataAsync(command.Request, cancellationToken);

            if (command.Request.File.Content.CanSeek)
            {
                command.Request.File.Content.Position = 0;
            }

            string imageId = Guid.NewGuid().ToString("N");
            string categoryPathSegment = ToPathSegment(command.Request.Category);
            string storagePath = $"{categoryPathSegment}/{imageId}";

            IReadOnlyCollection<string> savedFiles = await this.imageBinaryStorage.SaveAsync(
                storagePath,
                command.Request.File,
                command.Request.WithWatermark,
                cancellationToken);

            ImageUploadRequest preparedRequest = new ImageUploadRequest
            {
                ImageId = imageId,
                Category = command.Request.Category,
                File = command.Request.File,
                Description = command.Request.Description,
                WithWatermark = command.Request.WithWatermark,
                OwnerType = command.Request.OwnerType,
                OwnerId = string.IsNullOrWhiteSpace(command.Request.OwnerId) ? null : command.Request.OwnerId.Trim(),
                StoragePath = storagePath,
                Width = metadata?.Width ?? 0,
                Height = metadata?.Height ?? 0,
                SizeInBytes = metadata?.SizeInBytes ?? command.Request.File.Length,
                GeoLocation = metadata?.GeoLocation,
                ExifMetadata = metadata?.ExifMetadata,
            };

            Image image = await this.imageRepository.CreateAsync(preparedRequest, cancellationToken);

            return ApplicationResult<UploadedImageResult>.Success(new UploadedImageResult
            {
                Image = image,
                SavedFiles = savedFiles,
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<UploadedImageResult>.Failure(ImageApplicationErrors.ImageProcessingFailed());
        }
    }

    private static string ToPathSegment(ImageCategory category)
    {
        return category switch
        {
            ImageCategory.Avatar => "avatar",
            ImageCategory.ParkLogo => "park_logo",
            ImageCategory.Park => "park",
            ImageCategory.Attraction => "attraction",
            ImageCategory.Operator => "operator",
            ImageCategory.Manufacturer => "manufacturer",
            ImageCategory.Founder => "founder",
            ImageCategory.VideoThumbnail => "video_thumbnail",
            _ => "image",
        };
    }
}
