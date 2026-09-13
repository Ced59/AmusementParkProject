using AmusementPark.WebAPI.Contracts.Common;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.SocialPublishing;

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
