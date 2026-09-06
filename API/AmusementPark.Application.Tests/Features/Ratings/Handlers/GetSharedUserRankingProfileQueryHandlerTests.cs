using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Handlers;

public sealed class GetSharedUserRankingProfileQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSourceChangesDuringStatisticsRead_ShouldExposeNothing()
    {
        DateTime publishedAtUtc = new DateTime(2026, 9, 6, 18, 0, 0, DateTimeKind.Utc);
        ResolvedSharePublicationResult publication = new ResolvedSharePublicationResult(
            "owner-1",
            "Coaster Fan",
            SharePublicationType.PersonalRanking,
            ShareContentPolicy.Create(
                SharePublicationType.PersonalRanking,
                ShareDatePrecision.Hidden,
                new[] { ShareContentField.GlobalRatings }),
            publishedAtUtc,
            "personal-ranking:owner-1",
            7,
            1);
        MockSequence sequence = new MockSequence();
        Mock<ISharePublicationAccessResolver> accessResolver =
            new Mock<ISharePublicationAccessResolver>(MockBehavior.Strict);
        accessResolver.InSequence(sequence)
            .Setup(value => value.ResolveAsync(
                "share-1",
                SharePublicationType.PersonalRanking,
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ResolvedSharePublicationResult>.Success(publication));
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings.InSequence(sequence)
            .Setup(value => value.GetVisibleUserRatingStatsAsync(
                "owner-1",
                CancellationToken.None))
            .ReturnsAsync(new UserRatingStatsResult(
                1,
                4.5,
                4.5,
                4.5,
                Array.Empty<UserRatingStatBucketResult>(),
                Array.Empty<UserRatingStatBucketResult>(),
                Array.Empty<UserRatingStatBucketResult>()));
        accessResolver.InSequence(sequence)
            .Setup(value => value.RevalidateAsync(
                "share-1",
                publication,
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<bool>.Failure(
                RatingApplicationErrors.SharedRankingNotFound()));
        GetSharedUserRankingProfileQueryHandler handler =
            new GetSharedUserRankingProfileQueryHandler(
                accessResolver.Object,
                ratings.Object);

        ApplicationResult<SharedUserRankingProfileResult> result = await handler.HandleAsync(
            new GetSharedUserRankingProfileQuery("share-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "rating.shared-ranking.not-found");
        accessResolver.VerifyAll();
        ratings.VerifyAll();
    }
}
