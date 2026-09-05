using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class GetPassportGlobalStatisticsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldResolveNamesInBatchesWithoutExposingThemAsScopeKeys()
    {
        Mock<IPassportScopeStatisticsSourceReader> reader = new(MockBehavior.Strict);
        Mock<IParkNameReadRepository> parkNames = new(MockBehavior.Strict);
        Mock<IParkItemNameReadRepository> itemNames = new(MockBehavior.Strict);
        PassportVisitStatisticsObservation visit = new(
            "visit-1",
            "park-1",
            VisitDate.ForYear(2025),
            RatingValue.FromHalfSteps(8));
        PassportRideStatisticsObservation ride = new(
            "ride-1",
            "visit-1",
            "park-1",
            "item-1",
            VisitDate.ForYear(2025),
            RideOccurrenceStatus.Completed,
            RatingValue.FromHalfSteps(10),
            "Attraction",
            "Attraction");
        reader.Setup(value => value.ReadGlobalAsync(
                "owner-1",
                2025,
                "park-1",
                CancellationToken.None))
            .ReturnsAsync(new PassportGlobalStatisticsSource(
                new[] { 2025 },
                new[] { "park-1" },
                new[] { visit },
                new[] { ride }));
        parkNames.Setup(value => value.GetNamesByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, string?> { ["park-1"] = "Parc test" });
        itemNames.Setup(value => value.GetNamesByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "item-1" })),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, string?> { ["item-1"] = "Attraction test" });
        GetPassportGlobalStatisticsQueryHandler handler = new(
            reader.Object,
            parkNames.Object,
            itemNames.Object);

        ApplicationResult<PassportGlobalStatisticsResult> result = await handler.HandleAsync(
            new GetPassportGlobalStatisticsQuery(" owner-1 ", 2025, " park-1 "));

        Assert.True(result.IsSuccess);
        PassportGlobalStatisticsResult value = Assert.IsType<PassportGlobalStatisticsResult>(result.Value);
        Assert.Equal("Parc test", Assert.Single(value.AvailableParks).ParkName);
        Assert.Equal("Parc test", Assert.Single(value.TopParks).ParkName);
        Assert.Equal("Attraction test", Assert.Single(value.TopItems).ParkItemName);
        Assert.Equal(1, value.Summary.VisitCount);
        reader.VerifyAll();
        parkNames.VerifyAll();
        itemNames.VerifyAll();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    public async Task HandleAsync_WithInvalidYear_ShouldFailBeforeReading(int year)
    {
        Mock<IPassportScopeStatisticsSourceReader> reader = new(MockBehavior.Strict);
        Mock<IParkNameReadRepository> parkNames = new(MockBehavior.Strict);
        Mock<IParkItemNameReadRepository> itemNames = new(MockBehavior.Strict);
        GetPassportGlobalStatisticsQueryHandler handler = new(
            reader.Object,
            parkNames.Object,
            itemNames.Object);

        ApplicationResult<PassportGlobalStatisticsResult> result = await handler.HandleAsync(
            new GetPassportGlobalStatisticsQuery("owner-1", year, null));

        Assert.False(result.IsSuccess);
        Assert.Equal("visit.list-year-invalid", Assert.Single(result.Errors).Code);
        reader.VerifyNoOtherCalls();
    }
}
