using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Queries;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Handlers;

public sealed class PublishSocialLinkCommandHandler
    : ICommandHandler<PublishSocialLinkCommand, ApplicationResult<SocialPublication>>
{
    private readonly ISocialPublicationComposerService service;

    public PublishSocialLinkCommandHandler(ISocialPublicationComposerService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<SocialPublication>> HandleAsync(PublishSocialLinkCommand command, CancellationToken cancellationToken = default)
    {
        return this.service.PublishAsync(command.Request, command.RequestedByUserId, cancellationToken);
    }
}

public sealed class GetSocialPublicationDraftQueryHandler
    : IQueryHandler<GetSocialPublicationDraftQuery, ApplicationResult<SocialPublicationDraft>>
{
    private readonly ISocialPublicationComposerService service;

    public GetSocialPublicationDraftQueryHandler(ISocialPublicationComposerService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<SocialPublicationDraft>> HandleAsync(
        GetSocialPublicationDraftQuery query,
        CancellationToken cancellationToken = default)
    {
        return this.service.ResolveDraftAsync(
            query.Url,
            query.ImagePage,
            query.ImagePageSize,
            cancellationToken);
    }
}

public sealed class RetrySocialPublicationCommandHandler
    : ICommandHandler<RetrySocialPublicationCommand, ApplicationResult<SocialPublication>>
{
    private readonly ISocialPublicationService service;

    public RetrySocialPublicationCommandHandler(ISocialPublicationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<SocialPublication>> HandleAsync(RetrySocialPublicationCommand command, CancellationToken cancellationToken = default)
    {
        return this.service.RetryAsync(command.PublicationId, command.RequestedByUserId, cancellationToken);
    }
}

public sealed class RetryParkAnnouncementPublicationCommandHandler
    : ICommandHandler<RetryParkAnnouncementPublicationCommand, ApplicationResult<SocialPublication>>
{
    private readonly ISocialPublicationService service;

    public RetryParkAnnouncementPublicationCommandHandler(ISocialPublicationService service)
    {
        this.service = service;
    }

    public async Task<ApplicationResult<SocialPublication>> HandleAsync(
        RetryParkAnnouncementPublicationCommand command,
        CancellationToken cancellationToken = default)
    {
        return await this.service.RetryParkAnnouncementAsync(
            command.ParkId,
            command.PublicationId,
            command.RequestedByUserId,
            cancellationToken);
    }
}

public sealed class UpdateSocialPublicationCommandHandler
    : ICommandHandler<UpdateSocialPublicationCommand, ApplicationResult<SocialPublication>>
{
    private readonly ISocialPublicationService service;

    public UpdateSocialPublicationCommandHandler(ISocialPublicationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<SocialPublication>> HandleAsync(UpdateSocialPublicationCommand command, CancellationToken cancellationToken = default)
    {
        return this.service.UpdateAsync(command.PublicationId, command.Message, command.RequestedByUserId, cancellationToken);
    }
}

public sealed class DeleteSocialPublicationCommandHandler
    : ICommandHandler<DeleteSocialPublicationCommand, ApplicationResult<SocialPublication>>
{
    private readonly ISocialPublicationService service;

    public DeleteSocialPublicationCommandHandler(ISocialPublicationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<SocialPublication>> HandleAsync(DeleteSocialPublicationCommand command, CancellationToken cancellationToken = default)
    {
        return this.service.DeleteAsync(command.PublicationId, command.RequestedByUserId, cancellationToken);
    }
}

public sealed class SynchronizeSocialPublicationsCommandHandler
    : ICommandHandler<SynchronizeSocialPublicationsCommand, SocialPublicationSynchronizationResult>
{
    private readonly ISocialPublicationService service;

    public SynchronizeSocialPublicationsCommandHandler(ISocialPublicationService service)
    {
        this.service = service;
    }

    public Task<SocialPublicationSynchronizationResult> HandleAsync(SynchronizeSocialPublicationsCommand command, CancellationToken cancellationToken = default)
    {
        return this.service.SynchronizeAsync(command.Limit, cancellationToken);
    }
}

public sealed class RefreshParkAnnouncementPreviewCommandHandler
    : ICommandHandler<RefreshParkAnnouncementPreviewCommand, ApplicationResult<SocialPublication>>
{
    private readonly ISocialPublicationService service;

    public RefreshParkAnnouncementPreviewCommandHandler(ISocialPublicationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<SocialPublication>> HandleAsync(
        RefreshParkAnnouncementPreviewCommand command,
        CancellationToken cancellationToken = default)
    {
        return this.service.RefreshParkAnnouncementPreviewAsync(
            command.ParkId,
            command.RequestedByUserId,
            cancellationToken);
    }
}

public sealed class GetSocialPublishingOverviewQueryHandler
    : IQueryHandler<GetSocialPublishingOverviewQuery, SocialPublishingOverview>
{
    private readonly ISocialPublicationRepository repository;
    private readonly IReadOnlyCollection<ISocialPublisher> publishers;

    public GetSocialPublishingOverviewQueryHandler(
        ISocialPublicationRepository repository,
        IEnumerable<ISocialPublisher> publishers)
    {
        this.repository = repository;
        this.publishers = publishers.ToList();
    }

    public async Task<SocialPublishingOverview> HandleAsync(GetSocialPublishingOverviewQuery query, CancellationToken cancellationToken = default)
    {
        int limit = Math.Clamp(query.Limit, 1, 100);
        IReadOnlyCollection<SocialPublication> publications = await this.repository.ListRecentAsync(limit, cancellationToken);
        IReadOnlyCollection<SocialPublisherDescriptor> descriptors = this.publishers
            .Select(static publisher => publisher.Describe())
            .OrderBy(static descriptor => descriptor.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new SocialPublishingOverview(descriptors, publications);
    }
}

public sealed class ListPublishedParkAnnouncementIdsQueryHandler
    : IQueryHandler<ListPublishedParkAnnouncementIdsQuery, IReadOnlyCollection<string>>
{
    private readonly ISocialPublicationRepository repository;

    public ListPublishedParkAnnouncementIdsQueryHandler(ISocialPublicationRepository repository)
    {
        this.repository = repository;
    }

    public Task<IReadOnlyCollection<string>> HandleAsync(
        ListPublishedParkAnnouncementIdsQuery query,
        CancellationToken cancellationToken = default)
    {
        return this.repository.ListPublishedAutomaticParkAnnouncementParkIdsAsync(cancellationToken);
    }
}
