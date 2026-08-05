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
    private readonly ISocialPublicationService service;

    public PublishSocialLinkCommandHandler(ISocialPublicationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<SocialPublication>> HandleAsync(PublishSocialLinkCommand command, CancellationToken cancellationToken = default)
    {
        return this.service.PublishManualAsync(command.Request, command.RequestedByUserId, cancellationToken);
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
