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

    Task<ApplicationResult<SocialPublication>> UpdateAsync(
        string publicationId,
        string? message,
        string? requestedByUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SocialPublication>> DeleteAsync(
        string publicationId,
        string? requestedByUserId,
        CancellationToken cancellationToken);

    Task<SocialPublicationSynchronizationResult> SynchronizeAsync(
        int limit,
        CancellationToken cancellationToken);

    Task ApplyExternalChangeAsync(
        SocialNetwork network,
        SocialWebhookChange change,
        CancellationToken cancellationToken);

    Task<SocialPublication?> PublishParkAnnouncementAsync(
        Park park,
        string? requestedByUserId,
        CancellationToken cancellationToken);

    Task<SocialPublication?> GetParkAnnouncementAsync(
        string parkId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SocialPublication>> RefreshParkAnnouncementPreviewAsync(
        string parkId,
        string? requestedByUserId,
        CancellationToken cancellationToken);
}

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
