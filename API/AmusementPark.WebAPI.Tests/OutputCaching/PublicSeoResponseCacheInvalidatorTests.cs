using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.OutputCaching;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

public sealed class PublicSeoResponseCacheInvalidatorTests
{
    [Fact]
    public async Task InvalidateAsync_ShouldEvictTheSharedPublicSeoTag()
    {
        Mock<IOutputCacheStore> outputCacheStore = new Mock<IOutputCacheStore>(MockBehavior.Strict);
        outputCacheStore
            .Setup(store => store.EvictByTagAsync(
                ApiOutputCachePolicyNames.PublicSeoTag,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        PublicSeoResponseCacheInvalidator invalidator = new PublicSeoResponseCacheInvalidator(outputCacheStore.Object);

        await invalidator.InvalidateAsync(CancellationToken.None);

        outputCacheStore.VerifyAll();
    }
}
