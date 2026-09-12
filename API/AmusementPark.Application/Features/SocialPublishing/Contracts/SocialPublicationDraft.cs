using AmusementPark.Application.Common.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Contracts;

public sealed record SocialPublicationDraft(
    string Url,
    string DefaultMessage,
    SocialPublicationTargetKind TargetKind,
    string TargetName,
    ImageOwnerType? ImageOwnerType,
    string? ImageOwnerId,
    PagedResult<SocialPublicationImageOption> Images,
    bool HasPublishedParkAnnouncement,
    string? ParkAnnouncementId,
    SocialPublicationStatus? ParkAnnouncementStatus,
    string? ParkAnnouncementExternalUrl);

