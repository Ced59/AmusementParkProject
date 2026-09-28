using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Commands;
using AmusementPark.Application.Features.LiveData.Handlers;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.LiveData.Handlers;

public sealed class ReviewLiveTargetMappingCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldVerifyACandidateAgainstTheRealInternalPark()
    {
        DateTime discoveredAtUtc = new DateTime(2026, 9, 28, 17, 0, 0, DateTimeKind.Utc);
        DateTime reviewedAtUtc = discoveredAtUtc.AddMinutes(10);
        ExternalLiveTargetMapping current = ExternalLiveTargetMapping.CreateCandidate(
            Guid.NewGuid(),
            LiveDataSourceId.Parse("themeparks-wiki"),
            new ExternalLiveTargetDescriptor(
                LiveTargetType.Park,
                "external-park-1",
                null,
                "Phantasialand",
                null,
                "DE"),
            null,
            LiveMappingConfidence.Low,
            discoveredAtUtc);
        Mock<ILiveTargetMappingRepository> repository = new Mock<ILiveTargetMappingRepository>(
            MockBehavior.Strict);
        repository.Setup(candidate => candidate.GetLatestByIdAsync(
                current.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);
        repository.Setup(candidate => candidate.AppendRevisionAsync(
                It.Is<ExternalLiveTargetMapping>(mapping =>
                    mapping.Status == LiveMappingStatus.Verified
                    && mapping.Target != null
                    && mapping.Target.DisplayName == "Phantasialand"
                    && mapping.IsEligibleForLiveUse
                    && mapping.Revision == 2),
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveTargetMappingWriteOutcome.Created);
        Park park = new Park { Id = "park-1", Name = "Phantasialand", CountryCode = "DE" };
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(candidate => candidate.GetByIdAsync(
                "park-1",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(reviewedAtUtc));
        ReviewLiveTargetMappingCommandHandler handler = new(
            repository.Object,
            new LiveTargetReferenceResolver(parks.Object, items.Object),
            timeProvider.Object);

        ApplicationResult<LiveTargetMappingResult> result = await handler.HandleAsync(
            new ReviewLiveTargetMappingCommand(
                current.Id,
                1,
                LiveTargetMappingDecision.Verify,
                "park-1",
                "park-1",
                "admin-1",
                "Identifiants et pays vérifiés."),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value?.IsEligibleForLiveUse);
        Assert.Equal(2, result.Value?.Revision);
        repository.VerifyAll();
        parks.VerifyAll();
        timeProvider.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTheCurrentRevisionOnConflict()
    {
        DateTime nowUtc = new DateTime(2026, 9, 28, 17, 0, 0, DateTimeKind.Utc);
        ExternalLiveTargetMapping current = ExternalLiveTargetMapping.CreateCandidate(
            Guid.NewGuid(),
            LiveDataSourceId.Parse("themeparks-wiki"),
            new ExternalLiveTargetDescriptor(
                LiveTargetType.Park,
                "external-park-1",
                null,
                "Phantasialand",
                null,
                "DE"),
            null,
            LiveMappingConfidence.Low,
            nowUtc);
        Mock<ILiveTargetMappingRepository> repository = new Mock<ILiveTargetMappingRepository>(
            MockBehavior.Strict);
        repository.Setup(candidate => candidate.GetLatestByIdAsync(
                current.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        ReviewLiveTargetMappingCommandHandler handler = new(
            repository.Object,
            new LiveTargetReferenceResolver(parks.Object, items.Object));

        ApplicationResult<LiveTargetMappingResult> result = await handler.HandleAsync(
            new ReviewLiveTargetMappingCommand(
                current.Id,
                2,
                LiveTargetMappingDecision.Verify,
                "park-1",
                "park-1",
                "admin-1",
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        ApplicationError error = Assert.Single(result.Errors);
        Assert.Equal("live-data.mapping.revision-conflict", error.Code);
        Assert.Equal(1, error.CurrentVersion);
        repository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_ForParkItemMappedToAnotherParentPark_ShouldRejectVerification()
    {
        DateTime nowUtc = new DateTime(2026, 9, 28, 17, 0, 0, DateTimeKind.Utc);
        LiveDataSourceId sourceId = LiveDataSourceId.Parse("themeparks-wiki");
        ExternalLiveTargetMapping current = ExternalLiveTargetMapping.CreateCandidate(
            Guid.NewGuid(),
            sourceId,
            new ExternalLiveTargetDescriptor(
                LiveTargetType.ParkItem,
                "external-item-1",
                "external-park-1",
                "Shared Ride Name",
                "External Park",
                "DE"),
            null,
            LiveMappingConfidence.Low,
            nowUtc);
        ExternalLiveTargetMapping verifiedParent = ExternalLiveTargetMapping.CreateCandidate(
                Guid.NewGuid(),
                sourceId,
                new ExternalLiveTargetDescriptor(
                    LiveTargetType.Park,
                    "external-park-1",
                    null,
                    "External Park",
                    null,
                    "DE"),
                null,
                LiveMappingConfidence.Low,
                nowUtc)
            .Verify(
                new LiveTargetReference(
                    LiveTargetType.Park,
                    "park-1",
                    "park-1",
                    "Internal Park One",
                    "Internal Park One",
                    "DE"),
                "admin-1",
                "Parent verified.",
                nowUtc.AddMinutes(1));
        Mock<ILiveTargetMappingRepository> repository = new Mock<ILiveTargetMappingRepository>(
            MockBehavior.Strict);
        repository.Setup(candidate => candidate.GetLatestByIdAsync(
                current.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);
        repository.Setup(candidate => candidate.GetLatestByNaturalKeyAsync(
                sourceId,
                "external-park-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(verifiedParent);
        Park park = new Park { Id = "park-2", Name = "Internal Park Two", CountryCode = "DE" };
        ParkItem item = new ParkItem { Id = "item-2", ParkId = "park-2", Name = "Shared Ride Name" };
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(candidate => candidate.GetByIdAsync(
                "park-2",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        items.Setup(candidate => candidate.GetByIdAsync(
                "item-2",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        ReviewLiveTargetMappingCommandHandler handler = new(
            repository.Object,
            new LiveTargetReferenceResolver(parks.Object, items.Object));

        ApplicationResult<LiveTargetMappingResult> result = await handler.HandleAsync(
            new ReviewLiveTargetMappingCommand(
                current.Id,
                1,
                LiveTargetMappingDecision.Verify,
                "item-2",
                "park-2",
                "admin-1",
                "Same name, but a different park."),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "live-data.mapping.parent-not-verified",
            Assert.Single(result.Errors).Code);
        repository.VerifyAll();
        parks.VerifyAll();
        items.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenAppendLosesRace_ShouldReturnPersistedLatestRevision()
    {
        DateTime discoveredAtUtc = new DateTime(2026, 9, 28, 17, 0, 0, DateTimeKind.Utc);
        DateTime reviewedAtUtc = discoveredAtUtc.AddMinutes(10);
        ExternalLiveTargetMapping current = ExternalLiveTargetMapping.CreateCandidate(
            Guid.NewGuid(),
            LiveDataSourceId.Parse("themeparks-wiki"),
            new ExternalLiveTargetDescriptor(
                LiveTargetType.Park,
                "external-park-1",
                null,
                "Phantasialand",
                null,
                "DE"),
            null,
            LiveMappingConfidence.Low,
            discoveredAtUtc);
        LiveTargetReference target = new LiveTargetReference(
            LiveTargetType.Park,
            "park-1",
            "park-1",
            "Phantasialand",
            "Phantasialand",
            "DE");
        ExternalLiveTargetMapping concurrentlyPersisted = current.Verify(
            target,
            "admin-2",
            "Concurrent review.",
            reviewedAtUtc);
        Mock<ILiveTargetMappingRepository> repository = new Mock<ILiveTargetMappingRepository>(
            MockBehavior.Strict);
        repository.SetupSequence(candidate => candidate.GetLatestByIdAsync(
                current.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(current)
            .ReturnsAsync(concurrentlyPersisted);
        repository.Setup(candidate => candidate.AppendRevisionAsync(
                It.Is<ExternalLiveTargetMapping>(mapping => mapping.Revision == 2),
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveTargetMappingWriteOutcome.Conflict);
        Park park = new Park { Id = "park-1", Name = "Phantasialand", CountryCode = "DE" };
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(candidate => candidate.GetByIdAsync(
                "park-1",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(reviewedAtUtc));
        ReviewLiveTargetMappingCommandHandler handler = new(
            repository.Object,
            new LiveTargetReferenceResolver(parks.Object, items.Object),
            timeProvider.Object);

        ApplicationResult<LiveTargetMappingResult> result = await handler.HandleAsync(
            new ReviewLiveTargetMappingCommand(
                current.Id,
                1,
                LiveTargetMappingDecision.Verify,
                "park-1",
                "park-1",
                "admin-1",
                "Identifiers verified."),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        ApplicationError error = Assert.Single(result.Errors);
        Assert.Equal("live-data.mapping.revision-conflict", error.Code);
        Assert.Equal(2, error.CurrentVersion);
        repository.VerifyAll();
        parks.VerifyAll();
        timeProvider.VerifyAll();
    }
}
