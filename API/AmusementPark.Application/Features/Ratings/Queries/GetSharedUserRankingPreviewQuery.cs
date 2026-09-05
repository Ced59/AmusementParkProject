using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Ratings.Queries;

public sealed record GetSharedUserRankingPreviewQuery(
    string ShareId,
    ParkItemCategory? ParkItemCategory = null,
    ParkItemType? ParkItemType = null) : IQuery<ApplicationResult<UserRankingSharePreviewFileResult>>;
