using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
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

public sealed class RideOccurrenceHandlersTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AddBatch_ShouldExpandCountAndPersistOneOccurrencePerRide()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IVisitTargetResolver> targets = CreateTargetResolver(
            new VisitTarget(
                "item-1",
                "park-1",
                "Attraction",
                ParkItemCategory.Attraction,
                new DateOnly(2000, 1, 1),
                null));
        occurrences.Setup(repository => repository.GetLastSortPositionAsync(
                visit.Id,
                "owner-1",
                CancellationToken.None))
            .ReturnsAsync((long?)null);
        occurrences.Setup(repository => repository.ResolveExistingBatchCreationAsync(
                It.Is<RideOccurrenceCreationRequest>(request =>
                    request.VisitId == visit.Id
                    && request.UserId == visit.UserId
                    && request.Items.Count == 2
                    && request.Items.All(static item => item.ParkItemId == "item-1")),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync((IdempotentRideOccurrenceCreationResult?)null);
        IReadOnlyList<RideOccurrence>? captured = null;
        occurrences.Setup(repository => repository.CreateBatchIdempotentAsync(
                It.IsAny<IReadOnlyList<RideOccurrence>>(),
                "request-1",
                CancellationToken.None))
            .Callback((IReadOnlyList<RideOccurrence> items, string _, CancellationToken _) =>
                captured = items)
            .ReturnsAsync(() => new IdempotentRideOccurrenceCreationResult(
                IdempotentRideOccurrenceCreationStatus.Created,
                captured!));
        AddRideOccurrencesBatchCommandHandler handler = new AddRideOccurrencesBatchCommandHandler(
            visits.Object,
            occurrences.Object,
            targets.Object,
            CreateClock());

        ApplicationResult<CreateRideOccurrencesResult> result = await handler.HandleAsync(
            CreateBatchCommand(count: 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value?.Occurrences.Count);
        Assert.Equal(new[] { 1024L, 2048L }, captured?.Select(static item => item.SortPosition));
        Assert.All(captured!, static item =>
        {
            Assert.Equal(HistoricalConsistency.Verified, item.HistoricalConsistency);
            Assert.True(item.CountsAsRide);
        });
        visits.VerifyAll();
        occurrences.VerifyAll();
        targets.VerifyAll();
    }

    [Fact]
    public async Task AddBatch_WhenOperationAlreadyExists_ShouldReplayBeforeMutableReads()
    {
        Visit visit = CreateVisit();
        RideOccurrence occurrence = CreateOccurrence(visit, "occurrence-1", 1024);
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IVisitTargetResolver> targets =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        occurrences.Setup(repository => repository.ResolveExistingBatchCreationAsync(
                It.Is<RideOccurrenceCreationRequest>(request =>
                    request.VisitId == visit.Id
                    && request.UserId == visit.UserId
                    && Assert.Single(request.Items).ParkItemId == "item-1"),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync(new IdempotentRideOccurrenceCreationResult(
                IdempotentRideOccurrenceCreationStatus.Replayed,
                new[] { occurrence }));
        AddRideOccurrencesBatchCommandHandler handler = new AddRideOccurrencesBatchCommandHandler(
            visits.Object,
            occurrences.Object,
            targets.Object,
            clock.Object);

        ApplicationResult<CreateRideOccurrencesResult> result = await handler.HandleAsync(
            CreateBatchCommand());

        Assert.True(result.IsSuccess);
        Assert.True(result.Value?.WasReplayed);
        Assert.Equal("occurrence-1", Assert.Single(result.Value!.Occurrences).Id);
        occurrences.VerifyAll();
        visits.VerifyNoOtherCalls();
        targets.VerifyNoOtherCalls();
        clock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddBatch_WithCertainHistoricalConflict_ShouldRequireExplicitConfirmation()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IVisitTargetResolver> targets = CreateTargetResolver(
            new VisitTarget(
                "item-1",
                "park-1",
                "Attraction",
                ParkItemCategory.Attraction,
                new DateOnly(2027, 1, 1),
                null));
        occurrences.Setup(repository => repository.ResolveExistingBatchCreationAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync((IdempotentRideOccurrenceCreationResult?)null);
        AddRideOccurrencesBatchCommandHandler handler = new AddRideOccurrencesBatchCommandHandler(
            visits.Object,
            occurrences.Object,
            targets.Object,
            CreateClock());

        ApplicationResult<CreateRideOccurrencesResult> result = await handler.HandleAsync(
            CreateBatchCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "ride-occurrence.historical-conflict-confirmation-required",
            Assert.Single(result.Errors).Code);
        occurrences.VerifyAll();
        occurrences.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_WithStaleVersion_ShouldNotPersist()
    {
        Visit visit = CreateVisit();
        RideOccurrence occurrence = CreateOccurrence(visit, "occurrence-1", 1024);
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        occurrences.Setup(repository => repository.GetOwnedAsync(
                occurrence.Id,
                visit.Id,
                "owner-1",
                CancellationToken.None))
            .ReturnsAsync(occurrence);
        Mock<IVisitTargetResolver> targets =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        UpdateRideOccurrenceCommandHandler handler = new UpdateRideOccurrenceCommandHandler(
            visits.Object,
            occurrences.Object,
            targets.Object,
            CreateClock());

        ApplicationResult<RideOccurrenceResult> result = await handler.HandleAsync(
            new UpdateRideOccurrenceCommand(
                "owner-1",
                "visit-1",
                "occurrence-1",
                2,
                null,
                false,
                RideOccurrenceStatus.Attempted,
                null,
                false));

        Assert.False(result.IsSuccess);
        Assert.Equal("ride-occurrence.version-conflict", Assert.Single(result.Errors).Code);
        occurrences.VerifyAll();
        targets.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Reorder_WhenOperationAlreadyExists_ShouldReplayBeforeMutableReads()
    {
        Visit visit = CreateVisit();
        RideOccurrence occurrence = CreateOccurrence(visit, "occurrence-1", 1536);
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        occurrences.Setup(repository => repository.ResolveExistingReorderAsync(
                It.Is<RideOccurrenceReorderRequest>(request =>
                    request.OccurrenceId == occurrence.Id
                    && request.AnchorOccurrenceId == RideOccurrenceId.Parse("occurrence-2")),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync(new IdempotentRideOccurrenceReorderResult(
                IdempotentRideOccurrenceReorderStatus.Replayed,
                occurrence,
                false));
        ReorderRideOccurrenceCommandHandler handler = new ReorderRideOccurrenceCommandHandler(
            visits.Object,
            occurrences.Object,
            CreateClock());

        ApplicationResult<ReorderRideOccurrenceResult> result = await handler.HandleAsync(
            new ReorderRideOccurrenceCommand(
                "owner-1",
                "visit-1",
                "request-1",
                "occurrence-1",
                1,
                "occurrence-2",
                RideOccurrencePlacement.Before));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value?.WasReplayed);
        Assert.Equal(1536, result.Value?.Occurrence.SortPosition);
        occurrences.VerifyAll();
        visits.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Reorder_WithGap_ShouldPersistOnlyTheMovedOccurrence()
    {
        Visit visit = CreateVisit();
        RideOccurrence first = CreateOccurrence(visit, "occurrence-1", 1024);
        RideOccurrence second = CreateOccurrence(visit, "occurrence-2", 2048);
        RideOccurrence moved = CreateOccurrence(visit, "occurrence-3", 3072);
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        occurrences.Setup(repository => repository.ResolveExistingReorderAsync(
                It.IsAny<RideOccurrenceReorderRequest>(),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync((IdempotentRideOccurrenceReorderResult?)null);
        occurrences.Setup(repository => repository.ListOwnedByVisitAsync(
                It.Is<RideOccurrenceListCriteria>(criteria => criteria.After == null),
                CancellationToken.None))
            .ReturnsAsync(new RideOccurrencePage(
                new[] { first, second, moved },
                null));
        occurrences.Setup(repository => repository.ReorderIdempotentAsync(
                It.IsAny<RideOccurrenceReorderRequest>(),
                It.Is<IReadOnlyCollection<RideOccurrenceVersionedChange>>(changes =>
                    changes.Count == 1
                    && changes.Single().Occurrence.Id == moved.Id
                    && changes.Single().Occurrence.SortPosition == 1536),
                It.Is<RideOccurrence>(item => item.Id == moved.Id),
                false,
                NowUtc.AddMinutes(1),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync(() => new IdempotentRideOccurrenceReorderResult(
                IdempotentRideOccurrenceReorderStatus.Applied,
                moved,
                false));
        ReorderRideOccurrenceCommandHandler handler = new ReorderRideOccurrenceCommandHandler(
            visits.Object,
            occurrences.Object,
            CreateClock());

        ApplicationResult<ReorderRideOccurrenceResult> result = await handler.HandleAsync(
            new ReorderRideOccurrenceCommand(
                "owner-1",
                "visit-1",
                "request-1",
                "occurrence-3",
                1,
                "occurrence-2",
                RideOccurrencePlacement.Before));

        Assert.True(result.IsSuccess);
        Assert.Equal(1536, result.Value?.Occurrence.SortPosition);
        Assert.False(result.Value?.WasNormalized);
        occurrences.VerifyAll();
    }

    [Fact]
    public async Task List_ShouldFirstVerifyVisitOwnershipAndMapThePage()
    {
        Visit visit = CreateVisit();
        RideOccurrence occurrence = CreateOccurrence(visit, "occurrence-1", 1024);
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        occurrences.Setup(repository => repository.ListOwnedByVisitAsync(
                It.Is<RideOccurrenceListCriteria>(criteria =>
                    criteria.VisitId == visit.Id
                    && criteria.UserId == "owner-1"
                    && criteria.Limit == 25),
                CancellationToken.None))
            .ReturnsAsync(new RideOccurrencePage(new[] { occurrence }, null));
        ListRideOccurrencesQueryHandler handler = new ListRideOccurrencesQueryHandler(
            visits.Object,
            occurrences.Object);

        ApplicationResult<RideOccurrencePageResult> result = await handler.HandleAsync(
            new ListRideOccurrencesQuery("owner-1", "visit-1", 25));

        Assert.True(result.IsSuccess);
        Assert.Equal("occurrence-1", Assert.Single(result.Value!.Items).Id);
        visits.VerifyAll();
        occurrences.VerifyAll();
    }

    private static AddRideOccurrencesBatchCommand CreateBatchCommand(int count = 1)
    {
        return new AddRideOccurrencesBatchCommand(
            "owner-1",
            "visit-1",
            "request-1",
            new[]
            {
                new RideOccurrenceCreationItem(
                    "item-1",
                    null,
                    false,
                    RideOccurrenceStatus.Completed,
                    null,
                    false,
                    count),
            });
    }

    private static Mock<IUserVisitRepository> CreateVisitRepository(Visit visit)
    {
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        visits.Setup(repository => repository.GetOwnedAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(visit);
        return visits;
    }

    private static Mock<IVisitTargetResolver> CreateTargetResolver(VisitTarget target)
    {
        Mock<IVisitTargetResolver> targets =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        targets.Setup(resolver => resolver.ResolveAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, VisitTarget>
            {
                [target.ParkItemId] = target,
            });
        return targets;
    }

    private static IPassportClock CreateClock()
    {
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc.AddMinutes(1));
        return clock.Object;
    }

    private static Visit CreateVisit()
    {
        return Visit.Create(
            VisitId.Parse("visit-1"),
            "owner-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            NowUtc);
    }

    private static RideOccurrence CreateOccurrence(
        Visit visit,
        string id,
        long position)
    {
        return RideOccurrence.Create(
            RideOccurrenceId.Parse(id),
            visit,
            $"item-{id}",
            position,
            new OccurrenceMoment(null, false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            null,
            NowUtc);
    }
}
