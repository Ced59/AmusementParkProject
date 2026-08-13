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

public interface ISocialWebhookHandler
{
    SocialNetwork Network { get; }

    bool IsEnabled { get; }

    bool VerifySubscriptionToken(string? verifyToken);

    bool VerifySignature(string payload, string? signature);

    IReadOnlyCollection<SocialWebhookChange> ParseChanges(string payload);
}

public interface ISocialPublicationRepository
{
    Task<SocialPublication> CreateAsync(SocialPublication publication, CancellationToken cancellationToken);

    Task<SocialPublication> UpdateAsync(SocialPublication publication, CancellationToken cancellationToken);

    Task<SocialPublication?> TryClaimFailedForRetryAsync(
        string publicationId,
        DateTime expectedUpdatedAtUtc,
        string? requestedByUserId,
        CancellationToken cancellationToken);

    Task<SocialPublication?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<SocialPublication?> GetByDeduplicationKeyAsync(string deduplicationKey, CancellationToken cancellationToken);

    Task<SocialPublication?> GetByExternalPostIdAsync(string externalPostId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SocialPublication>> ListRecentAsync(int limit, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> ListPublishedAutomaticParkAnnouncementParkIdsAsync(
        CancellationToken cancellationToken);
}
