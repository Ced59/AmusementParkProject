using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Queries;

public sealed record ListMyUserCollectionEntriesQuery(
    string UserId,
    CollectionTargetType? TargetType = null,
    string? TargetId = null)
    : IQuery<ApplicationResult<IReadOnlyCollection<UserCollectionEntryResult>>>;
