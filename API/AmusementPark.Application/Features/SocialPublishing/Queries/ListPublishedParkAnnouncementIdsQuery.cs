using AmusementPark.Application.Abstractions;

namespace AmusementPark.Application.Features.SocialPublishing.Queries;

public sealed record ListPublishedParkAnnouncementIdsQuery
    : IQuery<IReadOnlyCollection<string>>;
