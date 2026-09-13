using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using Moq;

namespace AmusementPark.Application.Tests.Features.Ratings.Handlers;

internal sealed record PreviewHandlerFixture(
    GetSharedUserRankingPreviewQueryHandler Handler,
    Mock<IQueryHandler<GetUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>>> ParkRankings,
    Mock<IQueryHandler<GetUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>>> ParkItemRankings,
    Mock<IUserRankingSharePreviewRenderer> Renderer);
