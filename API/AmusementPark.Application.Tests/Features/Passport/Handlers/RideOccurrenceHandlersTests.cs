using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
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
        occurrences.Setup(repository => repository.GetAppendStateAsync(
                visit.Id,
                "owner-1",
                "request-1",
                CancellationToken.None))
            .ReturnsAsync(new RideOccurrenceAppendState(null, false));
        occurrences.Setup(repository => repository.ResolveExistingBatchCreationAsync(
                It.Is<RideOccurrenceCreationRequest>(request =>
                    request.VisitId == visit.Id
                    && request.UserId == visit.UserId
                    && request.Items.Count == 2
                    && request.Items.All(static item => item.ParkItemId == "item-1")),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync((IdempotentRideOccurrenceCreationResult?)null);
        SetupCreationKeyReservation(occurrences);
        IReadOnlyList<RideOccurrence>? captured = null;
        occurrences.Setup(repository => repository.CreateBatchIdempotentAsync(
                It.Is<RideOccurrenceCreationRequest>(request =>
                    request.Items.All(static item => !item.ConfirmHistoricalConflict)),
                It.IsAny<IReadOnlyList<RideOccurrence>>(),
                null,
                false,
                "request-1",
                CancellationToken.None))
            .Callback((
                RideOccurrenceCreationRequest _,
                IReadOnlyList<RideOccurrence> items,
                long? _,
                bool _,
                string _,
                CancellationToken _) =>
                captured = items)
            .ReturnsAsync(() => new IdempotentRideOccurrenceCreationResult(
                IdempotentRideOccurrenceCreationStatus.Created,
                captured!));
        AddRideOccurrencesBatchCommandHandler handler = CreateAddHandler(
            visits,
            occurrences,
            targets,
            CreateClock());

        ApplicationResult<CreateRideOccurrencesResult> result = await handler.HandleAsync(
            CreateBatchCommand(count: 2, source: RideLogSource.Import));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value?.WasNormalized);
        Assert.Equal(2, result.Value?.Occurrences.Count);
        Assert.Equal(new[] { 1024L, 2048L }, captured?.Select(static item => item.SortPosition));
        Assert.All(captured!, static item =>
        {
            Assert.Equal(HistoricalConsistency.Verified, item.HistoricalConsistency);
            Assert.Equal(RideLogSource.Import, item.Source);
            Assert.True(item.CountsAsRide);
        });
        visits.VerifyAll();
        occurrences.VerifyAll();
        targets.VerifyAll();
    }

    [Fact]
    public async Task AddBatch_WhenPendingOperationIsRetriedAfterCompletion_ShouldNotResumeIt()
    {
        Visit visit = CreateVisit();
        visit.Complete(new DateOnly(2026, 9, 3), NowUtc.AddMinutes(1));
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IVisitTargetResolver> targets =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        Mock<IPassportAuditPublisher> audit =
            new Mock<IPassportAuditPublisher>(MockBehavior.Strict);
        Mock<IVisitContentMutationLeaseManager> leases =
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict);
        IPassportClock clock = CreateClock();
        AddRideOccurrencesBatchCommandHandler handler =
            new AddRideOccurrencesBatchCommandHandler(
                visits.Object,
                occurrences.Object,
                targets.Object,
                new RideOccurrenceAppendOrderNormalizer(occurrences.Object, clock),
                clock,
                audit.Object,
                leases.Object);

        ApplicationResult<CreateRideOccurrencesResult> result =
            await handler.HandleAsync(CreateBatchCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal("visit.not-editable", Assert.Single(result.Errors).Code);
        visits.VerifyAll();
        occurrences.VerifyNoOtherCalls();
        targets.VerifyNoOtherCalls();
        audit.VerifyNoOtherCalls();
        leases.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddBatch_WhenPriorNormalizationIsDurable_ShouldPreserveTheSignal()
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
        occurrences.Setup(repository => repository.ResolveExistingBatchCreationAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync((IdempotentRideOccurrenceCreationResult?)null);
        SetupCreationKeyReservation(occurrences);
        occurrences.Setup(repository => repository.GetAppendStateAsync(
                visit.Id,
                visit.UserId,
                "request-1",
                CancellationToken.None))
            .ReturnsAsync(new RideOccurrenceAppendState(2048, true));
        occurrences.Setup(repository => repository.CreateBatchIdempotentAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                It.IsAny<IReadOnlyList<RideOccurrence>>(),
                2048,
                true,
                "request-1",
                CancellationToken.None))
            .ReturnsAsync((
                RideOccurrenceCreationRequest _,
                IReadOnlyList<RideOccurrence> created,
                long? _,
                bool _,
                string _,
                CancellationToken _) => new IdempotentRideOccurrenceCreationResult(
                    IdempotentRideOccurrenceCreationStatus.Created,
                    created,
                    true));
        AddRideOccurrencesBatchCommandHandler handler = CreateAddHandler(
            visits,
            occurrences,
            targets,
            CreateClock());

        ApplicationResult<CreateRideOccurrencesResult> result = await handler.HandleAsync(
            CreateBatchCommand());

        Assert.True(result.IsSuccess);
        Assert.True(result.Value?.WasNormalized);
        occurrences.VerifyAll();
        visits.VerifyAll();
        targets.VerifyAll();
    }

    [Fact]
    public async Task AddBatch_WhenAppendBaseChanges_ShouldReallocateTheWholeBatch()
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
        occurrences.Setup(repository => repository.ResolveExistingBatchCreationAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync((IdempotentRideOccurrenceCreationResult?)null);
        SetupCreationKeyReservation(occurrences);
        occurrences.SetupSequence(repository => repository.GetAppendStateAsync(
                visit.Id,
                "owner-1",
                "request-1",
                CancellationToken.None))
            .ReturnsAsync(new RideOccurrenceAppendState(null, false))
            .ReturnsAsync(new RideOccurrenceAppendState(2048, false));
        List<IReadOnlyList<long>> attemptedPositions = new List<IReadOnlyList<long>>();
        int persistenceAttempt = 0;
        occurrences.Setup(repository => repository.CreateBatchIdempotentAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                It.IsAny<IReadOnlyList<RideOccurrence>>(),
                It.IsAny<long?>(),
                false,
                "request-1",
                CancellationToken.None))
            .Callback((
                RideOccurrenceCreationRequest _,
                IReadOnlyList<RideOccurrence> items,
                long? _,
                bool _,
                string _,
                CancellationToken _) =>
            {
                persistenceAttempt++;
                attemptedPositions.Add(items
                    .Select(static item => item.SortPosition)
                    .ToArray());
            })
            .ReturnsAsync((
                RideOccurrenceCreationRequest _,
                IReadOnlyList<RideOccurrence> items,
                long? _,
                bool _,
                string _,
                CancellationToken _) => persistenceAttempt == 1
                ? new IdempotentRideOccurrenceCreationResult(
                    IdempotentRideOccurrenceCreationStatus.ConcurrencyConflict,
                    Array.Empty<RideOccurrence>())
                : new IdempotentRideOccurrenceCreationResult(
                    IdempotentRideOccurrenceCreationStatus.Created,
                    items));
        AddRideOccurrencesBatchCommandHandler handler = CreateAddHandler(
            visits,
            occurrences,
            targets,
            CreateClock());

        ApplicationResult<CreateRideOccurrencesResult> result = await handler.HandleAsync(
            CreateBatchCommand(count: 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, persistenceAttempt);
        Assert.Equal(new[] { 1024L, 2048L }, attemptedPositions[0]);
        Assert.Equal(new[] { 3072L, 4096L }, attemptedPositions[1]);
        visits.VerifyAll();
        occurrences.VerifyAll();
        targets.VerifyAll();
    }

    [Fact]
    public async Task AddBatch_WhenAppendPositionOverflows_ShouldNormalizeBeforeCreating()
    {
        Visit visit = CreateVisit();
        RideOccurrence first = CreateOccurrence(
            visit,
            "occurrence-1",
            long.MaxValue - 2048);
        RideOccurrence last = CreateOccurrence(
            visit,
            "occurrence-2",
            long.MaxValue - 100);
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
        occurrences.Setup(repository => repository.ResolveExistingBatchCreationAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync((IdempotentRideOccurrenceCreationResult?)null);
        SetupCreationKeyReservation(occurrences);
        occurrences.SetupSequence(repository => repository.GetAppendStateAsync(
                visit.Id,
                visit.UserId,
                "request-1",
                CancellationToken.None))
            .ReturnsAsync(new RideOccurrenceAppendState(last.SortPosition, false))
            .ReturnsAsync(new RideOccurrenceAppendState(2048, true));
        occurrences.Setup(repository => repository.ListOwnedByVisitAsync(
                It.Is<RideOccurrenceListCriteria>(criteria =>
                    criteria.VisitId == visit.Id
                    && criteria.UserId == visit.UserId
                    && criteria.After == null
                    && criteria.Limit == RideOccurrenceListCriteria.MaximumLimit),
                CancellationToken.None))
            .ReturnsAsync(new RideOccurrencePage(new[] { first, last }, null));
        occurrences.Setup(repository => repository.ReorderIdempotentAsync(
                It.Is<RideOccurrenceReorderRequest>(request =>
                    request.OccurrenceId == first.Id
                    && request.ExpectedVersion == 1
                    && request.AnchorOccurrenceId == null
                    && request.Placement == RideOccurrencePlacement.First),
                It.Is<IReadOnlyCollection<RideOccurrenceVersionedChange>>(changes =>
                    changes.Count == 2
                    && changes.Any(change =>
                        change.Occurrence.Id == first.Id
                        && change.Occurrence.SortPosition == 1024)
                    && changes.Any(change =>
                        change.Occurrence.Id == last.Id
                        && change.Occurrence.SortPosition == 2048)),
                It.Is<IReadOnlyCollection<RideOccurrenceOrderGuard>>(guards =>
                    guards.Count == 2
                    && guards.Any(guard =>
                        guard.OccurrenceId == first.Id
                        && guard.SortPosition == long.MaxValue - 2048)
                    && guards.Any(guard =>
                        guard.OccurrenceId == last.Id
                        && guard.SortPosition == long.MaxValue - 100)),
                It.Is<RideOccurrence>(occurrence =>
                    occurrence.Id == first.Id
                    && occurrence.SortPosition == 1024),
                true,
                NowUtc.AddMinutes(1),
                It.Is<string>(operationId =>
                    operationId.StartsWith(
                        "internal-passport-append-normalization-v1:",
                        StringComparison.Ordinal)
                    && operationId.Length == 106),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync(() => new IdempotentRideOccurrenceReorderResult(
                IdempotentRideOccurrenceReorderStatus.Applied,
                first,
                true));
        IReadOnlyList<RideOccurrence>? created = null;
        occurrences.Setup(repository => repository.CreateBatchIdempotentAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                It.IsAny<IReadOnlyList<RideOccurrence>>(),
                2048,
                true,
                "request-1",
                CancellationToken.None))
            .Callback((
                RideOccurrenceCreationRequest _,
                IReadOnlyList<RideOccurrence> items,
                long? _,
                bool _,
                string _,
                CancellationToken _) => created = items)
            .ReturnsAsync(() => new IdempotentRideOccurrenceCreationResult(
                IdempotentRideOccurrenceCreationStatus.Created,
                created!,
                true));
        AddRideOccurrencesBatchCommandHandler handler = CreateAddHandler(
            visits,
            occurrences,
            targets,
            CreateClock());

        ApplicationResult<CreateRideOccurrencesResult> result = await handler.HandleAsync(
            CreateBatchCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(3072, Assert.Single(created!).SortPosition);
        Assert.True(result.Value?.WasNormalized);
        occurrences.VerifyAll();
        visits.VerifyAll();
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
        AddRideOccurrencesBatchCommandHandler handler = CreateAddHandler(
            visits,
            occurrences,
            targets,
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
    public async Task AddBatch_WhenCreationKeyReservationConflicts_ShouldStopBeforeReadingOrder()
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
        occurrences.Setup(repository => repository.ResolveExistingBatchCreationAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync((IdempotentRideOccurrenceCreationResult?)null);
        SetupMissingCreationKeyReservation(occurrences);
        occurrences.Setup(repository => repository.ReserveBatchCreationKeyAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                It.IsAny<RideOccurrenceCreationPreparation>(),
                "request-1",
                NowUtc.AddMinutes(1),
                CancellationToken.None))
            .ReturnsAsync(new RideOccurrenceCreationKeyReservationResult(
                RideOccurrenceCreationKeyReservationStatus.Conflict));
        AddRideOccurrencesBatchCommandHandler handler = CreateAddHandler(
            visits,
            occurrences,
            targets,
            CreateClock());

        ApplicationResult<CreateRideOccurrencesResult> result = await handler.HandleAsync(
            CreateBatchCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "ride-occurrence.idempotency-key-conflict",
            Assert.Single(result.Errors).Code);
        occurrences.VerifyAll();
        occurrences.VerifyNoOtherCalls();
        visits.VerifyAll();
        targets.VerifyAll();
    }

    [Fact]
    public async Task AddBatch_WhenCreationFinalizesDuringValidation_ShouldRecheckTheCreation()
    {
        Visit visit = CreateVisit();
        RideOccurrence occurrence = CreateOccurrence(visit, "occurrence-1", 1024);
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
        occurrences.SetupSequence(repository => repository.ResolveExistingBatchCreationAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync((IdempotentRideOccurrenceCreationResult?)null)
            .ReturnsAsync(new IdempotentRideOccurrenceCreationResult(
                IdempotentRideOccurrenceCreationStatus.Replayed,
                new[] { occurrence }));
        SetupMissingCreationKeyReservation(occurrences);
        occurrences.Setup(repository => repository.ReserveBatchCreationKeyAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                It.IsAny<RideOccurrenceCreationPreparation>(),
                "request-1",
                NowUtc.AddMinutes(1),
                CancellationToken.None))
            .ReturnsAsync(new RideOccurrenceCreationKeyReservationResult(
                RideOccurrenceCreationKeyReservationStatus.Finalized));
        AddRideOccurrencesBatchCommandHandler handler = CreateAddHandler(
            visits,
            occurrences,
            targets,
            CreateClock());

        ApplicationResult<CreateRideOccurrencesResult> result = await handler.HandleAsync(
            CreateBatchCommand());

        Assert.True(result.IsSuccess);
        Assert.True(result.Value?.WasReplayed);
        Assert.Equal("occurrence-1", Assert.Single(result.Value!.Occurrences).Id);
        occurrences.VerifyAll();
        occurrences.VerifyNoOtherCalls();
        visits.VerifyAll();
        targets.VerifyAll();
    }

    [Fact]
    public async Task AddBatch_WithReservedPreparation_ShouldResumeBeforeMutableValidation()
    {
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IVisitTargetResolver> targets =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        RideOccurrenceCreationPreparation preparation =
            new RideOccurrenceCreationPreparation(
                "park-1",
                VisitDate.ForDay(2026, 9, 3),
                "Europe/Paris",
                LocalServiceDayConvention.VisitStartLocalDate,
                new[] { HistoricalConsistency.Verified });
        occurrences.Setup(repository => repository.ResolveExistingBatchCreationAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync((IdempotentRideOccurrenceCreationResult?)null);
        occurrences.Setup(repository => repository.ResolveBatchCreationKeyReservationAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync(new RideOccurrenceCreationKeyReservationResult(
                RideOccurrenceCreationKeyReservationStatus.Replayed,
                preparation));
        occurrences.Setup(repository => repository.GetAppendStateAsync(
                VisitId.Parse("visit-1"),
                "owner-1",
                "request-1",
                CancellationToken.None))
            .ReturnsAsync(new RideOccurrenceAppendState(null, false));
        occurrences.Setup(repository => repository.CreateBatchIdempotentAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                It.IsAny<IReadOnlyList<RideOccurrence>>(),
                null,
                false,
                "request-1",
                CancellationToken.None))
            .ReturnsAsync((
                RideOccurrenceCreationRequest _,
                IReadOnlyList<RideOccurrence> created,
                long? _,
                bool _,
                string _,
                CancellationToken _) => new IdempotentRideOccurrenceCreationResult(
                    IdempotentRideOccurrenceCreationStatus.Created,
                    created));
        AddRideOccurrencesBatchCommandHandler handler = CreateAddHandler(
            visits,
            occurrences,
            targets,
            CreateClock());

        ApplicationResult<CreateRideOccurrencesResult> result = await handler.HandleAsync(
            CreateBatchCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(
            HistoricalConsistency.Verified,
            Assert.Single(result.Value!.Occurrences).HistoricalConsistency);
        occurrences.VerifyAll();
        visits.VerifyNoOtherCalls();
        targets.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddBatch_WithStaleReservedVisitIdentity_ShouldRejectUnderLease()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IVisitTargetResolver> targets =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        Mock<IPassportAuditPublisher> audit =
            new Mock<IPassportAuditPublisher>(MockBehavior.Strict);
        Mock<IVisitContentMutationLeaseManager> leases =
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict);
        Mock<IVisitContentMutationLease> lease =
            new Mock<IVisitContentMutationLease>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        RideOccurrenceCreationPreparation stalePreparation =
            new RideOccurrenceCreationPreparation(
                visit.ParkId,
                VisitDate.ForDay(2026, 9, 2),
                visit.TimeZoneId,
                visit.ServiceDayConvention,
                new[] { HistoricalConsistency.Verified });
        occurrences.Setup(repository => repository.ResolveExistingBatchCreationAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync((IdempotentRideOccurrenceCreationResult?)null);
        occurrences.Setup(repository => repository.ResolveBatchCreationKeyReservationAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync(new RideOccurrenceCreationKeyReservationResult(
                RideOccurrenceCreationKeyReservationStatus.Replayed,
                stalePreparation));
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc.AddMinutes(1));
        leases.Setup(value => value.TryAcquireAsync(
                visit,
                NowUtc.AddMinutes(1),
                CancellationToken.None))
            .ReturnsAsync(lease.Object);
        lease.SetupGet(value => value.LeaseLostToken).Returns(CancellationToken.None);
        lease.SetupGet(value => value.ContentFenceToken).Returns(7);
        lease.Setup(value => value.MarkMutationCompleted());
        lease.Setup(value => value.DisposeAsync()).Returns(ValueTask.CompletedTask);
        AddRideOccurrencesBatchCommandHandler handler =
            new AddRideOccurrencesBatchCommandHandler(
                visits.Object,
                occurrences.Object,
                targets.Object,
                new RideOccurrenceAppendOrderNormalizer(occurrences.Object, clock.Object),
                clock.Object,
                audit.Object,
                leases.Object);

        ApplicationResult<CreateRideOccurrencesResult> result = await handler.HandleAsync(
            CreateBatchCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal("ride-occurrence.version-conflict", Assert.Single(result.Errors).Code);
        visits.VerifyAll();
        occurrences.VerifyAll();
        targets.VerifyNoOtherCalls();
        audit.VerifyNoOtherCalls();
        leases.VerifyAll();
        lease.VerifyAll();
        clock.VerifyAll();
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
        SetupMissingCreationKeyReservation(occurrences);
        AddRideOccurrencesBatchCommandHandler handler = CreateAddHandler(
            visits,
            occurrences,
            targets,
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
    public async Task AddBatch_WithConfirmedHistoricalConflict_ShouldFingerprintTheConfirmation()
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
                It.Is<RideOccurrenceCreationRequest>(request =>
                    Assert.Single(request.Items).ConfirmHistoricalConflict),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync((IdempotentRideOccurrenceCreationResult?)null);
        SetupCreationKeyReservation(occurrences);
        occurrences.Setup(repository => repository.GetAppendStateAsync(
                visit.Id,
                visit.UserId,
                "request-1",
                CancellationToken.None))
            .ReturnsAsync(new RideOccurrenceAppendState(null, false));
        occurrences.Setup(repository => repository.CreateBatchIdempotentAsync(
                It.Is<RideOccurrenceCreationRequest>(request =>
                    Assert.Single(request.Items).ConfirmHistoricalConflict),
                It.IsAny<IReadOnlyList<RideOccurrence>>(),
                null,
                false,
                "request-1",
                CancellationToken.None))
            .ReturnsAsync((
                RideOccurrenceCreationRequest _,
                IReadOnlyList<RideOccurrence> created,
                long? _,
                bool _,
                string _,
                CancellationToken _) => new IdempotentRideOccurrenceCreationResult(
                    IdempotentRideOccurrenceCreationStatus.Created,
                    created));
        AddRideOccurrencesBatchCommandHandler handler = CreateAddHandler(
            visits,
            occurrences,
            targets,
            CreateClock());

        ApplicationResult<CreateRideOccurrencesResult> result = await handler.HandleAsync(
            CreateBatchCommand(confirmHistoricalConflict: true));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            HistoricalConsistency.ConfirmedConflict,
            Assert.Single(result.Value!.Occurrences).HistoricalConsistency);
        occurrences.VerifyAll();
        visits.VerifyAll();
        targets.VerifyAll();
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
    public async Task Update_WithNoChangedFields_ShouldStillFenceTheLoadedVersion()
    {
        Visit visit = CreateVisit();
        RideOccurrence occurrence = CreateOccurrence(visit, "occurrence-1", 1024);
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        occurrences.Setup(repository => repository.GetOwnedAsync(
                occurrence.Id,
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(occurrence);
        occurrences.Setup(repository => repository.TryConfirmOwnedVersionAsync(
                occurrence.Id,
                visit.Id,
                visit.UserId,
                1,
                CancellationToken.None))
            .ReturnsAsync(false);
        Mock<IVisitTargetResolver> targets = CreateTargetResolver(
            new VisitTarget(
                occurrence.ParkItemId,
                visit.ParkId,
                "Attraction",
                ParkItemCategory.Attraction,
                new DateOnly(2000, 1, 1),
                null));
        UpdateRideOccurrenceCommandHandler handler = new UpdateRideOccurrenceCommandHandler(
            visits.Object,
            occurrences.Object,
            targets.Object,
            CreateClock());

        ApplicationResult<RideOccurrenceResult> result = await handler.HandleAsync(
            new UpdateRideOccurrenceCommand(
                visit.UserId,
                visit.Id.Value,
                occurrence.Id.Value,
                1,
                null,
                false,
                RideOccurrenceStatus.Completed,
                null,
                false));

        Assert.False(result.IsSuccess);
        Assert.Equal("ride-occurrence.version-conflict", Assert.Single(result.Errors).Code);
        Assert.Equal(1, occurrence.Version);
        visits.VerifyAll();
        occurrences.VerifyAll();
        targets.VerifyAll();
    }

    [Theory]
    [InlineData(0, RideOccurrenceStatus.Completed)]
    [InlineData(1, (RideOccurrenceStatus)0)]
    public async Task Update_WithMalformedFields_ShouldReturnValidationBeforePersistence(
        long expectedVersion,
        RideOccurrenceStatus status)
    {
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
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
                expectedVersion,
                null,
                false,
                status,
                null,
                false));

        Assert.False(result.IsSuccess);
        Assert.Equal("ride-occurrence.update-invalid", Assert.Single(result.Errors).Code);
        visits.VerifyNoOtherCalls();
        occurrences.VerifyNoOtherCalls();
        targets.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldUseTheSerializedDeletionPort()
    {
        Visit visit = CreateVisit();
        RideOccurrence occurrence = CreateOccurrence(visit, "occurrence-1", 1024);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        occurrences.Setup(repository => repository.GetOwnedAsync(
                occurrence.Id,
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(occurrence);
        occurrences.Setup(repository => repository.TryDeleteOwnedAsync(
                It.Is<RideOccurrence>(item => item.IsDeleted && item.Version == 2),
                1,
                CancellationToken.None))
            .ReturnsAsync(true);
        DeleteRideOccurrenceCommandHandler handler = new DeleteRideOccurrenceCommandHandler(
            occurrences.Object,
            CreateClock());

        ApplicationResult<RideOccurrenceResult> result = await handler.HandleAsync(
            new DeleteRideOccurrenceCommand(
                visit.UserId,
                visit.Id.Value,
                occurrence.Id.Value,
                1));

        Assert.True(result.IsSuccess);
        Assert.Equal("occurrence-1", result.Value?.Id);
        occurrences.VerifyAll();
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
    public async Task Reorder_WhenPendingOperationIsRetriedAfterCompletion_ShouldNotResumeIt()
    {
        Visit visit = CreateVisit();
        visit.Complete(new DateOnly(2026, 9, 3), NowUtc.AddMinutes(1));
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IPassportAuditPublisher> audit =
            new Mock<IPassportAuditPublisher>(MockBehavior.Strict);
        Mock<IVisitContentMutationLeaseManager> leases =
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict);
        ReorderRideOccurrenceCommandHandler handler =
            new ReorderRideOccurrenceCommandHandler(
                visits.Object,
                occurrences.Object,
                CreateClock(),
                audit.Object,
                leases.Object);

        ApplicationResult<ReorderRideOccurrenceResult> result = await handler.HandleAsync(
            new ReorderRideOccurrenceCommand(
                "owner-1",
                "visit-1",
                "request-1",
                "occurrence-1",
                1,
                "occurrence-2",
                RideOccurrencePlacement.Before));

        Assert.False(result.IsSuccess);
        Assert.Equal("visit.not-editable", Assert.Single(result.Errors).Code);
        visits.VerifyAll();
        occurrences.VerifyNoOtherCalls();
        audit.VerifyNoOtherCalls();
        leases.VerifyNoOtherCalls();
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
                It.Is<IReadOnlyCollection<RideOccurrenceOrderGuard>>(guards =>
                    guards.Count == 3
                    && guards.Any(guard => guard.OccurrenceId == first.Id)
                    && guards.Any(guard => guard.OccurrenceId == second.Id)
                    && guards.Any(guard => guard.OccurrenceId == moved.Id)),
                It.Is<RideOccurrence>(item => item.Id == moved.Id),
                false,
                NowUtc.AddMinutes(1),
                "request-1",
                null,
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
    public async Task Reorder_WhenKeyIsReusedForAnotherPayload_ShouldReportIdempotencyConflict()
    {
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        occurrences.Setup(repository => repository.ResolveExistingReorderAsync(
                It.IsAny<RideOccurrenceReorderRequest>(),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync(new IdempotentRideOccurrenceReorderResult(
                IdempotentRideOccurrenceReorderStatus.IdempotencyConflict,
                null,
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

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "ride-occurrence.idempotency-key-conflict",
            Assert.Single(result.Errors).Code);
        occurrences.VerifyAll();
        visits.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_ShouldScopeTheOccurrenceToOwnerAndVisit()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        RideOccurrence occurrence = CreateOccurrence(visit, "occurrence-1", 1024);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        occurrences.Setup(repository => repository.GetOwnedAsync(
                occurrence.Id,
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(occurrence);
        VisitTarget target = new VisitTarget(
            occurrence.ParkItemId,
            visit.ParkId,
            "Current ride name",
            ParkItemCategory.Attraction,
            null,
            null,
            "Operating");
        Mock<IVisitTargetResolver> targets = CreateTargetResolver(target);
        GetRideOccurrenceQueryHandler handler = new GetRideOccurrenceQueryHandler(
            visits.Object,
            occurrences.Object,
            targets.Object);

        ApplicationResult<RideOccurrenceResult> result = await handler.HandleAsync(
            new GetRideOccurrenceQuery(
                visit.UserId,
                visit.Id.Value,
                occurrence.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(occurrence.Id.Value, result.Value?.Id);
        Assert.Equal(occurrence.PrivateNote, result.Value?.PrivateNote);
        Assert.Equal("Current ride name", result.Value?.Target?.Name);
        Assert.Equal("Operating", result.Value?.Target?.LifecycleStatus);
        Assert.False(result.Value!.Target!.IsHistoricalSnapshot);
        visits.VerifyAll();
        occurrences.VerifyAll();
        targets.VerifyAll();
    }

    [Fact]
    public async Task Get_WhenOccurrenceIsOutsideOwnerScope_ShouldReturnNotFound()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        occurrences.Setup(repository => repository.GetOwnedAsync(
                RideOccurrenceId.Parse("occurrence-1"),
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync((RideOccurrence?)null);
        Mock<IVisitTargetResolver> targets =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        GetRideOccurrenceQueryHandler handler = new GetRideOccurrenceQueryHandler(
            visits.Object,
            occurrences.Object,
            targets.Object);

        ApplicationResult<RideOccurrenceResult> result = await handler.HandleAsync(
            new GetRideOccurrenceQuery("owner-1", "visit-1", "occurrence-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal("ride-occurrence.not-found", Assert.Single(result.Errors).Code);
        visits.VerifyAll();
        occurrences.VerifyAll();
        targets.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_WhenParentVisitIsDeleted_ShouldNotReadTheOccurrence()
    {
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        visits.Setup(repository => repository.GetOwnedAsync(
                VisitId.Parse("visit-1"),
                "owner-1",
                CancellationToken.None))
            .ReturnsAsync((Visit?)null);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IVisitTargetResolver> targets =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        GetRideOccurrenceQueryHandler handler = new GetRideOccurrenceQueryHandler(
            visits.Object,
            occurrences.Object,
            targets.Object);

        ApplicationResult<RideOccurrenceResult> result = await handler.HandleAsync(
            new GetRideOccurrenceQuery("owner-1", "visit-1", "occurrence-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal("ride-occurrence.not-found", Assert.Single(result.Errors).Code);
        visits.VerifyAll();
        occurrences.VerifyNoOtherCalls();
        targets.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_WhenLiveTargetNoLongerExists_ShouldUseHistoricalSnapshot()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        RideOccurrence occurrence = CreateOccurrence(
            visit,
            "occurrence-1",
            1024,
            new HistoricalTargetReference("Former ride name", "Attraction"));
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        occurrences.Setup(repository => repository.GetOwnedAsync(
                occurrence.Id,
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(occurrence);
        Mock<IVisitTargetResolver> targets =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        targets.Setup(resolver => resolver.ResolveAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Single() == occurrence.ParkItemId),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, VisitTarget>());
        GetRideOccurrenceQueryHandler handler = new GetRideOccurrenceQueryHandler(
            visits.Object,
            occurrences.Object,
            targets.Object);

        ApplicationResult<RideOccurrenceResult> result = await handler.HandleAsync(
            new GetRideOccurrenceQuery(visit.UserId, visit.Id.Value, occurrence.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal("Former ride name", result.Value?.Target?.Name);
        Assert.Equal("Attraction", result.Value?.Target?.Category);
        Assert.True(result.Value!.Target!.IsHistoricalSnapshot);
        visits.VerifyAll();
        occurrences.VerifyAll();
        targets.VerifyAll();
    }

    [Fact]
    public async Task Get_WhenLiveTargetMovedToAnotherPark_ShouldUseHistoricalSnapshot()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        RideOccurrence occurrence = CreateOccurrence(
            visit,
            "occurrence-1",
            1024,
            new HistoricalTargetReference("Original ride name", "Attraction"));
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        occurrences.Setup(repository => repository.GetOwnedAsync(
                occurrence.Id,
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(occurrence);
        VisitTarget movedTarget = new VisitTarget(
            occurrence.ParkItemId,
            "another-park",
            "Moved ride name",
            ParkItemCategory.Attraction,
            null,
            null,
            "Operating");
        Mock<IVisitTargetResolver> targets = CreateTargetResolver(movedTarget);
        GetRideOccurrenceQueryHandler handler = new GetRideOccurrenceQueryHandler(
            visits.Object,
            occurrences.Object,
            targets.Object);

        ApplicationResult<RideOccurrenceResult> result = await handler.HandleAsync(
            new GetRideOccurrenceQuery(visit.UserId, visit.Id.Value, occurrence.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal("Original ride name", result.Value?.Target?.Name);
        Assert.Equal("Attraction", result.Value?.Target?.Category);
        Assert.True(result.Value!.Target!.IsHistoricalSnapshot);
        visits.VerifyAll();
        occurrences.VerifyAll();
        targets.VerifyAll();
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
        VisitTarget target = new VisitTarget(
            occurrence.ParkItemId,
            visit.ParkId,
            "Batch-resolved ride",
            ParkItemCategory.Attraction,
            new DateOnly(2000, 1, 1),
            new DateOnly(2020, 12, 31),
            "ClosedDefinitively");
        Mock<IVisitTargetResolver> targets = CreateTargetResolver(target);
        ListRideOccurrencesQueryHandler handler = new ListRideOccurrencesQueryHandler(
            visits.Object,
            occurrences.Object,
            targets.Object);

        ApplicationResult<RideOccurrencePageResult> result = await handler.HandleAsync(
            new ListRideOccurrencesQuery("owner-1", "visit-1", 25));

        Assert.True(result.IsSuccess);
        RideOccurrenceResult item = Assert.Single(result.Value!.Items);
        Assert.Equal("occurrence-1", item.Id);
        Assert.Equal("Batch-resolved ride", item.Target?.Name);
        Assert.Equal("ClosedDefinitively", item.Target?.LifecycleStatus);
        Assert.Equal(HistoricalConsistency.ConfirmedConflict, item.HistoricalConsistency);
        Assert.False(item.HistoricalConflictConfirmed);
        visits.VerifyAll();
        occurrences.VerifyAll();
        targets.VerifyAll();
    }

    [Fact]
    public void ResultFactory_ShouldRequireFreshConfirmationWhenCurrentEvidenceIsResolved()
    {
        Visit visit = CreateVisit();
        RideOccurrence occurrence = RideOccurrence.Create(
            RideOccurrenceId.Parse("occurrence-confirmed"),
            visit,
            "item-confirmed",
            1024,
            new OccurrenceMoment(null, false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.ConfirmedConflict,
            null,
            null,
            NowUtc);
        VisitTarget target = new VisitTarget(
            occurrence.ParkItemId,
            visit.ParkId,
            "Attraction confirmée auparavant",
            ParkItemCategory.Attraction,
            new DateOnly(2027, 1, 1),
            null,
            "Operating");

        RideOccurrenceResult storedResult = PassportRideOccurrenceResultFactory.Create(occurrence);
        RideOccurrenceResult refreshedResult = PassportRideOccurrenceResultFactory.Create(
            occurrence,
            target,
            visit.Date);

        Assert.True(storedResult.HistoricalConflictConfirmed);
        Assert.Equal(HistoricalConsistency.ConfirmedConflict, refreshedResult.HistoricalConsistency);
        Assert.False(refreshedResult.HistoricalConflictConfirmed);
    }

    [Fact]
    public void ResultFactory_ShouldNotExposeCurrentEvidenceForHiddenTarget()
    {
        Visit visit = CreateVisit();
        RideOccurrence occurrence = CreateOccurrence(
            visit,
            "occurrence-hidden",
            1024,
            new HistoricalTargetReference("Nom conservé dans la visite", "Attraction"));
        VisitTarget hiddenTarget = new VisitTarget(
            occurrence.ParkItemId,
            visit.ParkId,
            "Nom courant masqué",
            ParkItemCategory.Attraction,
            new DateOnly(2027, 1, 1),
            new DateOnly(2028, 12, 31),
            "Operating",
            false);

        RideOccurrenceResult result = PassportRideOccurrenceResultFactory.Create(
            occurrence,
            hiddenTarget,
            visit.Date);

        Assert.Equal(HistoricalConsistency.Verified, result.HistoricalConsistency);
        RideOccurrenceTargetResult targetResult = Assert.IsType<RideOccurrenceTargetResult>(result.Target);
        Assert.True(targetResult.IsHistoricalSnapshot);
        Assert.Equal("Nom conservé dans la visite", targetResult.Name);
        Assert.Null(targetResult.OpeningDate);
        Assert.Null(targetResult.ClosingDate);
    }

    private static AddRideOccurrencesBatchCommand CreateBatchCommand(
        int count = 1,
        bool confirmHistoricalConflict = false,
        RideLogSource source = RideLogSource.Manual)
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
                    confirmHistoricalConflict,
                    count),
            },
            source);
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

    private static AddRideOccurrencesBatchCommandHandler CreateAddHandler(
        Mock<IUserVisitRepository> visits,
        Mock<IRideOccurrenceRepository> occurrences,
        Mock<IVisitTargetResolver> targets,
        IPassportClock clock)
    {
        return new AddRideOccurrencesBatchCommandHandler(
            visits.Object,
            occurrences.Object,
            targets.Object,
            new RideOccurrenceAppendOrderNormalizer(occurrences.Object, clock),
            clock);
    }

    private static void SetupCreationKeyReservation(
        Mock<IRideOccurrenceRepository> occurrences)
    {
        SetupMissingCreationKeyReservation(occurrences);
        occurrences.Setup(repository => repository.ReserveBatchCreationKeyAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                It.IsAny<RideOccurrenceCreationPreparation>(),
                "request-1",
                NowUtc.AddMinutes(1),
                CancellationToken.None))
            .ReturnsAsync((
                RideOccurrenceCreationRequest _,
                RideOccurrenceCreationPreparation preparation,
                string _,
                DateTime _,
                CancellationToken _) => new RideOccurrenceCreationKeyReservationResult(
                    RideOccurrenceCreationKeyReservationStatus.Reserved,
                    preparation));
    }

    private static void SetupMissingCreationKeyReservation(
        Mock<IRideOccurrenceRepository> occurrences)
    {
        occurrences.Setup(repository => repository.ResolveBatchCreationKeyReservationAsync(
                It.IsAny<RideOccurrenceCreationRequest>(),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync(new RideOccurrenceCreationKeyReservationResult(
                RideOccurrenceCreationKeyReservationStatus.Missing));
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
        long position,
        HistoricalTargetReference? historicalTarget = null)
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
            historicalTarget,
            null,
            NowUtc);
    }
}
