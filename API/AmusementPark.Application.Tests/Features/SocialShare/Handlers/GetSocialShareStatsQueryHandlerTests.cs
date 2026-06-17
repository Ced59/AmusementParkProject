using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialShare.Contracts;
using AmusementPark.Application.Features.SocialShare.Handlers;
using AmusementPark.Application.Features.SocialShare.Ports;
using AmusementPark.Application.Features.SocialShare.Queries;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.SocialShare.Handlers;

public sealed class GetSocialShareStatsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenFromIsAfterTo_ShouldReject()
    {
        Mock<ISocialShareEventRepository> repository = new Mock<ISocialShareEventRepository>(MockBehavior.Strict);
        GetSocialShareStatsQueryHandler handler = new GetSocialShareStatsQueryHandler(repository.Object);

        ApplicationResult<SocialShareStatsResult> result = await handler.HandleAsync(new GetSocialShareStatsQuery(
            new SocialShareStatsCriteria(
                new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc))));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "social-share.stats.date-range.invalid");
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenRangeIsTooLarge_ShouldClampToMaximumRange()
    {
        Mock<ISocialShareEventRepository> repository = new Mock<ISocialShareEventRepository>(MockBehavior.Strict);
        repository
            .Setup(repo => repo.GetStatsAsync(
                new DateTime(2025, 12, 19, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime fromUtc, DateTime toUtc, CancellationToken _) => new SocialShareStatsResult(
                fromUtc,
                toUtc,
                0,
                0,
                0,
                Array.Empty<SocialShareDailyStatsPoint>(),
                Array.Empty<SocialShareDimensionCount>(),
                Array.Empty<SocialShareDimensionCount>(),
                Array.Empty<SocialShareDimensionCount>(),
                Array.Empty<SocialShareTopTarget>()));

        GetSocialShareStatsQueryHandler handler = new GetSocialShareStatsQueryHandler(repository.Object);

        ApplicationResult<SocialShareStatsResult> result = await handler.HandleAsync(new GetSocialShareStatsQuery(
            new SocialShareStatsCriteria(
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc))));

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTime(2025, 12, 19, 0, 0, 0, DateTimeKind.Utc), result.Value!.FromUtc);
        repository.VerifyAll();
    }
}
