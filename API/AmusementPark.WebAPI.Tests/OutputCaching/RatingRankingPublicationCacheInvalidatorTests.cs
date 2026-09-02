using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

public sealed class RatingRankingPublicationCacheInvalidatorTests
{
    [Fact]
    public async Task InvalidateAsync_WhenAllCacheLayersSucceed_ShouldConfirmConvergence()
    {
        Mock<IRatingRankProvider> rankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        rankProvider.Setup(provider => provider.Invalidate());
        Mock<IOutputCacheStore> outputCacheStore = new Mock<IOutputCacheStore>(MockBehavior.Strict);
        outputCacheStore
            .Setup(store => store.EvictByTagAsync(
                ApiOutputCachePolicyNames.PublicDataTag,
                CancellationToken.None))
            .Returns(ValueTask.CompletedTask);
        Mock<ISsrPageCacheInvalidator> ssrInvalidator = new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
        ssrInvalidator
            .Setup(invalidator => invalidator.TryInvalidateAsync(
                It.Is<SsrPageCacheInvalidationRequest>(request =>
                    request.All
                    && request.IncludeSeoDocuments
                    && !request.AllowStale
                    && !request.Refresh),
                CancellationToken.None))
            .ReturnsAsync(true);
        RatingRankingPublicationCacheInvalidator invalidator = new RatingRankingPublicationCacheInvalidator(
            rankProvider.Object,
            outputCacheStore.Object,
            ssrInvalidator.Object,
            NullLogger<RatingRankingPublicationCacheInvalidator>.Instance);

        bool result = await invalidator.InvalidateAsync(CancellationToken.None);

        Assert.True(result);
        rankProvider.VerifyAll();
        outputCacheStore.VerifyAll();
        ssrInvalidator.VerifyAll();
    }

    [Fact]
    public async Task InvalidateAsync_WhenApiAndSsrCachesFail_ShouldAttemptEveryLayerAndReportFailure()
    {
        Mock<IRatingRankProvider> rankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        rankProvider.Setup(provider => provider.Invalidate());
        Mock<IOutputCacheStore> outputCacheStore = new Mock<IOutputCacheStore>(MockBehavior.Strict);
        outputCacheStore
            .Setup(store => store.EvictByTagAsync(
                ApiOutputCachePolicyNames.PublicDataTag,
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("output-cache-failed"));
        Mock<ISsrPageCacheInvalidator> ssrInvalidator = new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
        ssrInvalidator
            .Setup(invalidator => invalidator.TryInvalidateAsync(
                It.IsAny<SsrPageCacheInvalidationRequest>(),
                CancellationToken.None))
            .ReturnsAsync(false);
        RatingRankingPublicationCacheInvalidator invalidator = new RatingRankingPublicationCacheInvalidator(
            rankProvider.Object,
            outputCacheStore.Object,
            ssrInvalidator.Object,
            NullLogger<RatingRankingPublicationCacheInvalidator>.Instance);

        bool result = await invalidator.InvalidateAsync(CancellationToken.None);

        Assert.False(result);
        rankProvider.VerifyAll();
        outputCacheStore.VerifyAll();
        ssrInvalidator.VerifyAll();
    }
}
