using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Services;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Handlers;

public sealed class RetryOwnFacebookPublicationCommandHandler
    : ICommandHandler<RetryOwnFacebookPublicationCommand, ApplicationResult<SocialPublication>>
{
    private readonly ISocialPublicationRepository repository;
    private readonly ISocialPublicationService service;
    private readonly IReadOnlyCollection<ISocialPublisher> publishers;
    private readonly SocialPublicationReconciler reconciler;

    public RetryOwnFacebookPublicationCommandHandler(
        ISocialPublicationRepository repository,
        ISocialPublicationService service,
        IEnumerable<ISocialPublisher> publishers,
        SocialPublicationReconciler reconciler)
    {
        this.repository = repository;
        this.service = service;
        this.publishers = publishers.ToArray();
        this.reconciler = reconciler;
    }

    public async Task<ApplicationResult<SocialPublication>> HandleAsync(
        RetryOwnFacebookPublicationCommand command,
        CancellationToken cancellationToken = default)
    {
        SocialPublication? publication = await this.repository.GetByIdAsync(command.PublicationId, cancellationToken);
        bool isOriginalRequester = !string.IsNullOrWhiteSpace(command.RequestedByUserId)
            && string.Equals(publication?.CreatedByUserId, command.RequestedByUserId, StringComparison.Ordinal);
        // Older records lack the immutable creator; accept only the untouched first attempt.
        bool isUnmodifiedLegacyPublication = publication?.CreatedByUserId is null
            && !string.IsNullOrWhiteSpace(command.RequestedByUserId)
            && string.Equals(publication?.RequestedByUserId, command.RequestedByUserId, StringComparison.Ordinal)
            && publication?.LastSynchronizedAtUtc is null
            && publication?.AttemptedAtUtc is DateTime attemptedAtUtc
            && attemptedAtUtc >= publication.RequestedAtUtc
            && attemptedAtUtc <= publication.RequestedAtUtc.AddMinutes(1);
        if (publication is null
            || publication.Network != SocialNetwork.Facebook
            || publication.Trigger != SocialPublicationTrigger.Manual
            || (!isOriginalRequester && !isUnmodifiedLegacyPublication))
        {
            return ApplicationResult<SocialPublication>.Failure(
                SocialPublishingApplicationErrors.PublicationNotFound(command.PublicationId));
        }
        if (publication.Status != SocialPublicationStatus.Failed)
        {
            return ApplicationResult<SocialPublication>.Failure(
                SocialPublishingApplicationErrors.PublicationCannotBeRetried());
        }
        // Exact message matching also binds the post to its destination.
        if (string.IsNullOrWhiteSpace(publication.Url)
            || !publication.Message.TrimEnd().EndsWith(publication.Url, StringComparison.Ordinal))
        {
            return ApplicationResult<SocialPublication>.Failure(
                SocialPublishingApplicationErrors.PublicationReconciliationAmbiguous());
        }

        ISocialPublisher? publisher = this.publishers.FirstOrDefault(candidate => candidate.Network == SocialNetwork.Facebook);
        if (publisher is null || !publisher.Describe().IsConfigured)
        {
            return ApplicationResult<SocialPublication>.Failure(
                SocialPublishingApplicationErrors.PublisherOperationFailed(null));
        }
        ApplicationResult<SocialPublicationRecovery> recovery = await this.reconciler.RecoverFailedAsync(
            publisher,
            publication,
            command.RequestedByUserId,
            cancellationToken);
        if (!recovery.IsSuccess || recovery.Value is null)
        {
            return ApplicationResult<SocialPublication>.Failure(recovery.Errors);
        }
        if (recovery.Value.IsRecovered && recovery.Value.Publication is not null)
        {
            return ApplicationResult<SocialPublication>.Success(recovery.Value.Publication);
        }

        return await this.service.RetryAsync(publication.Id!, command.RequestedByUserId, cancellationToken);
    }
}
