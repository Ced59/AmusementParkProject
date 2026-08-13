using System.Globalization;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Services;

public sealed class SocialPublicationComposerService : ISocialPublicationComposerService
{
    public const string FacebookImageQueryParameter = "facebook-image";

    private const int MaximumImagePageSize = 24;

    private readonly ISocialPublicationService socialPublicationService;
    private readonly SocialPublicationTargetResolver targetResolver;
    private readonly IImageRepository imageRepository;

    public SocialPublicationComposerService(
        ISocialPublicationService socialPublicationService,
        SocialPublicationTargetResolver targetResolver,
        IImageRepository imageRepository)
    {
        this.socialPublicationService = socialPublicationService;
        this.targetResolver = targetResolver;
        this.imageRepository = imageRepository;
    }

    public async Task<ApplicationResult<SocialPublicationDraft>> ResolveDraftAsync(
        string? url,
        int imagePage,
        int imagePageSize,
        CancellationToken cancellationToken)
    {
        ResolvedSocialPublicationTarget? target = await this.targetResolver.ResolveAsync(url, cancellationToken);
        if (target is null)
        {
            return ApplicationResult<SocialPublicationDraft>.Failure(SocialPublishingApplicationErrors.InvalidUrl());
        }

        int normalizedPage = Math.Max(1, imagePage);
        int normalizedPageSize = Math.Clamp(imagePageSize, 1, MaximumImagePageSize);
        PagedResult<SocialPublicationImageOption> images = await this.GetEligibleImagesAsync(
            target,
            normalizedPage,
            normalizedPageSize,
            cancellationToken);
        SocialPublication? parkAnnouncement = target.Kind == SocialPublicationTargetKind.Park
            && target.Park?.Id is not null
                ? await this.socialPublicationService.GetParkAnnouncementAsync(target.Park.Id, cancellationToken)
                : null;
        bool hasPublishedParkAnnouncement = parkAnnouncement?.Status == SocialPublicationStatus.Published
            && !string.IsNullOrWhiteSpace(parkAnnouncement.ExternalPostId);
        SocialPublicationDraft draft = new SocialPublicationDraft(
            target.Url.AbsoluteUri,
            SocialPublicationMessageBuilder.BuildDefaultMessage(target),
            target.Kind,
            target.FrenchName,
            target.ImageOwnerType,
            target.ImageOwnerId,
            images,
            hasPublishedParkAnnouncement,
            parkAnnouncement?.Status,
            parkAnnouncement?.ExternalPostUrl);
        return ApplicationResult<SocialPublicationDraft>.Success(draft);
    }

    public async Task<ApplicationResult<SocialPublication>> PublishAsync(
        SocialLinkPublicationRequest request,
        string? requestedByUserId,
        CancellationToken cancellationToken)
    {
        if (request.Network != SocialNetwork.Facebook)
        {
            return ApplicationResult<SocialPublication>.Failure(SocialPublishingApplicationErrors.InvalidNetwork());
        }

        ResolvedSocialPublicationTarget? target = await this.targetResolver.ResolveAsync(request.Url, cancellationToken);
        if (target is null)
        {
            return ApplicationResult<SocialPublication>.Failure(SocialPublishingApplicationErrors.InvalidUrl());
        }

        string publicationUrl = target.Url.AbsoluteUri;
        string? previewImageId = NormalizeOptional(request.PreviewImageId);
        if (previewImageId is not null)
        {
            bool isEligible = await this.IsEligibleImageAsync(target, previewImageId, cancellationToken);
            if (!isEligible)
            {
                return ApplicationResult<SocialPublication>.Failure(SocialPublishingApplicationErrors.InvalidPreviewImage());
            }

            publicationUrl = AddQueryParameter(target.Url, FacebookImageQueryParameter, previewImageId);
        }

        string? customMessage = NormalizeOptional(request.Message);
        string message = customMessage ?? SocialPublicationMessageBuilder.BuildDefaultMessage(target);
        if (target.Kind == SocialPublicationTargetKind.Park
            && target.Park is not null
            && customMessage is null
            && previewImageId is null)
        {
            SocialPublication? parkAnnouncement = await this.socialPublicationService.PublishParkAnnouncementAsync(
                target.Park,
                requestedByUserId,
                cancellationToken);
            return parkAnnouncement is null
                ? ApplicationResult<SocialPublication>.Failure(SocialPublishingApplicationErrors.InvalidUrl())
                : ApplicationResult<SocialPublication>.Success(parkAnnouncement);
        }

        return await this.socialPublicationService.PublishManualAsync(
            new SocialLinkPublicationRequest(
                SocialNetwork.Facebook,
                message,
                publicationUrl),
            requestedByUserId,
            cancellationToken);
    }

