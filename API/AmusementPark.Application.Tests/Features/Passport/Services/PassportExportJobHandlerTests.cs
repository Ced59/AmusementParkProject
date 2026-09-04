using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Services;

public sealed class PassportExportJobHandlerTests
{
    [Fact]
    public async Task HandleAsync_LoadsOnlyOwnerDataAndPersistsGeneratedArtifact()
    {
        DateTime nowUtc = new DateTime(2026, 9, 4, 14, 0, 0, DateTimeKind.Utc);
        string exportId = "0123456789abcdef0123456789abcdef";
        PassportExport passportExport = new PassportExport(
            exportId,
            "user-1",
            PassportExportFormat.Json,
            PassportExportStatus.Pending,
            1,
            nowUtc,
            nowUtc,
            nowUtc.AddHours(1));
        Visit visit = Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForYear(2025),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            nowUtc);
        Visit deletedVisit = Visit.Create(
            VisitId.Parse("visit-deleted"),
            "user-1",
            "park-deleted",
            VisitDate.ForYear(2024),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            nowUtc);
        RideOccurrence deletedVisitOccurrence = RideOccurrence.Create(
            RideOccurrenceId.Parse("occurrence-deleted"),
            deletedVisit,
            "item-deleted",
            RideOccurrence.SortPositionStep,
            new OccurrenceMoment(null, false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            null,
            nowUtc);
        PassportExportArtifact artifact = new PassportExportArtifact(
            "passport.json",
            "application/json",
            new byte[] { 1, 2, 3 },
            1,
            new string('a', 64));
        Mock<IPassportExportRepository> exports = new Mock<IPassportExportRepository>(MockBehavior.Strict);
        exports.Setup(repository => repository.GetOwnedAsync(exportId, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(passportExport);
        exports.Setup(repository => repository.TryMarkProcessingAsync(exportId, "user-1", nowUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        exports.Setup(repository => repository.TryCompleteAsync(
                exportId,
                "user-1",
                artifact,
                nowUtc,
                nowUtc.Add(PassportExportJob.Retention),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        PassportExportSourceBudget? observedSourceBudget = null;
        Mock<IUserVisitRepository> visits = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        visits.Setup(repository => repository.ListAllOwnedForExportAsync(
                "user-1",
                It.Is<PassportExportSourceBudget>(budget =>
                    budget.MaximumBytes == PassportExportJob.MaximumSourceBytes),
                It.IsAny<CancellationToken>()))
            .Callback<string, PassportExportSourceBudget, CancellationToken>(
                (_, budget, _) => observedSourceBudget = budget)
            .ReturnsAsync(new[] { visit });
        Mock<IRideOccurrenceRepository> occurrences = new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        occurrences.Setup(repository => repository.ListAllOwnedForExportAsync(
                "user-1",
                It.Is<PassportExportSourceBudget>(budget =>
                    ReferenceEquals(budget, observedSourceBudget)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { deletedVisitOccurrence });
        Park park = new Park { Id = "park-1", Name = "Test Park" };
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { park });
        Mock<IVisitTargetResolver> targets = new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        targets.Setup(resolver => resolver.ResolveAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, VisitTarget>());
        Mock<IVisitExportWriter> writer = new Mock<IVisitExportWriter>(MockBehavior.Strict);
        writer.Setup(value => value.Write(It.Is<PassportExportWriteRequest>(request =>
                request.ExportId == exportId
                && request.Visits.Count == 1
                && request.RideOccurrences.Count == 0
                && request.Parks["park-1"].Name == "Test Park")))
            .Returns(artifact);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(nowUtc);
        PassportExportJobHandler handler = new PassportExportJobHandler(
            exports.Object,
            visits.Object,
            occurrences.Object,
            parks.Object,
            targets.Object,
            writer.Object,
            clock.Object);
        PassportExportJobPayload payload = new PassportExportJobPayload(
            exportId,
            "user-1",
            PassportExportFormat.Json);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            new DurableBackgroundJobExecutionContext(
                "job-1",
                PassportExportJob.PayloadVersion,
                JsonSerializer.SerializeToElement(payload),
                null,
                1,
                null),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        exports.VerifyAll();
        visits.VerifyAll();
        occurrences.VerifyAll();
        parks.VerifyAll();
        targets.VerifyAll();
        writer.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenSourceBudgetIsExceeded_MarksExportAsFailed()
    {
        DateTime nowUtc = new DateTime(2026, 9, 4, 14, 0, 0, DateTimeKind.Utc);
        string exportId = "0123456789abcdef0123456789abcdef";
        PassportExport passportExport = new PassportExport(
            exportId,
            "user-1",
            PassportExportFormat.Csv,
            PassportExportStatus.Pending,
            1,
            nowUtc,
            nowUtc,
            nowUtc.AddHours(1));
        Mock<IPassportExportRepository> exports =
            new Mock<IPassportExportRepository>(MockBehavior.Strict);
        exports.Setup(repository => repository.GetOwnedAsync(
                exportId,
                "user-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(passportExport);
        exports.Setup(repository => repository.TryMarkProcessingAsync(
                exportId,
                "user-1",
                nowUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        exports.Setup(repository => repository.TryFailAsync(
                exportId,
                "user-1",
                PassportExportErrorCodes.TooLarge,
                nowUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        visits.Setup(repository => repository.ListAllOwnedForExportAsync(
                "user-1",
                It.IsAny<PassportExportSourceBudget>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PassportExportSizeLimitException());
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(nowUtc);
        PassportExportJobHandler handler = new PassportExportJobHandler(
            exports.Object,
            visits.Object,
            Mock.Of<IRideOccurrenceRepository>(MockBehavior.Strict),
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IVisitTargetResolver>(MockBehavior.Strict),
            Mock.Of<IVisitExportWriter>(MockBehavior.Strict),
            clock.Object);
        PassportExportJobPayload payload = new PassportExportJobPayload(
            exportId,
            "user-1",
            PassportExportFormat.Csv);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            new DurableBackgroundJobExecutionContext(
                "job-1",
                PassportExportJob.PayloadVersion,
                JsonSerializer.SerializeToElement(payload),
                null,
                1,
                null),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.DeadLetter, result.Outcome);
        Assert.Equal(PassportExportErrorCodes.TooLarge, result.ErrorCode);
        exports.VerifyAll();
        visits.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_RejectsInvalidOpaqueExportIdentifier()
    {
        PassportExportJobHandler handler = new PassportExportJobHandler(
            Mock.Of<IPassportExportRepository>(MockBehavior.Strict),
            Mock.Of<IUserVisitRepository>(MockBehavior.Strict),
            Mock.Of<IRideOccurrenceRepository>(MockBehavior.Strict),
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IVisitTargetResolver>(MockBehavior.Strict),
            Mock.Of<IVisitExportWriter>(MockBehavior.Strict),
            Mock.Of<IPassportClock>(MockBehavior.Strict));
        PassportExportJobPayload payload = new PassportExportJobPayload(
            "sequential-id",
            "user-1",
            PassportExportFormat.Json);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            new DurableBackgroundJobExecutionContext(
                "job-1",
                PassportExportJob.PayloadVersion,
                JsonSerializer.SerializeToElement(payload),
                null,
                1,
                null),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.DeadLetter, result.Outcome);
        Assert.Equal(PassportExportErrorCodes.InvalidPayload, result.ErrorCode);
    }
}
