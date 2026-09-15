using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Queries;

public sealed record ListMyWatchSubscriptionsQuery(
    string UserId,
    CollectionTargetType? TargetType,
    string? TargetId)
    : IQuery<ApplicationResult<IReadOnlyCollection<WatchSubscriptionResult>>>;
