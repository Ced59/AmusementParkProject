using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

public sealed class SharePublicationCacheInvalidationExecutorTests
{
    [Theory]
    [InlineData(SharePublicationType.PersonalRanking, "rankings/shared")]
    [InlineData(SharePublicationType.VisitRecap, "passport/shared/visits")]
    [InlineData(SharePublicationType.YearRecap, "passport/shared/years")]
    [InlineData(SharePublicationType.PassportProfile, "passport/shared/profiles")]
    [InlineData(SharePublicationType.ProfileComparison, "passport/shared/comparisons")]
    public void BuildSsrRequest_ShouldPurgeOldAndNewLocalizedPublicRoutes(
        SharePublicationType publicationType,
        string routeSegment)
    {
        SharePublicationCacheInvalidationRequest source =
            new SharePublicationCacheInvalidationRequest(
                "publication-1",
                publicationType,
                new[] { "old/token", "new token" });

        SsrPageCacheInvalidationRequest request =
            SharePublicationCacheInvalidationExecutor.BuildSsrRequest(source);

        Assert.False(request.All);
        Assert.False(request.AllowStale);
        Assert.False(request.Refresh);
        Assert.False(request.IncludeSeoDocuments);
        Assert.Equal(16, request.Paths.Count);
        Assert.Contains($"/fr/{routeSegment}/old%2Ftoken", request.Paths);
        Assert.Contains($"/en/{routeSegment}/new%20token", request.Paths);
    }

    [Fact]
    public async Task TryInvalidateAsync_WhenSsrDoesNotConfirm_ShouldRequestDurableRetry()
    {
        SharePublicationCacheInvalidationRequest source =
            new SharePublicationCacheInvalidationRequest(
                "publication-1",
                SharePublicationType.VisitRecap,
                new[] { "old-token", "new-token" });
        Mock<IShareSocialImageCacheInvalidator> socialImages =
            new Mock<IShareSocialImageCacheInvalidator>(MockBehavior.Strict);
        socialImages.Setup(value => value.Invalidate(
            It.Is<IReadOnlyCollection<string>>(shareIds =>
                shareIds.Contains("old-token")
                && shareIds.Contains("new-token"))));
        Mock<ISsrPageCacheInvalidator> ssr =
            new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
        ssr.Setup(value => value.TryInvalidateAsync(
                It.Is<SsrPageCacheInvalidationRequest>(request =>
                    request.Paths.Count == 16
                    && !request.AllowStale),
                CancellationToken.None))
            .ReturnsAsync(false);
        SharePublicationCacheInvalidationExecutor executor =
            new SharePublicationCacheInvalidationExecutor(
                socialImages.Object,
                ssr.Object,
                NullLogger<SharePublicationCacheInvalidationExecutor>.Instance);

        bool succeeded = await executor.TryInvalidateAsync(source, CancellationToken.None);

        Assert.False(succeeded);
        socialImages.VerifyAll();
        ssr.VerifyAll();
    }
}
