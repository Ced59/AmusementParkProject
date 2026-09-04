using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class GetPassportScopeStatisticsQueryHandlersTests
{
    [Fact]
    public async Task ParkHandler_ShouldNormalizeScopeAndMapCurrentAndHistoricalEvidence()
    {
        Mock<IPassportScopeStatisticsSourceReader> reader =
            new Mock<IPassportScopeStatisticsSourceReader>(MockBehavior.Strict);
        reader.Setup(value => value.ReadParkAsync(
                "owner-1",
                "park-1",
                CancellationToken.None))
            .ReturnsAsync(new PassportParkStatisticsSource(
                new[] { Visit("visit-1", "park-1", 2025, 8) },
                new[] { Ride("occ-1", "visit-1", "park-1", "item-1", 2025, 10) },
                RatingValue.FromHalfSteps(9),
                new[]
                {
                    new PassportCurrentItemRatingObservation(
                        "item-1",
                        RatingValue.FromHalfSteps(8)),
                }));
        GetPassportParkStatisticsQueryHandler handler =
            new GetPassportParkStatisticsQueryHandler(reader.Object);

        ApplicationResult<PassportParkStatisticsResult> result = await handler.HandleAsync(
            new GetPassportParkStatisticsQuery(" owner-1 ", " park-1 "));

        Assert.True(result.IsSuccess);
        PassportParkStatisticsResult value = Assert.IsType<PassportParkStatisticsResult>(
            result.Value);
        Assert.Equal("park-1", value.ParkId);
        Assert.Equal(1, value.Summary.VisitCount);
        Assert.Equal(4d, value.Summary.HistoricalParkRatings?.Average);
        Assert.Equal(4.5d, value.CurrentGlobalRating);
        Assert.Equal(0.5d, value.CurrentGlobalMinusHistoricalAverage);
        Assert.Equal("item-1", Assert.Single(value.CurrentTopItems).ParkItemId);
        Assert.Equal(5d, Assert.Single(value.HistoricalTopItems).Average);
        reader.VerifyAll();
    }

    [Theory]
    [InlineData("", "park-1")]
    [InlineData("owner-1", " ")]
    public async Task ParkHandler_WithInvalidScope_ShouldFailBeforeReading(
        string userId,
        string parkId)
    {
        Mock<IPassportScopeStatisticsSourceReader> reader =
            new Mock<IPassportScopeStatisticsSourceReader>(MockBehavior.Strict);
        GetPassportParkStatisticsQueryHandler handler =
            new GetPassportParkStatisticsQueryHandler(reader.Object);

        ApplicationResult<PassportParkStatisticsResult> result = await handler.HandleAsync(
            new GetPassportParkStatisticsQuery(userId, parkId));

        Assert.False(result.IsSuccess);
        Assert.Equal("identifier.required", Assert.Single(result.Errors).Code);
        reader.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task YearHandler_ShouldMapPerParkBreakdown()
    {
        Mock<IPassportScopeStatisticsSourceReader> reader =
            new Mock<IPassportScopeStatisticsSourceReader>(MockBehavior.Strict);
        reader.Setup(value => value.ReadYearAsync(
                "owner-1",
                2025,
                CancellationToken.None))
            .ReturnsAsync(new PassportYearStatisticsSource(
                new[]
                {
                    Visit("visit-a", "park-a", 2025, null),
                    Visit("visit-b", "park-b", 2025, null),
                },
                Array.Empty<PassportRideStatisticsObservation>()));
        GetPassportYearStatisticsQueryHandler handler =
            new GetPassportYearStatisticsQueryHandler(reader.Object);

        ApplicationResult<PassportYearStatisticsResult> result = await handler.HandleAsync(
            new GetPassportYearStatisticsQuery(" owner-1 ", 2025));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value?.ParkCount);
        Assert.Equal(
            new[] { "park-a", "park-b" },
            result.Value?.ByPark.Select(static item => item.ParkId));
        reader.VerifyAll();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    public async Task YearHandler_WithInvalidYear_ShouldFailBeforeReading(int year)
    {
        Mock<IPassportScopeStatisticsSourceReader> reader =
            new Mock<IPassportScopeStatisticsSourceReader>(MockBehavior.Strict);
        GetPassportYearStatisticsQueryHandler handler =
            new GetPassportYearStatisticsQueryHandler(reader.Object);

        ApplicationResult<PassportYearStatisticsResult> result = await handler.HandleAsync(
            new GetPassportYearStatisticsQuery("owner-1", year));

        Assert.False(result.IsSuccess);
        Assert.Equal("visit.list-year-invalid", Assert.Single(result.Errors).Code);
        reader.VerifyNoOtherCalls();
    }

    private static PassportVisitStatisticsObservation Visit(
        string visitId,
        string parkId,
        int year,
        byte? ratingHalfSteps)
    {
        return new PassportVisitStatisticsObservation(
            visitId,
            parkId,
            VisitDate.ForYear(year),
            ratingHalfSteps.HasValue
                ? RatingValue.FromHalfSteps(ratingHalfSteps.Value)
                : null);
    }

    private static PassportRideStatisticsObservation Ride(
        string occurrenceId,
        string visitId,
        string parkId,
        string itemId,
        int year,
        byte? ratingHalfSteps)
    {
        return new PassportRideStatisticsObservation(
            occurrenceId,
            visitId,
            parkId,
            itemId,
            VisitDate.ForYear(year),
            RideOccurrenceStatus.Completed,
            ratingHalfSteps.HasValue
                ? RatingValue.FromHalfSteps(ratingHalfSteps.Value)
                : null,
            "Attraction",
            "Attraction");
    }
}
