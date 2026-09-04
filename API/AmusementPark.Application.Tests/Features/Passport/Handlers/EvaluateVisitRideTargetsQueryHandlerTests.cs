using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class EvaluateVisitRideTargetsQueryHandlerTests
{
    private static readonly DateTime NowUtc = new DateTime(
        2026,
        9,
        5,
        12,
        0,
        0,
        DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_ShouldEvaluateEveryTargetAgainstTheOwnedVisitDate()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visitRepository =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        visitRepository.Setup(value => value.GetOwnedAsync(
                VisitId.Parse("visit-1"),
                "owner-1",
                CancellationToken.None))
            .ReturnsAsync(visit);
        Mock<IVisitTargetResolver> targetResolver =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        targetResolver.Setup(value => value.ResolveAsync(
                It.Is<IReadOnlyCollection<string>>(ids =>
                    ids.SequenceEqual(new[] { "active", "closed", "unknown" })),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            {
                ["active"] = CreateTarget(
                    "active",
                    "Attraction active",
                    new DateOnly(2020, 1, 1),
                    null),
                ["closed"] = CreateTarget(
                    "closed",
                    "Attraction fermée",
                    new DateOnly(2010, 1, 1),
                    new DateOnly(2020, 12, 31)),
                ["unknown"] = CreateTarget("unknown", "Dates inconnues", null, null),
            });
        EvaluateVisitRideTargetsQueryHandler handler =
            new EvaluateVisitRideTargetsQueryHandler(
                visitRepository.Object,
                targetResolver.Object);

        ApplicationResult<IReadOnlyCollection<VisitRideTargetEvaluationResult>> result =
            await handler.HandleAsync(
                new EvaluateVisitRideTargetsQuery(
                    " owner-1 ",
                    "visit-1",
                    new[] { "active", "closed", "unknown", "active" }),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        VisitRideTargetEvaluationResult[] evaluations = Assert.IsAssignableFrom<
            IReadOnlyCollection<VisitRideTargetEvaluationResult>>(result.Value).ToArray();
        Assert.Collection(
            evaluations,
            value => Assert.Equal(HistoricalConsistency.Verified, value.HistoricalConsistency),
            value =>
            {
                Assert.Equal(HistoricalConsistency.ConfirmedConflict, value.HistoricalConsistency);
                Assert.Equal(new DateOnly(2020, 12, 31), value.ClosingDate);
            },
            value => Assert.Equal(HistoricalConsistency.Unverified, value.HistoricalConsistency));
        visitRepository.VerifyAll();
        targetResolver.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotRevealTargetsWhenTheVisitIsNotOwned()
    {
        Mock<IUserVisitRepository> visitRepository =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        visitRepository.Setup(value => value.GetOwnedAsync(
                VisitId.Parse("visit-1"),
                "owner-1",
                CancellationToken.None))
            .ReturnsAsync((Visit?)null);
        Mock<IVisitTargetResolver> targetResolver =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        EvaluateVisitRideTargetsQueryHandler handler =
            new EvaluateVisitRideTargetsQueryHandler(
                visitRepository.Object,
                targetResolver.Object);

        ApplicationResult<IReadOnlyCollection<VisitRideTargetEvaluationResult>> result =
            await handler.HandleAsync(
                new EvaluateVisitRideTargetsQuery(
                    "owner-1",
                    "visit-1",
                    new[] { "item-1" }),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("visit.not-found", Assert.Single(result.Errors).Code);
        visitRepository.VerifyAll();
        targetResolver.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldApplyTheRideTargetIdentityRules()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visitRepository =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        visitRepository.Setup(value => value.GetOwnedAsync(
                VisitId.Parse("visit-1"),
                "owner-1",
                CancellationToken.None))
            .ReturnsAsync(visit);
        Mock<IVisitTargetResolver> targetResolver =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        targetResolver.Setup(value => value.ResolveAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Single() == "item-1"),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            {
                ["item-1"] = new VisitTarget(
                    "item-1",
                    "another-park",
                    "Autre parc",
                    ParkItemCategory.Attraction,
                    null,
                    null),
            });
        EvaluateVisitRideTargetsQueryHandler handler =
            new EvaluateVisitRideTargetsQueryHandler(
                visitRepository.Object,
                targetResolver.Object);

        ApplicationResult<IReadOnlyCollection<VisitRideTargetEvaluationResult>> result =
            await handler.HandleAsync(
                new EvaluateVisitRideTargetsQuery(
                    "owner-1",
                    "visit-1",
                    new[] { "item-1" }),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("ride-occurrence.target-park-mismatch", Assert.Single(result.Errors).Code);
        visitRepository.VerifyAll();
        targetResolver.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotExposeHistoricalBoundsFromAHiddenTarget()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visitRepository =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        visitRepository.Setup(value => value.GetOwnedAsync(
                VisitId.Parse("visit-1"),
                "owner-1",
                CancellationToken.None))
            .ReturnsAsync(visit);
        Mock<IVisitTargetResolver> targetResolver =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        targetResolver.Setup(value => value.ResolveAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Single() == "hidden-item"),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            {
                ["hidden-item"] = CreateTarget(
                    "hidden-item",
                    "Attraction cachée",
                    new DateOnly(1990, 1, 1),
                    new DateOnly(2000, 1, 1)) with { IsVisible = false },
            });
        EvaluateVisitRideTargetsQueryHandler handler =
            new EvaluateVisitRideTargetsQueryHandler(
                visitRepository.Object,
                targetResolver.Object);

        ApplicationResult<IReadOnlyCollection<VisitRideTargetEvaluationResult>> result =
            await handler.HandleAsync(
                new EvaluateVisitRideTargetsQuery(
                    "owner-1",
                    "visit-1",
                    new[] { "hidden-item" }),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("ride-occurrence.target-not-found", Assert.Single(result.Errors).Code);
        visitRepository.VerifyAll();
        targetResolver.VerifyAll();
    }

    private static Visit CreateVisit()
    {
        return Visit.Create(
            VisitId.Parse("visit-1"),
            "owner-1",
            "park-1",
            VisitDate.ForDay(2026, 7, 26),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            NowUtc);
    }

    private static VisitTarget CreateTarget(
        string id,
        string name,
        DateOnly? openingDate,
        DateOnly? closingDate)
    {
        return new VisitTarget(
            id,
            "park-1",
            name,
            ParkItemCategory.Attraction,
            openingDate,
            closingDate);
    }
}
