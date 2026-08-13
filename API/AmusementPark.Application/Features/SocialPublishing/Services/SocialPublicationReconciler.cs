using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.SocialPublishing.Services;

public sealed record SocialPublicationRecovery(
    bool IsRecovered,
    SocialPublication? Publication);

public sealed class SocialPublicationReconciler
{
    private readonly ISocialPublicationRepository repository;
    private readonly ILogger<SocialPublicationReconciler> logger;

    public SocialPublicationReconciler(
        ISocialPublicationRepository repository,
        ILogger<SocialPublicationReconciler> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }

    public async Task<ApplicationResult<SocialPublicationRecovery>> RecoverFailedAsync(
        ISocialPublisher publisher,
        SocialPublication publication,
        string? requestedByUserId,
        CancellationToken cancellationToken)
    {
        SocialPublisherLinkReconciliationResult reconciliation = await this.TryReconcileAsync(
            publisher,
            publication,
            cancellationToken);
        if (!reconciliation.IsSuccess)
        {
            return ApplicationResult<SocialPublicationRecovery>.Failure(
                SocialPublishingApplicationErrors.PublisherOperationFailed(reconciliation.FailureMessage));
        }

        if (reconciliation.IsAmbiguous)
        {
            return ApplicationResult<SocialPublicationRecovery>.Failure(
                SocialPublishingApplicationErrors.PublicationReconciliationAmbiguous());
        }

        if (!reconciliation.IsFound)
        {
            return ApplicationResult<SocialPublicationRecovery>.Success(
                new SocialPublicationRecovery(false, null));
        }

        publication.RequestedByUserId = requestedByUserId;
        SocialPublication recovered = await this.MarkAsPublishedAsync(
            publication,
            reconciliation,
            cancellationToken);
        return ApplicationResult<SocialPublicationRecovery>.Success(
            new SocialPublicationRecovery(true, recovered));
    }

    public async Task<SocialPublication?> RecoverLostResponseAsync(
        ISocialPublisher publisher,
        SocialPublication publication,
        Exception exception,
        CancellationToken cancellationToken)
    {
        this.logger.LogWarning(
            exception,
            "Social publication {PublicationId} lost the publisher response; attempting reconciliation.",
            publication.Id);
        if (publication.Trigger != SocialPublicationTrigger.AutomaticParkPublication)
        {
            return null;
        }

        SocialPublisherLinkReconciliationResult reconciliation = await this.TryReconcileAsync(
            publisher,
            publication,
            cancellationToken);
        return reconciliation.IsSuccess && reconciliation.IsFound && !reconciliation.IsAmbiguous
            ? await this.MarkAsPublishedAsync(publication, reconciliation, cancellationToken)
            : null;
    }

    private async Task<SocialPublisherLinkReconciliationResult> TryReconcileAsync(
        ISocialPublisher publisher,
        SocialPublication publication,
        CancellationToken cancellationToken)
    {
        try
        {
            return await publisher.ReconcilePublishedLinkAsync(
                new SocialPublisherLinkReconciliationRequest(
                    publication.Message,
                    publication.AttemptedAtUtc ?? publication.RequestedAtUtc),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(
                exception,
                "Social publication {PublicationId} could not be reconciled with the publisher.",
                publication.Id);
            return new SocialPublisherLinkReconciliationResult(
                false,
                false,
                false,
                null,
                null,
                "publisher-reconciliation-unavailable",
                "Facebook n'a pas pu confirmer si la publication existe déjà.");
        }
    }

    private async Task<SocialPublication> MarkAsPublishedAsync(
        SocialPublication publication,
        SocialPublisherLinkReconciliationResult reconciliation,
        CancellationToken cancellationToken)
    {
        publication.Status = SocialPublicationStatus.Published;
        publication.PublishedAtUtc = publication.AttemptedAtUtc ?? DateTime.UtcNow;
        publication.LastSynchronizedAtUtc = DateTime.UtcNow;
        publication.ExternalPostId = reconciliation.ExternalPostId;
        publication.ExternalPostUrl = reconciliation.ExternalPostUrl;
        publication.FailureCode = null;
        publication.FailureMessage = null;
        publication.Touch();
        return await this.repository.UpdateAsync(publication, cancellationToken);
    }
}
