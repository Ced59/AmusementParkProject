using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Handlers;

public sealed class PublicParkHistoricalDataLoaderTests
{
    [Fact]
    public async Task AssessRolloutGateAsync_WhenGateIsCached_ShouldNotLoadHistoricalFacts()
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        HistoricalSubject parkSubject = new(
            HistoricalSubjectType.Park,
            park.Id,
            park.Name!,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            park.Id);
        PublicParkHistoricalScope scope = new(
            park,
            new[] { parkSubject },
            new Dictionary<string, string>(StringComparer.Ordinal));
        HistoricalParkRolloutGate cachedGate = new(
            2,
            2,
            1,
            new[] { 1998 });
        Mock<IHistoricalParkRolloutGateCache> cache = new(MockBehavior.Strict);
        cache
            .Setup(value => value.GetOrCreateAsync(
                park.Id,
                It.Is<string>(fingerprint => fingerprint.Length == 64),
                It.IsAny<Func<CancellationToken, Task<HistoricalParkRolloutGate>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedGate);
        Mock<IHistoricalFactRepository> facts = new(MockBehavior.Strict);
        Mock<IHistoricalParkRolloutGateAssessmentService> assessment = new(MockBehavior.Strict);
        PublicParkHistoricalDataLoader loader = new(
            new Mock<IParkRepository>(MockBehavior.Strict).Object,
            new Mock<IParkItemRepository>(MockBehavior.Strict).Object,
            new Mock<IParkZoneRepository>(MockBehavior.Strict).Object,
            facts.Object,
            assessment.Object,
            cache.Object);

        HistoricalParkRolloutGate result = await loader.AssessRolloutGateAsync(
            scope,
            CancellationToken.None);

        Assert.Same(cachedGate, result);
        cache.VerifyAll();
        facts.VerifyNoOtherCalls();
        assessment.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HasLegacyPublicTimelineAsync_WhenAvailabilityIsCached_ShouldNotQueryHistoricalFacts()
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        HistoricalSubject parkSubject = new(
            HistoricalSubjectType.Park,
            park.Id,
            park.Name!,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            park.Id);
        PublicParkHistoricalScope scope = new(
            park,
            new[] { parkSubject },
            new Dictionary<string, string>(StringComparer.Ordinal));
        Mock<IHistoricalParkRolloutGateCache> cache = new(MockBehavior.Strict);
        cache
            .Setup(value => value.GetOrCreateLegacyTimelineAvailabilityAsync(
                park.Id,
                It.Is<string>(fingerprint => fingerprint.Length == 64),
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IHistoricalFactRepository> facts = new(MockBehavior.Strict);
        PublicParkHistoricalDataLoader loader = new(
            new Mock<IParkRepository>(MockBehavior.Strict).Object,
            new Mock<IParkItemRepository>(MockBehavior.Strict).Object,
            new Mock<IParkZoneRepository>(MockBehavior.Strict).Object,
            facts.Object,
            new Mock<IHistoricalParkRolloutGateAssessmentService>(MockBehavior.Strict).Object,
            cache.Object);

        bool result = await loader.HasLegacyPublicTimelineAsync(scope, CancellationToken.None);

        Assert.True(result);
        cache.VerifyAll();
        facts.VerifyNoOtherCalls();
    }
}
