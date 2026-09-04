using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class GetPassportItemStatisticsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldSeparatePrivateHistoryFromCurrentGlobalRating()
    {
        Mock<IPassportItemStatisticsSourceReader> sourceReader =
            new Mock<IPassportItemStatisticsSourceReader>(MockBehavior.Strict);
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        sourceReader.Setup(reader => reader.ReadAsync(
                "owner-1",
                "item-1",
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                CreateObservation("occ-1", "visit-1", VisitDate.ForYear(2024), 1024, 6),
                CreateObservation("occ-2", "visit-2", VisitDate.ForDay(2025, 6, 1), 1024, 8),
                CreateObservation("occ-3", "visit-2", VisitDate.ForDay(2025, 6, 1), 2048, null),
            });
        ratings.Setup(repository => repository.GetUserRatingAsync(
                "owner-1",
                RatingTargetType.ParkItem,
                "item-1",
                CancellationToken.None))
            .ReturnsAsync(new UserRating { Value = 4.5d });
        GetPassportItemStatisticsQueryHandler handler = new GetPassportItemStatisticsQueryHandler(
            sourceReader.Object,
            ratings.Object);

        ApplicationResult<PassportItemStatisticsResult> result = await handler.HandleAsync(
            new GetPassportItemStatisticsQuery(" owner-1 ", " item-1 "));

        Assert.True(result.IsSuccess);
        PassportItemStatisticsResult value = Assert.IsType<PassportItemStatisticsResult>(result.Value);
        Assert.Equal("item-1", value.ParkItemId);
        Assert.Equal(3, value.RideCount);
        Assert.Equal(2, value.VisitCount);
        Assert.Equal(2, value.RatingCoverage.RatedRideCount);
        Assert.Equal(3, value.RatingCoverage.TotalRideCount);
        Assert.Equal(2d / 3d, value.RatingCoverage.Rate, 12);
        Assert.Equal(3.5d, value.HistoricalRatings?.Average);
        Assert.Equal(4.5d, value.CurrentGlobalRating);
        Assert.Equal(1d, value.CurrentGlobalMinusHistoricalAverage);
        Assert.Equal(VisitDatePrecision.Year, value.FirstExperience?.Date.Precision);
        Assert.Equal(VisitDatePrecision.Day, value.LastExperience?.Date.Precision);
        Assert.Equal(2, value.ByVisit.Count);
        Assert.Equal(new[] { 2024, 2025 }, value.ByYear.Select(static item => item.Year));
        Assert.Equal(2, value.RatingTimeline.Count);
        Assert.Equal("occ-1", value.RatingTimeline.First().RideOccurrenceId);
        Assert.Null(value.Trend);
        sourceReader.VerifyAll();
        ratings.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WithoutHistoryOrGlobalRating_ShouldReturnAnExplicitEmptyResult()
    {
        Mock<IPassportItemStatisticsSourceReader> sourceReader =
            new Mock<IPassportItemStatisticsSourceReader>(MockBehavior.Strict);
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        sourceReader.Setup(reader => reader.ReadAsync(
                "owner-1",
                "item-1",
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<PassportItemRideObservation>());
        ratings.Setup(repository => repository.GetUserRatingAsync(
                "owner-1",
                RatingTargetType.ParkItem,
                "item-1",
                CancellationToken.None))
            .ReturnsAsync((UserRating?)null);
        GetPassportItemStatisticsQueryHandler handler = new GetPassportItemStatisticsQueryHandler(
            sourceReader.Object,
            ratings.Object);

        ApplicationResult<PassportItemStatisticsResult> result = await handler.HandleAsync(
            new GetPassportItemStatisticsQuery("owner-1", "item-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value?.RideCount);
        Assert.Equal(0, result.Value?.RatingCoverage.TotalRideCount);
        Assert.Null(result.Value?.HistoricalRatings);
        Assert.Null(result.Value?.CurrentGlobalRating);
        Assert.Null(result.Value?.CurrentGlobalMinusHistoricalAverage);
        Assert.Empty(result.Value?.ByVisit ?? Array.Empty<PassportItemVisitStatisticsResult>());
        Assert.Empty(result.Value?.ByYear ?? Array.Empty<PassportItemYearStatisticsResult>());
        Assert.Empty(result.Value?.RatingTimeline ?? Array.Empty<PassportItemRatingPointResult>());
        Assert.Null(result.Value?.Trend);
        sourceReader.VerifyAll();
        ratings.VerifyAll();
    }

    [Theory]
    [InlineData("", "item-1")]
    [InlineData("owner-1", " ")]
    public async Task HandleAsync_WithInvalidScope_ShouldFailBeforeReading(
        string userId,
        string parkItemId)
    {
        Mock<IPassportItemStatisticsSourceReader> sourceReader =
            new Mock<IPassportItemStatisticsSourceReader>(MockBehavior.Strict);
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        GetPassportItemStatisticsQueryHandler handler = new GetPassportItemStatisticsQueryHandler(
            sourceReader.Object,
            ratings.Object);

        ApplicationResult<PassportItemStatisticsResult> result = await handler.HandleAsync(
            new GetPassportItemStatisticsQuery(userId, parkItemId));

        Assert.False(result.IsSuccess);
        Assert.Equal("identifier.required", Assert.Single(result.Errors).Code);
        sourceReader.VerifyNoOtherCalls();
        ratings.VerifyNoOtherCalls();
    }

    private static PassportItemRideObservation CreateObservation(
        string occurrenceId,
        string visitId,
        VisitDate date,
        long sortPosition,
        byte? halfSteps)
    {
        return new PassportItemRideObservation(
            occurrenceId,
            visitId,
            date,
            sortPosition,
            halfSteps.HasValue ? RatingValue.FromHalfSteps(halfSteps.Value) : null);
    }
}
