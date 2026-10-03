using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Users.Services;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Users;

public sealed class AccountRatingDeletionServiceTests
{
    [Fact]
    public async Task DeleteAsync_ShouldReuseSafeRatingHandlerUntilNoRatingRemains()
    {
        UserRatingListItemResult rating = CreateRating();
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings.SetupSequence(value => value.GetUserRatingsAsync(
                "user-1",
                1,
                100,
                null,
                CancellationToken.None))
            .ReturnsAsync(new PagedResult<UserRatingListItemResult>(
                new[] { rating },
                1,
                100,
                1))
            .ReturnsAsync(new PagedResult<UserRatingListItemResult>(
                Array.Empty<UserRatingListItemResult>(),
                1,
                100,
                0));
        Mock<ICommandHandler<DeleteUserRatingCommand, ApplicationResult<RatingSummaryResult>>> handler =
            new Mock<ICommandHandler<DeleteUserRatingCommand, ApplicationResult<RatingSummaryResult>>>(
                MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                new DeleteUserRatingCommand("user-1", RatingTargetType.Park, "park-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<RatingSummaryResult>.Success(rating.Summary));
        AccountRatingDeletionService service = new AccountRatingDeletionService(
            ratings.Object,
            handler.Object);

        int deletedCount = await service.DeleteAsync("user-1", CancellationToken.None);

        Assert.Equal(1, deletedCount);
        ratings.VerifyAll();
        handler.VerifyAll();
    }

    private static UserRatingListItemResult CreateRating()
    {
        RatingSummaryResult summary = new RatingSummaryResult(
            RatingTargetType.Park,
            "park-1",
            1,
            4.5,
            4.1);
        return new UserRatingListItemResult(
            "rating-1",
            RatingTargetType.Park,
            "park-1",
            "Park",
            "park-1",
            "Park",
            null,
            null,
            4.5,
            DateTime.UtcNow,
            summary);
    }
}
