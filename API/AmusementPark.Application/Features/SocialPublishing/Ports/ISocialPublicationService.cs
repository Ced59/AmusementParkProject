using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Ports;

public interface ISocialPublicationService
{
    Task<ApplicationResult<SocialPublication>> PublishManualAsync(
        SocialLinkPublicationRequest request,
        string? requestedByUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SocialPublication>> RetryAsync(
        string publicationId,
        string? requestedByUserId,
        CancellationToken cancellationToken);

    Task<SocialPublication?> PublishParkAnnouncementAsync(
        Park park,
        string? requestedByUserId,
        CancellationToken cancellationToken);
}
