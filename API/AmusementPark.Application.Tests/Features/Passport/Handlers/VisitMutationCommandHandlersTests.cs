using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class VisitMutationCommandHandlersTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 4, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task UpdateMetadata_ShouldPersistTheCorrectionAndItsMinimizedAuditProofAtomically()
    {
        Visit visit = CreateVisit(VisitStatus.Draft);
        Mock<IUserVisitRepository> visits = CreateOwnedRepository(visit);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        occurrences.Setup(repository => repository.ListOwnedByVisitAsync(
                It.Is<RideOccurrenceListCriteria>(criteria =>
                    criteria.VisitId == visit.Id
                    && criteria.UserId == visit.UserId
                    && criteria.Limit == 1),
                CancellationToken.None))
            .ReturnsAsync(new RideOccurrencePage(Array.Empty<RideOccurrence>(), null));
        Mock<IVisitContentMutationLease> lease =
            new Mock<IVisitContentMutationLease>(MockBehavior.Strict);
        lease.SetupGet(value => value.Token).Returns("lease-1");
        lease.SetupGet(value => value.LeaseLostToken).Returns(CancellationToken.None);
        lease.Setup(value => value.DisposeAsync()).Returns(ValueTask.CompletedTask);
        Mock<IVisitContentMutationLeaseManager> leases =
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict);
        leases.Setup(manager => manager.TryAcquireAsync(
                visit,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(lease.Object);
        Mock<IPassportAuditPublisher> audit = CreateAuditPublisher();
        PassportAuditEvent? capturedAudit = null;
        visits.Setup(repository => repository.TryUpdateOwnedAuditedWithinContentMutationLeaseAsync(
                visit,
                1,
                It.IsAny<PassportAuditEvent>(),
                "lease-1",
                CancellationToken.None))
            .Callback((
                Visit _,
                long _,
                PassportAuditEvent auditEvent,
                string _,
                CancellationToken _) => capturedAudit = auditEvent)
            .ReturnsAsync(true);
        Mock<IPassportTimeZoneValidator> timeZones = new Mock<IPassportTimeZoneValidator>(MockBehavior.Strict);
        timeZones.Setup(validator => validator.IsValid("Europe/Paris")).Returns(true);
        UpdateVisitMetadataCommandHandler handler = new UpdateVisitMetadataCommandHandler(
            visits.Object,
            occurrences.Object,
            leases.Object,
            CreateClock().Object,
            timeZones.Object,
            audit.Object);

        ApplicationResult<VisitResult> result = await handler.HandleAsync(
            new UpdateVisitMetadataCommand(
                "owner-1", "visit-1", 2025, null, null, VisitDatePrecision.Year, true,
                "Europe/Paris", LocalServiceDayConvention.VisitStartLocalDate,
                "Souvenir", "texte qui ne doit pas être audité", 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value?.Version);
        Assert.NotNull(capturedAudit);
        Assert.Equal(PassportAuditEventType.VisitDateChanged, capturedAudit.EventType);
        Assert.Equal(VisitDate.ForDay(2026, 9, 3), capturedAudit.PreviousVisitDate);
        Assert.Equal(VisitDate.ForYear(2025, true), capturedAudit.NewVisitDate);
        Assert.True(capturedAudit.PrivateTextChanged);
        Assert.Contains(PassportAuditChangedField.PrivateNote, capturedAudit.ChangedFields);
        visits.VerifyAll();
        occurrences.VerifyAll();
        leases.VerifyAll();
        lease.VerifyGet(value => value.Token, Times.Once);
        lease.Verify(value => value.DisposeAsync(), Times.Once);
        audit.VerifyAll();
    }

    [Fact]
    public async Task UpdateMetadata_WhenTemporalIdentityChangesWithOccurrences_ShouldRejectTheCorrection()
    {
        Visit visit = CreateVisit(VisitStatus.Draft);
        RideOccurrence occurrence = RideOccurrence.Create(
            RideOccurrenceId.Parse("occurrence-1"),
            visit,
            "item-1",
            1024,
            new OccurrenceMoment(null, false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            null,
            NowUtc);
        Mock<IUserVisitRepository> visits = CreateOwnedRepository(visit);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        occurrences.Setup(repository => repository.ListOwnedByVisitAsync(
                It.Is<RideOccurrenceListCriteria>(criteria =>
                    criteria.VisitId == visit.Id
                    && criteria.UserId == visit.UserId
                    && criteria.Limit == 1),
                CancellationToken.None))
            .ReturnsAsync(new RideOccurrencePage(new[] { occurrence }, null));
        Mock<IPassportAuditPublisher> audit =
            new Mock<IPassportAuditPublisher>(MockBehavior.Strict);
        Mock<IVisitContentMutationLease> lease =
            new Mock<IVisitContentMutationLease>(MockBehavior.Strict);
        lease.SetupGet(value => value.LeaseLostToken).Returns(CancellationToken.None);
        lease.Setup(value => value.DisposeAsync()).Returns(ValueTask.CompletedTask);
        Mock<IVisitContentMutationLeaseManager> leases =
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict);
        leases.Setup(manager => manager.TryAcquireAsync(
                visit,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(lease.Object);
        Mock<IPassportTimeZoneValidator> timeZones =
            new Mock<IPassportTimeZoneValidator>(MockBehavior.Strict);
        timeZones.Setup(validator => validator.IsValid("Europe/Paris")).Returns(true);
        UpdateVisitMetadataCommandHandler handler = new UpdateVisitMetadataCommandHandler(
            visits.Object,
            occurrences.Object,
            leases.Object,
            CreateClock().Object,
            timeZones.Object,
            audit.Object);

        ApplicationResult<VisitResult> result = await handler.HandleAsync(
            new UpdateVisitMetadataCommand(
                "owner-1", "visit-1", 2025, null, null, VisitDatePrecision.Year, true,
                "Europe/Paris", LocalServiceDayConvention.VisitStartLocalDate,
                "Souvenir", "Privé", 1));

        Assert.False(result.IsSuccess);
        Assert.Equal("visit.temporal-metadata-locked", Assert.Single(result.Errors).Code);
        Assert.Equal(VisitDate.ForDay(2026, 9, 3), visit.Date);
        visits.VerifyAll();
        occurrences.VerifyAll();
        leases.VerifyAll();
        lease.Verify(value => value.DisposeAsync(), Times.Once);
        audit.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Complete_ShouldUseTheParkLocalDateAndAuditTheStatusTransition()
    {
        Visit visit = CreateVisit(VisitStatus.Draft);
        Mock<IUserVisitRepository> visits = CreateOwnedRepository(visit);
        Mock<IPassportAuditPublisher> audit = CreateAuditPublisher();
        Mock<IPassportPendingMutationReconciler> pendingMutations =
            new Mock<IPassportPendingMutationReconciler>(MockBehavior.Strict);
        pendingMutations.Setup(reconciler => reconciler.ReconcileBeforeLifecycleTransitionAsync(
                visit,
                CancellationToken.None))
            .ReturnsAsync(true);
        visits.Setup(repository => repository.TryUpdateOwnedAuditedAsync(
                visit,
                1,
                It.Is<PassportAuditEvent>(auditEvent =>
                    auditEvent.EventType == PassportAuditEventType.VisitCompleted
                    && auditEvent.PreviousVisitStatus == VisitStatus.Draft
                    && auditEvent.NewVisitStatus == VisitStatus.Completed),
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IPassportLocalDateResolver> localDate = new Mock<IPassportLocalDateResolver>(MockBehavior.Strict);
        localDate.Setup(resolver => resolver.Resolve(NowUtc, "Europe/Paris"))
            .Returns(new DateOnly(2026, 9, 4));
        CompleteVisitCommandHandler handler = new CompleteVisitCommandHandler(
            visits.Object,
            CreateClock().Object,
            localDate.Object,
            audit.Object,
            pendingMutations.Object);

        ApplicationResult<VisitResult> result = await handler.HandleAsync(
            new CompleteVisitCommand("owner-1", "visit-1", 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(VisitStatus.Completed, result.Value?.Status);
        Assert.Equal(NowUtc, result.Value?.CompletedAtUtc);
        visits.VerifyAll();
        localDate.VerifyAll();
        audit.VerifyAll();
        pendingMutations.VerifyAll();
    }

    [Fact]
    public async Task Complete_WhenPendingContentCannotBeReconciled_ShouldKeepDraft()
    {
        Visit visit = CreateVisit(VisitStatus.Draft);
        Mock<IUserVisitRepository> visits = CreateOwnedRepository(visit);
        Mock<IPassportAuditPublisher> audit =
            new Mock<IPassportAuditPublisher>(MockBehavior.Strict);
        Mock<IPassportPendingMutationReconciler> pendingMutations =
            new Mock<IPassportPendingMutationReconciler>(MockBehavior.Strict);
        pendingMutations.Setup(reconciler => reconciler.ReconcileBeforeLifecycleTransitionAsync(
                visit,
                CancellationToken.None))
            .ReturnsAsync(false);
        Mock<IPassportLocalDateResolver> localDate =
            new Mock<IPassportLocalDateResolver>(MockBehavior.Strict);
        CompleteVisitCommandHandler handler = new CompleteVisitCommandHandler(
            visits.Object,
            CreateClock().Object,
            localDate.Object,
            audit.Object,
            pendingMutations.Object);

        ApplicationResult<VisitResult> result = await handler.HandleAsync(
            new CompleteVisitCommand("owner-1", "visit-1", 1));

        Assert.False(result.IsSuccess);
        Assert.Equal("visit.version-conflict", Assert.Single(result.Errors).Code);
        Assert.Equal(VisitStatus.Draft, visit.Status);
        visits.VerifyAll();
        pendingMutations.VerifyAll();
        localDate.VerifyNoOtherCalls();
        audit.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateMetadata_WhenNothingChanges_ShouldStillConfirmTheExpectedVersion()
    {
        Visit visit = CreateVisit(VisitStatus.Draft);
        Mock<IUserVisitRepository> visits = CreateOwnedRepository(visit);
        visits.Setup(repository => repository.TryConfirmOwnedVersionAsync(
                visit.Id,
                visit.UserId,
                1,
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IPassportAuditPublisher> audit = new Mock<IPassportAuditPublisher>(MockBehavior.Strict);
        Mock<IPassportTimeZoneValidator> timeZones = new Mock<IPassportTimeZoneValidator>(MockBehavior.Strict);
        timeZones.Setup(validator => validator.IsValid("Europe/Paris")).Returns(true);
        UpdateVisitMetadataCommandHandler handler = new UpdateVisitMetadataCommandHandler(
            visits.Object,
            CreateClock().Object,
            timeZones.Object,
            audit.Object);

        ApplicationResult<VisitResult> result = await handler.HandleAsync(
            new UpdateVisitMetadataCommand(
                "owner-1", "visit-1", 2026, 9, 3, VisitDatePrecision.Day, false,
                "Europe/Paris", LocalServiceDayConvention.VisitStartLocalDate,
                "Journée", "Privé", 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value?.Version);
        visits.VerifyAll();
        audit.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Reopen_WhenArchived_ShouldRestoreADraftAndKeepTheSameVisitIdentity()
    {
        Visit visit = CreateVisit(VisitStatus.Archived);
        Mock<IUserVisitRepository> visits = CreateOwnedRepository(visit);
        Mock<IPassportAuditPublisher> audit = CreateAuditPublisher();
        visits.Setup(repository => repository.TryUpdateOwnedAuditedAsync(
                visit,
                1,
                It.Is<PassportAuditEvent>(auditEvent =>
                    auditEvent.EventType == PassportAuditEventType.VisitReopened
                    && auditEvent.PreviousVisitStatus == VisitStatus.Archived),
                CancellationToken.None))
            .ReturnsAsync(true);
        ReopenVisitCommandHandler handler = new ReopenVisitCommandHandler(
            visits.Object,
            CreateClock().Object,
            audit.Object);

        ApplicationResult<VisitResult> result = await handler.HandleAsync(
            new ReopenVisitCommand("owner-1", "visit-1", 1));

        Assert.True(result.IsSuccess);
        Assert.Equal("visit-1", result.Value?.Id);
        Assert.Equal(VisitStatus.Draft, result.Value?.Status);
        visits.VerifyAll();
        audit.VerifyAll();
    }

    [Fact]
    public async Task Archive_WhenVersionIsStale_ShouldFailBeforeMutation()
    {
        Visit visit = CreateVisit(VisitStatus.Draft);
        Mock<IUserVisitRepository> visits = CreateOwnedRepository(visit);
        Mock<IPassportAuditPublisher> audit = new Mock<IPassportAuditPublisher>(MockBehavior.Strict);
        Mock<IPassportPendingMutationReconciler> pendingMutations =
            new Mock<IPassportPendingMutationReconciler>(MockBehavior.Strict);
        ArchiveVisitCommandHandler handler = new ArchiveVisitCommandHandler(
            visits.Object,
            CreateClock().Object,
            audit.Object,
            pendingMutations.Object);

        ApplicationResult<VisitResult> result = await handler.HandleAsync(
            new ArchiveVisitCommand("owner-1", "visit-1", 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorType.Conflict, Assert.Single(result.Errors).Type);
        Assert.Equal("visit.version-conflict", Assert.Single(result.Errors).Code);
        Assert.Equal(VisitStatus.Draft, visit.Status);
        visits.VerifyAll();
        audit.VerifyNoOtherCalls();
        pendingMutations.VerifyNoOtherCalls();
    }

    private static Mock<IUserVisitRepository> CreateOwnedRepository(Visit visit)
    {
        Mock<IUserVisitRepository> visits = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        visits.Setup(repository => repository.GetOwnedAsync(
                VisitId.Parse("visit-1"),
                "owner-1",
                CancellationToken.None))
            .ReturnsAsync(visit);
        return visits;
    }

    private static Mock<IPassportAuditPublisher> CreateAuditPublisher()
    {
        Mock<IPassportAuditPublisher> audit = new Mock<IPassportAuditPublisher>(MockBehavior.Strict);
        audit.Setup(publisher => publisher.TryPublishAsync(
                It.IsAny<PassportAuditEvent>(),
                CancellationToken.None))
            .ReturnsAsync(true);
        return audit;
    }

    private static Mock<IPassportClock> CreateClock()
    {
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        return clock;
    }

    private static Visit CreateVisit(VisitStatus status)
    {
        DateTime? completedAtUtc = status == VisitStatus.Completed ? NowUtc : null;
        return Visit.Restore(
            VisitId.Parse("visit-1"),
            "owner-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            status,
            VisitPrivacy.Private,
            "Journée",
            "Privé",
            1,
            NowUtc.AddDays(-1),
            NowUtc,
            completedAtUtc);
    }
}
