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
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.LiveData.Handlers;

public sealed class CreateLiveTargetMappingCandidateCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldPersistANonEligibleLowConfidenceCandidate()
    {
        DateTime nowUtc = new DateTime(2026, 9, 28, 18, 0, 0, DateTimeKind.Utc);
        Mock<ILiveTargetMappingRepository> repository = new Mock<ILiveTargetMappingRepository>(
            MockBehavior.Strict);
        repository.Setup(candidate => candidate.GetLatestByNaturalKeyAsync(
                LiveDataSourceId.Parse("themeparks-wiki"),
                "external-item-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalLiveTargetMapping?)null);
        repository.Setup(candidate => candidate.AppendRevisionAsync(
                It.Is<ExternalLiveTargetMapping>(mapping =>
                    mapping.Status == LiveMappingStatus.Candidate
                    && mapping.Confidence == LiveMappingConfidence.Low
                    && !mapping.IsEligibleForLiveUse
                    && mapping.Revision == 1),
                0,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveTargetMappingWriteOutcome.Created);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        CreateLiveTargetMappingCandidateCommandHandler handler = new(
            repository.Object,
            new LiveTargetReferenceResolver(parks.Object, items.Object),
            timeProvider.Object);

        ApplicationResult<LiveTargetMappingResult> result = await handler.HandleAsync(
            new CreateLiveTargetMappingCandidateCommand(
                "themeparks-wiki",
                LiveTargetType.ParkItem,
                "external-item-1",
                "external-park-1",
                "Mystery Castle",
                "Phantasialand",
                "DE",
                null,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(LiveMappingStatus.Candidate, result.Value?.Status);
        Assert.False(result.Value?.IsEligibleForLiveUse);
        repository.VerifyAll();
        timeProvider.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectAnIncompleteSuggestedTargetPair()
    {
        Mock<ILiveTargetMappingRepository> repository = new Mock<ILiveTargetMappingRepository>(
            MockBehavior.Strict);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        CreateLiveTargetMappingCandidateCommandHandler handler = new(
            repository.Object,
            new LiveTargetReferenceResolver(parks.Object, items.Object));

        ApplicationResult<LiveTargetMappingResult> result = await handler.HandleAsync(
            new CreateLiveTargetMappingCandidateCommand(
                "themeparks-wiki",
                LiveTargetType.Park,
                "external-park-1",
                null,
                "Phantasialand",
                null,
                "DE",
                "park-1",
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("live-data.mapping.invalid", Assert.Single(result.Errors).Code);
    }
}
