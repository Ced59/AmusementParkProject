using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Results;
using AmusementPark.Application.Features.Comments;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler d'upload d'image.
/// </summary>
public sealed class UploadImageCommandHandler : ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>
{
    private const long MaximumAvatarFileSizeInBytes = 5 * 1024 * 1024;
    private const int MaximumAvatarEdge = 4096;
    private const long MaximumAvatarPixels = 8_000_000;
    private const int MaximumCommentImageEdge = 8192;
    private const long MaximumCommentImagePixels = 24_000_000;
    private static readonly HashSet<string> AllowedAvatarContentTypes = new HashSet<string>(
        new[] { "image/jpeg", "image/png", "image/webp" },
        StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> AllowedCommentImageContentTypes = new HashSet<string>(
        new[] { "image/jpeg", "image/png", "image/webp" },
        StringComparer.OrdinalIgnoreCase);
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

        if (!command.AllowManagedCommentLifecycle
            && ManagedCommentImageMutationGuard.IsManagedScope(
                command.Request.Category,
                command.Request.OwnerType))
        {
            return ApplicationResult<UploadedImageResult>.Failure(
                ImageApplicationErrors.CommentImageLifecycleManaged());
        }

        if (command.Request.File is null || command.Request.File.Content == Stream.Null || string.IsNullOrWhiteSpace(command.Request.File.FileName))
        {
            return ApplicationResult<UploadedImageResult>.Failure(ImageApplicationErrors.NoImageFileProvided());
        }

        if (command.Request.Category == ImageCategory.Avatar
            && !IsValidAvatarFile(command.Request.File))
        {
            return ApplicationResult<UploadedImageResult>.Failure(ImageApplicationErrors.AvatarUploadInvalid());
        }

        try
        {
            bool withWatermark = ShouldApplyWatermark(command.Request.Category, command.Request.WithWatermark);
            ImageProcessingMetadata? metadata = await this.imageProcessingPipeline.ExtractMetadataAsync(command.Request, cancellationToken);
            if (command.Request.Category == ImageCategory.Avatar
                && !IsValidAvatarMetadata(metadata))
            {
                return ApplicationResult<UploadedImageResult>.Failure(ImageApplicationErrors.AvatarUploadInvalid());
            }

            if (command.Request.Category == ImageCategory.Comment
                && (metadata is null
                    || string.IsNullOrWhiteSpace(metadata.DetectedContentType)
                    || !AllowedCommentImageContentTypes.Contains(metadata.DetectedContentType)
                    || metadata.FrameCount != 1))
            {
                return ApplicationResult<UploadedImageResult>.Failure(
                    CommentApplicationErrors.ImageUploadInvalid());
            }

            if (command.Request.Category == ImageCategory.Comment
                && (metadata!.Width <= 0
                    || metadata.Height <= 0
                    || metadata.Width > MaximumCommentImageEdge
                    || metadata.Height > MaximumCommentImageEdge
                    || (long)metadata.Width * metadata.Height > MaximumCommentImagePixels))
            {
                return ApplicationResult<UploadedImageResult>.Failure(
                    CommentApplicationErrors.ImageDimensionsInvalid());
            }

            if (command.Request.File.Content.CanSeek)
            {
                command.Request.File.Content.Position = 0;
            }

            string imageId = Guid.NewGuid().ToString("N");
            string categoryPathSegment = ToPathSegment(command.Request.Category);
            string storagePath = $"{categoryPathSegment}/{imageId}";
            FilePayload persistedFile = BuildPersistedFile(command.Request, metadata);

            ImageUploadRequest preparedRequest = new ImageUploadRequest
            {
                ImageId = imageId,
                Category = command.Request.Category,
                File = persistedFile,
                Description = command.Request.Description,
                WithWatermark = withWatermark,
                OwnerType = command.Request.OwnerType,
                OwnerId = string.IsNullOrWhiteSpace(command.Request.OwnerId) ? null : command.Request.OwnerId.Trim(),
                StoragePath = storagePath,
                SourceUrl = command.Request.SourceUrl,
                Width = metadata?.Width ?? 0,
                Height = metadata?.Height ?? 0,
                SizeInBytes = metadata?.SizeInBytes ?? command.Request.File.Length,
                GeoLocation = IsPrivateImageCategory(command.Request.Category) ? null : metadata?.GeoLocation,
                ExifMetadata = IsPrivateImageCategory(command.Request.Category) ? null : metadata?.ExifMetadata,
                IsPublished = command.Request.IsPublished,
            };

            if (command.Request.Category == ImageCategory.Comment)
            {
                Image draft;
                try
                {
                    draft = await this.imageRepository.CreateAsync(preparedRequest, cancellationToken);
                }
                catch
                {
                    await this.RequestCommentDraftCleanupBestEffortAsync(
                        imageId,
                        preparedRequest.OwnerId ?? string.Empty);
                    throw;
                }

                IReadOnlyCollection<string> commentFiles;
                try
                {
                    commentFiles = await this.imageBinaryStorage.SaveWithoutMetadataAsync(
                        storagePath,
                        command.Request.File,
                        withWatermark,
                        cancellationToken);
                }
                catch
                {
                    await this.RequestCommentDraftCleanupBestEffortAsync(
                        draft.Id,
                        draft.OwnerId ?? string.Empty);
                    throw;
                }

                return ApplicationResult<UploadedImageResult>.Success(new UploadedImageResult
                {
                    Image = draft,
                    SavedFiles = commentFiles,
                });
            }

            IReadOnlyCollection<string> savedFiles = command.Request.Category == ImageCategory.Avatar
                ? await this.imageBinaryStorage.SaveWithoutMetadataAsync(
                    storagePath,
                    command.Request.File,
                    withWatermark,
                    cancellationToken)
                : await this.imageBinaryStorage.SaveAsync(
                    storagePath,
                    command.Request.File,
                    withWatermark,
                    cancellationToken);
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
            return command.Request.Category == ImageCategory.Comment
                ? ApplicationResult<UploadedImageResult>.Failure(CommentApplicationErrors.ImageUploadInvalid())
                : ApplicationResult<UploadedImageResult>.Failure(ImageApplicationErrors.ImageProcessingFailed());
        }
    }

