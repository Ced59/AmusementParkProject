using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.WebAPI.Contracts.SocialPublishing;

namespace AmusementPark.WebAPI.Mappers;

internal static class SocialPublishingHttpMappers
{
    public static SocialLinkPublicationRequest ToApplication(this PublishSocialLinkRequestDto dto)
    {
        return new SocialLinkPublicationRequest(
            dto.Network switch
            {
                SocialNetworkDto.Facebook => SocialNetwork.Facebook,
                _ => (SocialNetwork)(-1),
            },
            dto.Message,
            dto.Url,
            dto.PreviewImageId);
    }

    public static SocialPublicationDraftDto ToHttp(this SocialPublicationDraft draft)
    {
        return new SocialPublicationDraftDto
        {
            Url = draft.Url,
            DefaultMessage = draft.DefaultMessage,
            TargetKind = draft.TargetKind.ToString(),
            TargetName = draft.TargetName,
            ImageOwnerType = draft.ImageOwnerType?.ToString(),
            ImageOwnerId = draft.ImageOwnerId,
            Images = draft.Images.ToPagedResponse(static image => new SocialPublicationImageOptionDto
            {
                Id = image.Id,
                Label = image.Label,
                IsCurrent = image.IsCurrent,
                Width = image.Width,
                Height = image.Height,
            }),
        };
    }

    public static SocialPublishingOverviewDto ToHttp(this SocialPublishingOverview overview)
    {
        return new SocialPublishingOverviewDto
        {
            Publishers = overview.Publishers.Select(static publisher => new SocialPublisherDto
            {
                Network = publisher.Network.ToString(),
                DisplayName = publisher.DisplayName,
                IsEnabled = publisher.IsEnabled,
                IsConfigured = publisher.IsConfigured,
                TargetUrl = publisher.TargetUrl,
                SupportsAutomaticParkAnnouncements = publisher.SupportsAutomaticParkAnnouncements,
            }).ToList(),
            RecentPublications = overview.RecentPublications.Select(static publication => publication.ToHttp()).ToList(),
        };
    }

    public static SocialPublicationDto ToHttp(this SocialPublication publication)
    {
        return new SocialPublicationDto
        {
            Id = publication.Id ?? string.Empty,
            Network = publication.Network.ToString(),
            Status = publication.Status.ToString(),
            Trigger = publication.Trigger.ToString(),
            Message = publication.Message,
            Url = publication.Url,
            SourceEntityType = publication.SourceEntityType,
            SourceEntityId = publication.SourceEntityId,
            RequestedAtUtc = publication.RequestedAtUtc,
            AttemptedAtUtc = publication.AttemptedAtUtc,
            PublishedAtUtc = publication.PublishedAtUtc,
            DeletedAtUtc = publication.DeletedAtUtc,
            LastSynchronizedAtUtc = publication.LastSynchronizedAtUtc,
            ExternalPostId = publication.ExternalPostId,
            ExternalPostUrl = publication.ExternalPostUrl,
            FailureCode = publication.FailureCode,
            FailureMessage = publication.FailureMessage,
        };
    }

    public static SocialPublicationSynchronizationResultDto ToHttp(this SocialPublicationSynchronizationResult result)
    {
        return new SocialPublicationSynchronizationResultDto
        {
            CheckedCount = result.CheckedCount,
            UpdatedCount = result.UpdatedCount,
            DeletedCount = result.DeletedCount,
            FailureCount = result.FailureCount,
        };
    }
}