    private async Task<PagedResult<SocialPublicationImageOption>> GetEligibleImagesAsync(
        ResolvedSocialPublicationTarget target,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (target.ImageOwnerType is null
            || target.ImageOwnerId is null
            || target.ImageCategory is null)
        {
            return new PagedResult<SocialPublicationImageOption>(
                Array.Empty<SocialPublicationImageOption>(),
                page,
                pageSize,
                0);
        }

        PagedResult<Image> ownerImages = await this.imageRepository.GetPageAsync(
            page,
            pageSize,
            new ImageSearchCriteria(
                Category: target.ImageCategory.Value,
                OwnerType: target.ImageOwnerType.Value,
                OwnerId: target.ImageOwnerId,
                IsPublished: true,
                SortBy: "updated",
                SortDirection: "desc"),
            cancellationToken);
        List<SocialPublicationImageOption> pageItems = ownerImages.Items
            .Where(image => IsEligibleImage(target, image))
            .Select((image, index) => new SocialPublicationImageOption(
                image.Id,
                ResolveImageLabel(image, target.LanguageCode, ((page - 1) * pageSize) + index + 1),
                image.IsCurrent,
                image.Width,
                image.Height))
            .ToList();
        return new PagedResult<SocialPublicationImageOption>(pageItems, page, pageSize, ownerImages.TotalItems);
    }

    private async Task<bool> IsEligibleImageAsync(
        ResolvedSocialPublicationTarget target,
        string imageId,
        CancellationToken cancellationToken)
    {
        Image? image = await this.imageRepository.GetByIdAsync(imageId, cancellationToken);
        return image is not null && IsEligibleImage(target, image);
    }

    private static bool IsEligibleImage(ResolvedSocialPublicationTarget target, Image image)
    {
        return target.ImageOwnerType is not null
            && target.ImageOwnerId is not null
            && target.ImageCategory is not null
            && image.IsPublished
            && !string.IsNullOrWhiteSpace(image.Id)
            && image.OwnerType == target.ImageOwnerType.Value
            && string.Equals(image.OwnerId, target.ImageOwnerId, StringComparison.Ordinal)
            && image.Category == target.ImageCategory.Value;
    }

    private static string AddQueryParameter(Uri url, string name, string value)
    {
        UriBuilder builder = new UriBuilder(url);
        string encodedPair = $"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}";
        builder.Query = encodedPair;
        return builder.Uri.AbsoluteUri;
    }

    private static string ResolveImageLabel(Image image, string languageCode, int position)
    {
        string? caption = image.Captions
            .FirstOrDefault(value => string.Equals(value.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
            ?.Value;
        caption ??= image.Captions
            .FirstOrDefault(value => string.Equals(value.LanguageCode, "fr", StringComparison.OrdinalIgnoreCase))
            ?.Value;
        caption ??= image.Captions.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value.Value))?.Value;
        return NormalizeOptional(caption)
            ?? NormalizeOptional(image.Description)
            ?? NormalizeOptional(image.OriginalFileName)
            ?? $"Image {position.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string? NormalizeOptional(string? value)
    {
        string? normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
