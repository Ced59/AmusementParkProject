using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Ports;

public interface ISocialPublisher
{
    SocialNetwork Network { get; }

    SocialPublisherDescriptor Describe();

    Task<SocialPublisherResult> PublishLinkAsync(SocialPublisherRequest request, CancellationToken cancellationToken);

    Task<SocialPublisherLinkReconciliationResult> ReconcilePublishedLinkAsync(
        SocialPublisherLinkReconciliationRequest request,
        CancellationToken cancellationToken);

    Task<SocialPublisherOperationResult> RefreshLinkPreviewAsync(
        string url,
        CancellationToken cancellationToken);

    Task<SocialPublisherOperationResult> UpdatePostAsync(
        string externalPostId,
        string message,
        CancellationToken cancellationToken);

    Task<SocialPublisherOperationResult> DeletePostAsync(
        string externalPostId,
        CancellationToken cancellationToken);

    Task<SocialPublisherPostSnapshotResult> GetPostAsync(
        string externalPostId,
        CancellationToken cancellationToken);
}
