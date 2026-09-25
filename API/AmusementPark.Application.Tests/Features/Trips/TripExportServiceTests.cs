using System.Text.Json;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripExportServiceTests
{
    [Fact]
    public async Task ExportAsync_ShouldReturnAPortablePlanWithoutTechnicalIdentifiers()
    {
        DateTime nowUtc = new(2027, 8, 12, 9, 30, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.Parse("7a74f380-4045-4e5e-8248-a232d197b6c7"),
            "account-owner-42",
            "Week-end entre amis",
            TripDateProposal.Fixed(new DateOnly(2027, 8, 20), new DateOnly(2027, 8, 21)),
            "Europe/Paris",
            nowUtc);
        TripParkCandidate candidate = TripParkCandidate.Create(
            TripParkCandidateId.Parse("076322ae-9418-45dc-aef5-cc9b045cd884"),
            trip.Id,
            "technical-park-id",
            new[] { new DateOnly(2027, 8, 20) },
            TripParkCandidateSource.Manual,
            "Notre choix préféré",
            null,
            trip.Members.Single().Id,
            1024,
            nowUtc);
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        plans.Setup(repository => repository.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(trip);
        plans.SetupSequence(repository => repository.GetProgramReadSequenceAsync(
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(4)
            .ReturnsAsync(4);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        candidates.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(new[] { candidate });
        Mock<ITripDayPlanRepository> days = new(MockBehavior.Strict);
        days.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripDayPlan>());
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "technical-park-id" })),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new Park
                {
                    Id = "technical-park-id",
                    Name = "Phantasialand",
                    IsVisible = true,
                },
            });
        Mock<ITripItemDecisionRepository> decisions = new(MockBehavior.Strict);
        decisions.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripItemDecision>());
        Mock<IParkItemRepository> parkItems = new(MockBehavior.Strict);
        Mock<ITripAuditWriter> auditWriter = new(MockBehavior.Strict);
        auditWriter.Setup(writer => writer.AppendReadOnlyAsync(
                It.Is<TripActivityWrite>(write =>
                    write.Kind == TripActivityKind.PlanExported
                    && write.ActorRole == TripEffectiveRole.Owner
                    && write.AffectedCount == 1),
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<TimeProvider> timeProvider = new(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        TripProgramResultFactory programFactory = new(
            plans.Object,
            candidates.Object,
            days.Object,
            parks.Object);
        TripActivityRecorder recorder = new(auditWriter.Object, timeProvider.Object);
        TripExportService service = new(
            plans.Object,
            decisions.Object,
            parkItems.Object,
            parks.Object,
            programFactory,
            recorder,
            timeProvider.Object);

        ApplicationResult<TripExportResult> result = await service.ExportAsync(
            trip.OwnerUserId,
            trip.Id.Value,
            "mobile-export-1",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Phantasialand", Assert.Single(result.Value.CandidateParks).ParkName);
        string json = JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain(trip.Id.Value, json, StringComparison.Ordinal);
        Assert.DoesNotContain(candidate.Id.Value, json, StringComparison.Ordinal);
        Assert.DoesNotContain("technical-park-id", json, StringComparison.Ordinal);
        Assert.DoesNotContain(trip.OwnerUserId, json, StringComparison.Ordinal);
        plans.VerifyAll();
        candidates.VerifyAll();
        days.VerifyAll();
        parks.VerifyAll();
        decisions.VerifyAll();
        auditWriter.VerifyAll();
    }

    [Fact]
    public async Task ExportAsync_WhenTheTripIsNotAccessible_ShouldNotReadOrAuditItsProgram()
    {
        TripPlanId tripId = TripPlanId.New();
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        plans.Setup(repository => repository.GetAccessibleAsync(
                "account-1",
                tripId,
                CancellationToken.None))
            .ReturnsAsync((TripPlan?)null);
        Mock<ITripItemDecisionRepository> decisions = new(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItems = new(MockBehavior.Strict);
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        Mock<ITripDayPlanRepository> days = new(MockBehavior.Strict);
        Mock<ITripAuditWriter> auditWriter = new(MockBehavior.Strict);
        TripProgramResultFactory programFactory = new(
            plans.Object,
            candidates.Object,
            days.Object,
            parks.Object);
        TripExportService service = new(
            plans.Object,
            decisions.Object,
            parkItems.Object,
            parks.Object,
            programFactory,
            new TripActivityRecorder(auditWriter.Object));

        ApplicationResult<TripExportResult> result = await service.ExportAsync(
            "account-1",
            tripId.Value,
            "export-1",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("trip.plan.not-found", Assert.Single(result.Errors).Code);
        plans.VerifyAll();
        candidates.VerifyNoOtherCalls();
        days.VerifyNoOtherCalls();
        decisions.VerifyNoOtherCalls();
        auditWriter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExportAsync_WhenTheAuditCannotBePersisted_ShouldFailClosed()
    {
        DateTime nowUtc = new(2027, 8, 12, 9, 30, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "account-owner-42",
            "Voyage",
            TripDateProposal.None(),
            null,
            nowUtc);
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        plans.Setup(repository => repository.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(trip);
        plans.SetupSequence(repository => repository.GetProgramReadSequenceAsync(
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(0)
            .ReturnsAsync(0);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        candidates.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripParkCandidate>());
        Mock<ITripDayPlanRepository> days = new(MockBehavior.Strict);
        days.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripDayPlan>());
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        Mock<ITripItemDecisionRepository> decisions = new(MockBehavior.Strict);
        decisions.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripItemDecision>());
        Mock<IParkItemRepository> parkItems = new(MockBehavior.Strict);
        Mock<ITripAuditWriter> auditWriter = new(MockBehavior.Strict);
        auditWriter.Setup(writer => writer.AppendReadOnlyAsync(
                It.IsAny<TripActivityWrite>(),
                CancellationToken.None))
            .ReturnsAsync(false);
        Mock<TimeProvider> timeProvider = new(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        TripProgramResultFactory programFactory = new(
            plans.Object,
            candidates.Object,
            days.Object,
            parks.Object);
        TripExportService service = new(
            plans.Object,
            decisions.Object,
            parkItems.Object,
            parks.Object,
            programFactory,
            new TripActivityRecorder(auditWriter.Object, timeProvider.Object),
            timeProvider.Object);

        ApplicationResult<TripExportResult> result = await service.ExportAsync(
            trip.OwnerUserId,
            trip.Id.Value,
            "export-1",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("trip.export.unavailable", Assert.Single(result.Errors).Code);
        plans.VerifyAll();
        candidates.VerifyAll();
        days.VerifyAll();
        decisions.VerifyAll();
        auditWriter.VerifyAll();
    }
}
