using AmusementPark.WebAPI.Contracts.Common;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.SocialPublishing;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SocialNetworkDto
{
    Facebook = 0,
}

public sealed class PublishSocialLinkRequestDto
{
    public SocialNetworkDto Network { get; set; } = SocialNetworkDto.Facebook;

    public string? Message { get; set; }

    public string? Url { get; set; }

    public string? PreviewImageId { get; set; }
}

public sealed class SocialPublicationDraftDto
{
    public string Url { get; set; } = string.Empty;

    public string DefaultMessage { get; set; } = string.Empty;

    public string TargetKind { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public string? ImageOwnerType { get; set; }

    public string? ImageOwnerId { get; set; }

    public bool HasPublishedParkAnnouncement { get; set; }

    public string? ParkAnnouncementId { get; set; }

    public string? ParkAnnouncementStatus { get; set; }

    public string? ParkAnnouncementExternalUrl { get; set; }

    public PagedResponseDto<SocialPublicationImageOptionDto> Images { get; set; } = new PagedResponseDto<SocialPublicationImageOptionDto>();
}

public sealed class SocialPublicationImageOptionDto
{
    public string Id { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public bool IsCurrent { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }
}

public sealed class UpdateSocialPublicationRequestDto
{
    public string? Message { get; set; }
}

public sealed class ParkSocialPreviewPublicationDto
{
    public string ParkId { get; set; } = string.Empty;
}

public sealed class SocialPublicationSynchronizationResultDto
{
    public int CheckedCount { get; set; }

    public int UpdatedCount { get; set; }

    public int DeletedCount { get; set; }

    public int FailureCount { get; set; }
}

public sealed class SocialPublishingOverviewDto
{
    public List<SocialPublisherDto> Publishers { get; set; } = new List<SocialPublisherDto>();

    public List<SocialPublicationDto> RecentPublications { get; set; } = new List<SocialPublicationDto>();
}

public sealed class SocialPublisherDto
{
    public string Network { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public bool IsConfigured { get; set; }

    public string? TargetUrl { get; set; }

    public bool SupportsAutomaticParkAnnouncements { get; set; }
}

public sealed class SocialPublicationDto
{
    public string Id { get; set; } = string.Empty;

    public string Network { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Trigger { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string? SourceEntityType { get; set; }

    public string? SourceEntityId { get; set; }

    public DateTime RequestedAtUtc { get; set; }

    public DateTime? AttemptedAtUtc { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public DateTime? LastSynchronizedAtUtc { get; set; }

    public string? ExternalPostId { get; set; }

    public string? ExternalPostUrl { get; set; }

    public string? FailureCode { get; set; }

    public string? FailureMessage { get; set; }
}
