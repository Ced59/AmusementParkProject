using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.FactualEvents.Services;
using AmusementPark.Core.Domain.FactualEvents;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.FactualEvents.Services;

public sealed class FactualChangeEventAdministrationServiceTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 15, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task VerifyAsync_WithCurrentDraft_ShouldPersistVerifiedFact()
    {
        FactualChangeEvent factualEvent = CreateDraft(DataConfidence.High);
        Mock<IFactualChangeEventRepository> repository = CreateRepository(factualEvent);
        repository.Setup(value => value.ReplaceAsync(
                It.Is<FactualChangeEvent>(item =>
                    item.Status == FactualChangeStatus.Verified
                    && item.VerifiedAtUtc == NowUtc
                    && !item.CanBeDistributed),
                1,
                CancellationToken.None))
            .ReturnsAsync(FactualChangeEventMutationOutcome.Success);
        FactualChangeEventAdministrationService service = CreateService(repository);

        ApplicationResult result = await service.VerifyAsync(
            factualEvent.Id.Value,
            1,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.VerifyAll();
    }

    [Fact]
    public async Task PublishAsync_WithVerifiedFact_ShouldMakeOnlyPublishedFactDistributable()
    {
        FactualChangeEvent factualEvent = CreateDraft(DataConfidence.Medium);
        factualEvent.Verify(NowUtc.AddMinutes(-1));
        Mock<IFactualChangeEventRepository> repository = CreateRepository(factualEvent);
        repository.Setup(value => value.ReplaceAsync(
                It.Is<FactualChangeEvent>(item =>
                    item.Status == FactualChangeStatus.Published
                    && item.PublishedAtUtc == NowUtc
                    && item.CanBeDistributed),
                2,
                CancellationToken.None))
            .ReturnsAsync(FactualChangeEventMutationOutcome.Success);
        FactualChangeEventAdministrationService service = CreateService(repository);

        ApplicationResult result = await service.PublishAsync(
            factualEvent.Id.Value,
            2,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.VerifyAll();
    }

    [Fact]
    public async Task VerifyAsync_WithLowConfidence_ShouldRejectTransitionWithoutWrite()
    {
        FactualChangeEvent factualEvent = CreateDraft(DataConfidence.Low);
        Mock<IFactualChangeEventRepository> repository = CreateRepository(factualEvent);
        FactualChangeEventAdministrationService service = CreateService(repository);

        ApplicationResult result = await service.VerifyAsync(
            factualEvent.Id.Value,
            1,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("factual-event.transition.invalid", Assert.Single(result.Errors).Code);
        repository.VerifyAll();
    }

    [Fact]
    public async Task VerifyAsync_WithStaleVersion_ShouldRejectWithoutWrite()
    {
        FactualChangeEvent factualEvent = CreateDraft(DataConfidence.High);
        Mock<IFactualChangeEventRepository> repository = CreateRepository(factualEvent);
        FactualChangeEventAdministrationService service = CreateService(repository);

        ApplicationResult result = await service.VerifyAsync(
            factualEvent.Id.Value,
            2,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("factual-event.version.conflict", Assert.Single(result.Errors).Code);
        repository.VerifyAll();
    }

    [Fact]
    public async Task VerifyAsync_WhenCompareAndSwapLosesRace_ShouldReturnConflict()
    {
        FactualChangeEvent factualEvent = CreateDraft(DataConfidence.High);
        Mock<IFactualChangeEventRepository> repository = CreateRepository(factualEvent);
        repository.Setup(value => value.ReplaceAsync(
                factualEvent,
                1,
                CancellationToken.None))
            .ReturnsAsync(FactualChangeEventMutationOutcome.Conflict);
        FactualChangeEventAdministrationService service = CreateService(repository);

        ApplicationResult result = await service.VerifyAsync(
            factualEvent.Id.Value,
            1,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("factual-event.version.conflict", Assert.Single(result.Errors).Code);
        repository.VerifyAll();
    }

    [Fact]
    public async Task CorrectAsync_WithNewerPublishedLogicalRevision_ShouldLinkBothFacts()
    {
        FactualChangeEvent original = CreateDraft(DataConfidence.High);
        original.Verify(NowUtc.AddMinutes(-4));
        original.Publish(NowUtc.AddMinutes(-3));
        FactualChangeEvent successor = CreateSuccessor();
        Mock<IFactualChangeEventRepository> repository =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetAsync(original.Id, CancellationToken.None))
            .ReturnsAsync(original);
        repository.Setup(value => value.GetAsync(successor.Id, CancellationToken.None))
            .ReturnsAsync(successor);
        repository.Setup(value => value.ReplaceAsync(
                It.Is<FactualChangeEvent>(item =>
                    item.Status == FactualChangeStatus.Corrected
                    && item.SupersededByEventId == successor.Id
                    && item.TerminalAtUtc == NowUtc),
                3,
                CancellationToken.None))
            .ReturnsAsync(FactualChangeEventMutationOutcome.Success);
        FactualChangeEventAdministrationService service = CreateService(repository);

        ApplicationResult result = await service.CorrectAsync(
            original.Id.Value,
            3,
            successor.Id.Value,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.VerifyAll();
    }

    [Fact]
    public async Task RetractAsync_WithPublishedFact_ShouldKeepReasonAndTerminalDate()
    {
        FactualChangeEvent factualEvent = CreateDraft(DataConfidence.High);
        factualEvent.Verify(NowUtc.AddMinutes(-2));
        factualEvent.Publish(NowUtc.AddMinutes(-1));
        Mock<IFactualChangeEventRepository> repository = CreateRepository(factualEvent);
        repository.Setup(value => value.ReplaceAsync(
                It.Is<FactualChangeEvent>(item =>
                    item.Status == FactualChangeStatus.Retracted
                    && item.ReasonCode == "source-invalidated"
                    && item.TerminalAtUtc == NowUtc),
                3,
                CancellationToken.None))
            .ReturnsAsync(FactualChangeEventMutationOutcome.Success);
        FactualChangeEventAdministrationService service = CreateService(repository);

        ApplicationResult result = await service.RetractAsync(
            factualEvent.Id.Value,
            3,
            "source-invalidated",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.VerifyAll();
    }

    [Fact]
    public async Task RetractAsync_WhileInitialDistributionIsPending_ShouldKeepPublishedFact()
    {
        FactualChangeEvent factualEvent = CreateDraft(DataConfidence.High);
        factualEvent.Verify(NowUtc.AddMinutes(-2));
        factualEvent.Publish(NowUtc.AddMinutes(-1));
        Mock<IFactualChangeEventRepository> repository = CreateRepository(factualEvent);
        FactualChangeEventAdministrationService service = CreateService(
            repository,
            distributionCompleted: false);

        ApplicationResult result = await service.RetractAsync(
            factualEvent.Id.Value,
            3,
            "source-invalidated",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("factual-event.distribution.pending", Assert.Single(result.Errors).Code);
        Assert.Equal(FactualChangeStatus.Published, factualEvent.Status);
        repository.VerifyAll();
    }

    [Fact]
    public async Task RetractAsync_WithUnsupportedReasonCode_ShouldRejectBeforeLoadingTheEvent()
    {
        Mock<IFactualChangeEventRepository> repository =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        FactualChangeEventAdministrationService service = CreateService(repository);

        ApplicationResult result = await service.RetractAsync(
            "event-1",
            3,
            "unsupported-reason",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("factual-event.admin.mutation.invalid", Assert.Single(result.Errors).Code);
        repository.VerifyNoOtherCalls();
    }

    private static Mock<IFactualChangeEventRepository> CreateRepository(
        FactualChangeEvent factualEvent)
    {
        Mock<IFactualChangeEventRepository> repository =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetAsync(
                factualEvent.Id,
                CancellationToken.None))
            .ReturnsAsync(factualEvent);
        return repository;
    }

    private static FactualChangeEventAdministrationService CreateService(
        Mock<IFactualChangeEventRepository> repository,
        bool distributionCompleted = true)
    {
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(value => value.GetUtcNow())
            .Returns(new DateTimeOffset(NowUtc));
        Mock<IFactualChangeEventDistributionStateReader> distributionState =
            new Mock<IFactualChangeEventDistributionStateReader>(MockBehavior.Strict);
        distributionState.Setup(value => value.IsInitialDistributionCompletedAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(distributionCompleted);
        return new FactualChangeEventAdministrationService(
            repository.Object,
            distributionState.Object,
            timeProvider.Object);
    }

    private static FactualChangeEvent CreateDraft(DataConfidence confidence)
    {
        return FactualChangeEvent.CreateDraft(
            FactualChangeEventId.Parse("event-admin-1"),
            FactualEventType.ParkNameChanged,
            ChangeTarget.ForPark("park-1"),
            FactValue.FromText("Ancien nom"),
            FactValue.FromText("Nouveau nom"),
            new SourceReference(
                SourceReferenceType.OfficialWebsite,
                "Parc exemple",
                "Annonce officielle",
                "https://example.com/news",
                NowUtc.AddHours(-2)),
            confidence,
            NowUtc.AddHours(-1),
            "park:park-1:name",
            1,
            NowUtc.AddMinutes(-30));
    }

    private static FactualChangeEvent CreateSuccessor()
    {
        FactualChangeEvent successor = FactualChangeEvent.CreateDraft(
            FactualChangeEventId.Parse("event-admin-2"),
            FactualEventType.ParkNameChanged,
            ChangeTarget.ForPark("park-1"),
            FactValue.FromText("Nouveau nom"),
            FactValue.FromText("Nom corrigé"),
            new SourceReference(
                SourceReferenceType.OfficialWebsite,
                "Parc exemple",
                "Rectificatif officiel",
                "https://example.com/correction",
                NowUtc.AddMinutes(-5)),
            DataConfidence.High,
            NowUtc.AddMinutes(-4),
            "park:park-1:name",
            2,
            NowUtc.AddMinutes(-4));
        successor.Verify(NowUtc.AddMinutes(-2));
        successor.Publish(NowUtc.AddMinutes(-1));
        return successor;
    }
}
