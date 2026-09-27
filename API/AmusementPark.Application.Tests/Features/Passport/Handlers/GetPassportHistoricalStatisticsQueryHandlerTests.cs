using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class GetPassportHistoricalStatisticsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithCanonicalHistory_ShouldReturnNamedPrivateStatistics()
    {
        PassportVisitStatisticsObservation visit = new(
            "visit-1",
            "park-1",
            VisitDate.ForYear(2001),
            null);
        PassportRideStatisticsObservation ride = new(
            "ride-1",
            visit.VisitId,
            visit.ParkId,
            "item-1",
            visit.VisitDate,
            RideOccurrenceStatus.Completed,
            null,
            null,
            null);
        Mock<IPassportScopeStatisticsSourceReader> sourceReader = new();
        sourceReader.Setup(reader => reader.ReadGlobalAsync(
                "user-1",
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PassportGlobalStatisticsSource(
                new[] { 2001 },
                new[] { "park-1" },
                new[] { visit },
                new[] { ride }));
        Mock<IPassportHistoricalTargetResolver> resolver = new();
        resolver.Setup(item => item.ResolveAllManyAsync(
                "park-1",
                It.IsAny<IReadOnlyCollection<VisitDate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                string parkId,
                IReadOnlyCollection<VisitDate> dates,
                CancellationToken cancellationToken) =>
                dates.ToDictionary(
                    static date => date,
                    date => Context(date.Year == 2001
                        ? Target("Old Name", HistoricalOperationalState.KnownOpen)
                        : Target("New Name", HistoricalOperationalState.KnownClosed))));
        Mock<IParkNameReadRepository> parkNames = new();
        parkNames.Setup(repository => repository.GetNamesByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string?>
            {
                ["park-1"] = "Example Park",
            });
        GetPassportHistoricalStatisticsQueryHandler handler = new(
            sourceReader.Object,
            resolver.Object,
            parkNames.Object);

        AmusementPark.Application.Errors.ApplicationResult<PassportHistoricalStatisticsResult>
            result =
            await handler.HandleAsync(new GetPassportHistoricalStatisticsQuery("user-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.CanonicallyResolvedRideCount);
        Assert.Equal("Example Park", Assert.Single(result.Value.DisappearedAttractions).ParkName);
        Assert.Equal("Old Name", Assert.Single(result.Value.HistoricalNames).NameAtVisit);
        resolver.Verify(item => item.ResolveAllManyAsync(
            "park-1",
            It.Is<IReadOnlyCollection<VisitDate>>(dates => dates.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidUserId_ShouldNotReadPrivateData()
    {
        Mock<IPassportScopeStatisticsSourceReader> sourceReader = new();
        GetPassportHistoricalStatisticsQueryHandler handler = new(
            sourceReader.Object,
            Mock.Of<IPassportHistoricalTargetResolver>(),
            Mock.Of<IParkNameReadRepository>());

        AmusementPark.Application.Errors.ApplicationResult<PassportHistoricalStatisticsResult>
            result =
            await handler.HandleAsync(new GetPassportHistoricalStatisticsQuery(" "));

        Assert.False(result.IsSuccess);
        sourceReader.VerifyNoOtherCalls();
    }

    private static PassportHistoricalTargetContext Context(PassportHistoricalTarget target)
    {
        return new PassportHistoricalTargetContext(
            new Dictionary<string, PassportHistoricalTarget>(StringComparer.Ordinal)
            {
                [target.ParkItemId] = target,
            },
            HistoricalCoverageStatus.HighConfidence,
            100,
            "history-v1");
    }

    private static PassportHistoricalTarget Target(
        string name,
        HistoricalOperationalState state)
    {
        return new PassportHistoricalTarget(
            "item-1",
            "park-1",
            name,
            "Attraction",
            state,
            HistoricalConsistency.Verified,
            new HistoricalTargetReference(name, "Attraction"),
            false,
            null,
            null,
            null,
            null,
            null);
    }
}