    private async Task RequestCommentDraftCleanupBestEffortAsync(string imageId, string ownerId)
    {
        try
        {
            await this.imageRepository.RequestCommentDraftCleanupAsync(
                imageId,
                ownerId,
                DateTime.UtcNow,
                CancellationToken.None);
        }
        catch
        {
            // Le brouillon reste détectable par la rétention de secours.
        }
    }

    private static bool ShouldApplyWatermark(ImageCategory category, bool requestedWithWatermark)
    {
        return requestedWithWatermark && !IsLogoCategory(category);
    }

    private static bool IsValidAvatarFile(FilePayload file)
    {
        long streamLength = file.Content.CanSeek ? file.Content.Length : file.Length;
        return file.Length > 0
            && file.Length <= MaximumAvatarFileSizeInBytes
            && streamLength > 0
            && streamLength <= MaximumAvatarFileSizeInBytes;
    }

    private static bool IsValidAvatarMetadata(ImageProcessingMetadata? metadata)
    {
        return metadata is not null
            && !string.IsNullOrWhiteSpace(metadata.DetectedContentType)
            && AllowedAvatarContentTypes.Contains(metadata.DetectedContentType)
            && metadata.FrameCount == 1
            && metadata.Width > 0
            && metadata.Height > 0
            && metadata.Width <= MaximumAvatarEdge
            && metadata.Height <= MaximumAvatarEdge
            && (long)metadata.Width * metadata.Height * metadata.FrameCount <= MaximumAvatarPixels;
    }

    private static FilePayload BuildPersistedFile(
        ImageUploadRequest request,
        ImageProcessingMetadata? metadata)
    {
        if (!IsPrivateImageCategory(request.Category)
            || string.IsNullOrWhiteSpace(metadata?.DetectedContentType))
        {
            return request.File;
        }

        return new FilePayload
        {
            FileName = request.File.FileName,
            ContentType = metadata.DetectedContentType,
            Length = request.File.Length,
            Content = request.File.Content,
        };
    }

    private static bool IsPrivateImageCategory(ImageCategory category)
    {
        return category is ImageCategory.Avatar or ImageCategory.Comment;
    }

    private static bool IsLogoCategory(ImageCategory category)
    {
        return category is ImageCategory.Logo;
    }

    private static string ToPathSegment(ImageCategory category)
    {
        return category switch
        {
            ImageCategory.Avatar => "avatar",
            ImageCategory.Logo => "logo",
            ImageCategory.Park => "park",
            ImageCategory.ParkItem => "park_item",
            ImageCategory.Operator => "operator",
            ImageCategory.Manufacturer => "manufacturer",
            ImageCategory.Founder => "founder",
            ImageCategory.VideoThumbnail => "video_thumbnail",
            ImageCategory.Comment => "comment",
            _ => "image",
        };
    }
}
