using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class ListVisitHistoricalRideTargetsQueryHandlerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 27, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_ShouldFilterAndPageTheCanonicalHistoricalCatalogue()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visits = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        visits.Setup(repository => repository.GetOwnedAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(visit);
        PassportHistoricalTarget knownOpen = CreateTarget(
            "ride-open",
            "Le Cyclone",
            HistoricalOperationalState.KnownOpen,
            "zone-1");
        PassportHistoricalTarget possible = CreateTarget(
            "ride-possible",
            "Cyclone Junior",
            HistoricalOperationalState.PossiblyOpen,
            "zone-1");
        PassportHistoricalTarget closed = CreateTarget(
            "ride-closed",
            "Ancien Train",
            HistoricalOperationalState.KnownClosed,
            "zone-2");
        Mock<IPassportHistoricalTargetResolver> targets =
            new Mock<IPassportHistoricalTargetResolver>(MockBehavior.Strict);
        targets.Setup(resolver => resolver.ResolveAllAsync(visit, CancellationToken.None))
            .ReturnsAsync(new PassportHistoricalTargetContext(
                new[] { knownOpen, possible, closed }.ToDictionary(
                    static target => target.ParkItemId,
                    StringComparer.Ordinal),
                HistoricalCoverageStatus.Substantial,
                76,
                "history-v2"));
        ListVisitHistoricalRideTargetsQueryHandler handler =
            new ListVisitHistoricalRideTargetsQueryHandler(visits.Object, targets.Object);

        ApplicationResult<VisitHistoricalRideTargetPageResult> result =
            await handler.HandleAsync(new ListVisitHistoricalRideTargetsQuery(
                visit.UserId,
                visit.Id.Value,
                "cyclone",
                VisitHistoricalTargetScope.AllHistory,
                "zone-1",
                1,
                1));

        Assert.True(result.IsSuccess);
        VisitHistoricalRideTargetPageResult page = Assert.IsType<VisitHistoricalRideTargetPageResult>(
            result.Value);
        VisitRideTargetEvaluationResult item = Assert.Single(page.Items);
        Assert.Equal("ride-open", item.ParkItemId);
        Assert.Equal(HistoricalOperationalState.KnownOpen, item.OperationalState);
        Assert.Equal(2, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(1, page.KnownOpenCount);
        Assert.Equal(1, page.PossiblyOpenCount);
        Assert.Equal(3, page.AllHistoryCount);
        Assert.Equal(76, page.CoveragePercent);
        Assert.Equal("history-v2", page.MethodologyVersion);
        visits.VerifyAll();
        targets.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotResolveHistoryForAnUnownedVisit()
    {
        Mock<IUserVisitRepository> visits = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        visits.Setup(repository => repository.GetOwnedAsync(
                VisitId.Parse("visit-1"),
                "owner-1",
                CancellationToken.None))
            .ReturnsAsync((Visit?)null);
        Mock<IPassportHistoricalTargetResolver> targets =
            new Mock<IPassportHistoricalTargetResolver>(MockBehavior.Strict);
        ListVisitHistoricalRideTargetsQueryHandler handler =
            new ListVisitHistoricalRideTargetsQueryHandler(visits.Object, targets.Object);

        ApplicationResult<VisitHistoricalRideTargetPageResult> result =
            await handler.HandleAsync(new ListVisitHistoricalRideTargetsQuery(
                "owner-1",
                "visit-1",
                null,
                VisitHistoricalTargetScope.KnownOpen,
                null,
                1,
                24));

        Assert.False(result.IsSuccess);
        Assert.Equal("visit.not-found", Assert.Single(result.Errors).Code);
        visits.VerifyAll();
        targets.VerifyNoOtherCalls();
    }

    private static Visit CreateVisit()
    {
        return Visit.Create(
            VisitId.Parse("visit-1"),
            "owner-1",
            "park-1",
            VisitDate.ForYear(1994),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            NowUtc);
    }

    private static PassportHistoricalTarget CreateTarget(
        string id,
        string name,
        HistoricalOperationalState operationalState,
        string zoneId)
    {
        string category = "Attraction";
        return new PassportHistoricalTarget(
            id,
            "park-1",
            name,
            category,
            operationalState,
            RideOccurrenceHistoricalConsistencyEvaluator.Evaluate(operationalState),
            new HistoricalTargetReference(name, category),
            operationalState == HistoricalOperationalState.KnownClosed,
            null,
            zoneId,
            null,
            null,
            null);
    }
}
