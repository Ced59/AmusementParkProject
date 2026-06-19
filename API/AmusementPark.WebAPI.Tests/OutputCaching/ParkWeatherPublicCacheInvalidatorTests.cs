using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

public sealed class ParkWeatherPublicCacheInvalidatorTests
{
    [Fact]
    public async Task InvalidateUpdatedWeatherAsync_WhenParksAreUpdated_ShouldEvictWeatherTagAndHardPurgeParkPages()
    {
        SsrPageCacheInvalidationRequest capturedRequest = null!;
        Park park = new Park
        {
            Id = "park-1",
            Name = "Magic Park",
            IsVisible = true,
        };
        Mock<IOutputCacheStore> outputCacheStore = new Mock<IOutputCacheStore>(MockBehavior.Strict);
        outputCacheStore
            .Setup(store => store.EvictByTagAsync(ApiOutputCachePolicyNames.PublicWeatherDataTag, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        Mock<ISsrPageCacheInvalidator> ssrPageCacheInvalidator = new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
        ssrPageCacheInvalidator
            .Setup(invalidator => invalidator.InvalidateAsync(It.IsAny<SsrPageCacheInvalidationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SsrPageCacheInvalidationRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);
        ParkWeatherPublicCacheInvalidator invalidator = new ParkWeatherPublicCacheInvalidator(
            outputCacheStore.Object,
            ssrPageCacheInvalidator.Object,
            NullLogger<ParkWeatherPublicCacheInvalidator>.Instance);

        await invalidator.InvalidateUpdatedWeatherAsync(new[] { park }, CancellationToken.None);

        Assert.False(capturedRequest.All);
        Assert.False(capturedRequest.AllowStale);
        Assert.False(capturedRequest.Refresh);
        Assert.False(capturedRequest.IncludeSeoDocuments);
        Assert.Empty(capturedRequest.Prefixes);
        Assert.Contains("/fr/park/park-1/magic-park", capturedRequest.Paths);
        Assert.Contains("/fr/park/park-1/magic-park/weather", capturedRequest.Paths);
        Assert.Contains("/en/park/park-1/magic-park", capturedRequest.Paths);
        Assert.Contains("/en/park/park-1/magic-park/weather", capturedRequest.Paths);
        Assert.Equal(16, capturedRequest.Paths.Count);
        outputCacheStore.VerifyAll();
        ssrPageCacheInvalidator.VerifyAll();
    }

    [Fact]
    public async Task InvalidateUpdatedWeatherAsync_WhenNoParkIsUpdated_ShouldSkipCacheBackends()
    {
        Mock<IOutputCacheStore> outputCacheStore = new Mock<IOutputCacheStore>(MockBehavior.Strict);
        Mock<ISsrPageCacheInvalidator> ssrPageCacheInvalidator = new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
        ParkWeatherPublicCacheInvalidator invalidator = new ParkWeatherPublicCacheInvalidator(
            outputCacheStore.Object,
            ssrPageCacheInvalidator.Object,
            NullLogger<ParkWeatherPublicCacheInvalidator>.Instance);

        await invalidator.InvalidateUpdatedWeatherAsync(Array.Empty<Park>(), CancellationToken.None);

        outputCacheStore.Verify(
            store => store.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        ssrPageCacheInvalidator.Verify(
            cacheInvalidator => cacheInvalidator.InvalidateAsync(It.IsAny<SsrPageCacheInvalidationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
