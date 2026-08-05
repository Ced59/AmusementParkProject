using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Ports;

public interface ISocialPublisher
{
    SocialNetwork Network { get; }

    SocialPublisherDescriptor Describe();

    Task<SocialPublisherResult> PublishLinkAsync(SocialPublisherRequest request, CancellationToken cancellationToken);
}

public interface ISocialPublicationRepository
{
    Task<SocialPublication> CreateAsync(SocialPublication publication, CancellationToken cancellationToken);

    Task<SocialPublication> UpdateAsync(SocialPublication publication, CancellationToken cancellationToken);

    Task<SocialPublication?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<SocialPublication?> GetByDeduplicationKeyAsync(string deduplicationKey, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SocialPublication>> ListRecentAsync(int limit, CancellationToken cancellationToken);
}
