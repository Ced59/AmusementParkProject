using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Queries;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Handlers;

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

