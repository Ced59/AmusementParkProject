using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Infrastructure.Services.Ratings;
using SixLabors.ImageSharp;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Ratings;

public sealed class UserRankingSharePreviewRendererTests
{
    [Fact]
    public async Task RenderPngAsync_ShouldCreateAValidSocialImageWithTheTopFiveItems()
    {
        UserRankingSharePreviewResult preview = new UserRankingSharePreviewResult(
            "Camille",
            Enumerable.Range(1, 7)
                .Select(index => new UserRankingSharePreviewItemResult(
                    index,
                    $"Favourite attraction {index}",
                    "Demo Park",
                    5d - index * 0.5d))
                .ToList());
        UserRankingSharePreviewRenderer renderer = new UserRankingSharePreviewRenderer();

        byte[] content = await renderer.RenderPngAsync(preview, CancellationToken.None);

        Assert.True(content.Length > 1_000);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, content.Take(8));
        using Image image = Image.Load(content);
        Assert.Equal(1200, image.Width);
        Assert.Equal(630, image.Height);
    }
}
