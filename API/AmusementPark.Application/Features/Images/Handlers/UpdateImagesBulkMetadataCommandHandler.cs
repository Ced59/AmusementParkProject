using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler d'actions de masse sur les métadonnées images.
/// </summary>
public sealed class UpdateImagesBulkMetadataCommandHandler : ICommandHandler<UpdateImagesBulkMetadataCommand, ApplicationResult<BulkAdministrationUpdateResult>>
{
    private readonly IImageRepository imageRepository;
    private readonly ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>> updateImageMetadataCommandHandler;
    private readonly IPublicSeoUpdateNotifier? publicSeoUpdateNotifier;

    public UpdateImagesBulkMetadataCommandHandler(
        IImageRepository imageRepository,
        ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>> updateImageMetadataCommandHandler,
        IPublicSeoUpdateNotifier? publicSeoUpdateNotifier = null)
    {
        this.imageRepository = imageRepository;
        this.updateImageMetadataCommandHandler = updateImageMetadataCommandHandler;
        this.publicSeoUpdateNotifier = publicSeoUpdateNotifier;
    }

    public async Task<ApplicationResult<BulkAdministrationUpdateResult>> HandleAsync(UpdateImagesBulkMetadataCommand command, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> imageIds = command.ImageIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (imageIds.Count == 0)
        {
            return ApplicationResult<BulkAdministrationUpdateResult>.Failure(ApplicationErrors.Required(nameof(command.ImageIds)));
        }

        bool hasAction = command.Metadata.IsPublished.HasValue ||
                         command.Metadata.Category.HasValue ||
                         (command.Metadata.AddTagIds?.Count > 0) ||
                         (command.Metadata.RemoveTagIds?.Count > 0);

        if (!hasAction)
        {
            return ApplicationResult<BulkAdministrationUpdateResult>.Failure(ApplicationErrors.Required("bulkImageAction"));
        }

        if (command.Metadata.Category == ImageCategory.Comment)
        {
            return ApplicationResult<BulkAdministrationUpdateResult>.Failure(
                ImageApplicationErrors.CommentImageLifecycleManaged());
        }

        IReadOnlyCollection<Image> existingImages = await this.imageRepository.GetByIdsAsync(
            imageIds,
            cancellationToken);
        if (existingImages.Any(ManagedCommentImageMutationGuard.IsManagedScope))
        {
            return ApplicationResult<BulkAdministrationUpdateResult>.Failure(
                ImageApplicationErrors.CommentImageLifecycleManaged());
        }

        int updatedCount = command.Metadata.Category.HasValue
            ? await this.UpdateOneByOneAsync(
                imageIds,
                existingImages,
                command.Metadata,
                cancellationToken)
            : await this.imageRepository.UpdateBulkMetadataAsync(imageIds, command.Metadata, cancellationToken);

        if (this.publicSeoUpdateNotifier is not null && updatedCount > 0)
        {
            IReadOnlyCollection<Image> currentImages = await this.imageRepository.GetByIdsAsync(imageIds, cancellationToken);
            await PublicImageSeoUpdateNotification.NotifyAsync(
                this.publicSeoUpdateNotifier,
                existingImages,
                currentImages,
                cancellationToken);
        }

        return ApplicationResult<BulkAdministrationUpdateResult>.Success(new BulkAdministrationUpdateResult
        {
            RequestedCount = imageIds.Count,
            UpdatedCount = updatedCount,
        });
    }

    private async Task<int> UpdateOneByOneAsync(
        IReadOnlyCollection<string> imageIds,
        IReadOnlyCollection<Image> existingImages,
        ImageBulkMetadataUpdate metadata,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, Image> imagesById = existingImages.ToDictionary(
            static image => image.Id,
            StringComparer.Ordinal);
        int updatedCount = 0;
        foreach (string imageId in imageIds)
        {
            if (!imagesById.TryGetValue(imageId, out Image? existing))
            {
                continue;
            }

            ImageMetadataUpdate update = BuildMetadataUpdate(existing, metadata);
            ApplicationResult<Image> result = await this.updateImageMetadataCommandHandler.HandleAsync(
                new UpdateImageMetadataCommand(existing.Id, update, SuppressSeoNotification: true),
                cancellationToken);

            if (!result.IsSuccess)
            {
                continue;
            }

            updatedCount++;
        }

        return updatedCount;
    }

    private static ImageMetadataUpdate BuildMetadataUpdate(Image existing, ImageBulkMetadataUpdate metadata)
    {
        return new ImageMetadataUpdate
        {
            Description = existing.Description,
            GeoLocation = existing.GeoLocation is null ? null : new GeoPointValue(existing.GeoLocation.Latitude, existing.GeoLocation.Longitude),
            AltTexts = ToLocalizedValues(existing.AltTexts),
            Captions = ToLocalizedValues(existing.Captions),
            Credits = ToLocalizedValues(existing.Credits),
            TagIds = ApplyTagPatch(existing.TagIds, metadata.AddTagIds, metadata.RemoveTagIds),
            Category = metadata.Category ?? existing.Category,
            OwnerType = existing.OwnerType,
            OwnerId = existing.OwnerId,
            IsPublished = metadata.IsPublished ?? existing.IsPublished,
            SourceUrl = existing.SourceUrl,
        };
    }

    private static IReadOnlyCollection<string> ApplyTagPatch(
        IReadOnlyCollection<string> existingTagIds,
        IReadOnlyCollection<string>? addTagIds,
        IReadOnlyCollection<string>? removeTagIds)
    {
        HashSet<string> tagIds = existingTagIds
            .Where(static tagId => !string.IsNullOrWhiteSpace(tagId))
            .Select(static tagId => tagId.Trim())
            .ToHashSet(StringComparer.Ordinal);

        foreach (string tagId in addTagIds ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(tagId))
            {
                tagIds.Add(tagId.Trim());
            }
        }

        foreach (string tagId in removeTagIds ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(tagId))
            {
                tagIds.Remove(tagId.Trim());
            }
        }

        return tagIds.ToArray();
    }

    private static IReadOnlyCollection<LocalizedTextValue> ToLocalizedValues(IEnumerable<LocalizedText> values)
    {
        List<LocalizedTextValue> result = new List<LocalizedTextValue>();
        foreach (LocalizedText value in values)
        {
            if (string.IsNullOrWhiteSpace(value.LanguageCode) || string.IsNullOrWhiteSpace(value.Value))
            {
                continue;
            }

            result.Add(new LocalizedTextValue(value.LanguageCode.Trim(), value.Value.Trim()));
        }

        return result;
    }
}
