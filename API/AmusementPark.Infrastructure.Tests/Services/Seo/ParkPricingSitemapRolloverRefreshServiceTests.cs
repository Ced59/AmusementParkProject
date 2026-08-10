using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Infrastructure.Services.Seo;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Seo;

public sealed class ParkPricingSitemapRolloverRefreshServiceTests
{
    [Fact]
    public void ResolveDelayUntilNextUtcRollover_ShouldTargetTheNextDateBoundary()
    {
        DateTimeOffset nowUtc = new DateTimeOffset(2026, 8, 9, 23, 50, 0, TimeSpan.Zero);

        TimeSpan delay = ParkPricingSitemapRolloverRefreshService.ResolveDelayUntilNextUtcRollover(nowUtc);

        Assert.Equal(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(5), delay);
    }

    [Fact]
    public async Task RefreshScheduler_WhenGenerationSucceeds_ShouldInvalidatePublicSeoResponses()
    {
        Mock<IPublicSeoResponseCacheInvalidator> invalidator = new Mock<IPublicSeoResponseCacheInvalidator>(MockBehavior.Strict);
        invalidator
            .Setup(value => value.InvalidateAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        InMemorySeoSitemapRefreshScheduler scheduler = new InMemorySeoSitemapRefreshScheduler(
            Mock.Of<IServiceScopeFactory>(),
            invalidator.Object);
        SitemapGenerationResult result = new SitemapGenerationResult
        {
            Status = SitemapGenerationStatus.Succeeded,
        };

        await scheduler.InvalidatePublicResponsesAfterSuccessfulGenerationAsync(result, CancellationToken.None);

        invalidator.VerifyAll();
    }
}
