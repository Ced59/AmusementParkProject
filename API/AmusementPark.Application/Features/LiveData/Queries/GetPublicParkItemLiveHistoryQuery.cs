using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Results;

namespace AmusementPark.Application.Features.LiveData.Queries;

public sealed record GetPublicParkItemLiveHistoryQuery(
    string ParkItemId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Bucket) : IQuery<ApplicationResult<PublicLiveHistoryResult>>;
