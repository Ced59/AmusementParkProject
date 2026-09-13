using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Ports;

public interface ISocialPublicationComposerService
{
    Task<ApplicationResult<SocialPublicationDraft>> ResolveDraftAsync(
        string? url,
        int imagePage,
        int imagePageSize,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SocialPublication>> PublishAsync(
        SocialLinkPublicationRequest request,
        string? requestedByUserId,
        CancellationToken cancellationToken);
}
