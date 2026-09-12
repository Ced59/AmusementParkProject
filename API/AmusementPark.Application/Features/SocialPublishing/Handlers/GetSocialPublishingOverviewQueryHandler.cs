using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Queries;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Handlers;

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

