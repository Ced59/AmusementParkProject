using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
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
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItems = new Mock<IParkItemRepository>(MockBehavior.Strict);
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
        parks.Setup(repository => repository.GetByIdAsync(
                "park-1",
                true,
                CancellationToken.None))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Parc test" });
        parkItems.Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "item-1" })),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new ParkItem { Id = "item-1", ParkId = "park-1", Name = "Attraction test" },
            });
        GetPassportParkStatisticsQueryHandler handler =
            new GetPassportParkStatisticsQueryHandler(
                reader.Object,
                parks.Object,
                parkItems.Object);

        ApplicationResult<PassportParkStatisticsResult> result = await handler.HandleAsync(
            new GetPassportParkStatisticsQuery(" owner-1 ", " park-1 "));

        Assert.True(result.IsSuccess);
        PassportParkStatisticsResult value = Assert.IsType<PassportParkStatisticsResult>(
            result.Value);
        Assert.Equal("park-1", value.ParkId);
        Assert.Equal("Parc test", value.ParkName);
        Assert.Equal(1, value.Summary.VisitCount);
        Assert.Equal(4d, value.Summary.HistoricalParkRatings?.Average);
        Assert.Equal(4.5d, value.CurrentGlobalRating);
        Assert.Equal(0.5d, value.CurrentGlobalMinusHistoricalAverage);
        Assert.Equal("item-1", Assert.Single(value.CurrentTopItems).ParkItemId);
        Assert.Equal("Attraction test", Assert.Single(value.CurrentTopItems).ParkItemName);
        Assert.Equal(5d, Assert.Single(value.HistoricalTopItems).Average);
        Assert.Equal("Attraction test", Assert.Single(value.HistoricalTopItems).ParkItemName);
        reader.VerifyAll();
        parks.VerifyAll();
        parkItems.VerifyAll();
    }

    [Fact]
    public async Task ParkHandler_WithoutPrivateEvidence_ShouldNotResolveHiddenCatalogNames()
    {
        Mock<IPassportScopeStatisticsSourceReader> reader =
            new Mock<IPassportScopeStatisticsSourceReader>(MockBehavior.Strict);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItems = new Mock<IParkItemRepository>(MockBehavior.Strict);
        reader.Setup(value => value.ReadParkAsync(
                "owner-1",
                "park-1",
                CancellationToken.None))
            .ReturnsAsync(new PassportParkStatisticsSource(
                Array.Empty<PassportVisitStatisticsObservation>(),
                Array.Empty<PassportRideStatisticsObservation>(),
                null,
                Array.Empty<PassportCurrentItemRatingObservation>()));
        GetPassportParkStatisticsQueryHandler handler =
            new GetPassportParkStatisticsQueryHandler(
                reader.Object,
                parks.Object,
                parkItems.Object);

        ApplicationResult<PassportParkStatisticsResult> result = await handler.HandleAsync(
            new GetPassportParkStatisticsQuery("owner-1", "park-1"));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value?.ParkName);
        Assert.Empty(result.Value?.CurrentTopItems ?? Array.Empty<PassportCurrentItemRatingResult>());
        Assert.Empty(result.Value?.HistoricalTopItems ?? Array.Empty<PassportHistoricalItemRatingResult>());
        reader.VerifyAll();
        parks.VerifyNoOtherCalls();
        parkItems.VerifyNoOtherCalls();
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
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItems = new Mock<IParkItemRepository>(MockBehavior.Strict);
        GetPassportParkStatisticsQueryHandler handler =
            new GetPassportParkStatisticsQueryHandler(
                reader.Object,
                parks.Object,
                parkItems.Object);

        ApplicationResult<PassportParkStatisticsResult> result = await handler.HandleAsync(
            new GetPassportParkStatisticsQuery(userId, parkId));

        Assert.False(result.IsSuccess);
        Assert.Equal("identifier.required", Assert.Single(result.Errors).Code);
        reader.VerifyNoOtherCalls();
        parks.VerifyNoOtherCalls();
        parkItems.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task YearHandler_ShouldMapPerParkBreakdown()
    {
        Mock<IPassportScopeStatisticsSourceReader> reader =
            new Mock<IPassportScopeStatisticsSourceReader>(MockBehavior.Strict);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
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
        parks.Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.OrderBy(static id => id)
                    .SequenceEqual(new[] { "park-a", "park-b" })),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new Park { Id = "park-a", Name = "Parc Alpha" },
                new Park { Id = "park-b", Name = "Parc Bêta" },
            });
        GetPassportYearStatisticsQueryHandler handler =
            new GetPassportYearStatisticsQueryHandler(reader.Object, parks.Object);

        ApplicationResult<PassportYearStatisticsResult> result = await handler.HandleAsync(
            new GetPassportYearStatisticsQuery(" owner-1 ", 2025));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value?.ParkCount);
        Assert.Equal(
            new[] { "park-a", "park-b" },
            result.Value?.ByPark.Select(static item => item.ParkId));
        Assert.Equal(
            new[] { "Parc Alpha", "Parc Bêta" },
            result.Value?.ByPark.Select(static item => item.ParkName));
        reader.VerifyAll();
        parks.VerifyAll();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    public async Task YearHandler_WithInvalidYear_ShouldFailBeforeReading(int year)
    {
        Mock<IPassportScopeStatisticsSourceReader> reader =
            new Mock<IPassportScopeStatisticsSourceReader>(MockBehavior.Strict);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        GetPassportYearStatisticsQueryHandler handler =
            new GetPassportYearStatisticsQueryHandler(reader.Object, parks.Object);

        ApplicationResult<PassportYearStatisticsResult> result = await handler.HandleAsync(
            new GetPassportYearStatisticsQuery("owner-1", year));

        Assert.False(result.IsSuccess);
        Assert.Equal("visit.list-year-invalid", Assert.Single(result.Errors).Code);
        reader.VerifyNoOtherCalls();
        parks.VerifyNoOtherCalls();
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
