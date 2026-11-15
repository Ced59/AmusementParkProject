using System.Text.Json;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Home;
using AmusementPark.WebAPI.Contracts.Parks;
using AmusementPark.WebAPI.Contracts.AttractionManufacturers;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class PublicCardProjectionHttpMapperTests
{
    [Fact]
    public void Projections_PreserveLanguagesAndUseEachExistingCardLengthAndEllipsis()
    {
        string prefix = new string('a', 100);
        List<LocalizedTextDto> full = new() { new() { LanguageCode = "fr", Value = "<p>" + prefix + ", " + new string('b', 100) + "</p>" } };
        ParkDto park = new ParkDto { Id = "park", Descriptions = full }.ToCardHttp();
        HomeFeaturedParkDto home = new HomeFeaturedParkDto { Id = "park", Descriptions = full }.ToCardHttp();
        HomeLatestArticleDto article = new HomeLatestArticleDto { EventId = "event", Summaries = full }.ToCardHttp();
        AttractionManufacturerDto manufacturer = new AttractionManufacturerDto { Name = "Maker", Biography = full }.ToCardHttp();
        Assert.Empty(park.Descriptions);
        Assert.Equal(prefix + "...", Assert.Single(park.ShortDescriptions!).Value);
        Assert.Equal(prefix + "…", Assert.Single(home.ShortDescriptions!).Value);
        Assert.Equal(prefix + "…", Assert.Single(article.ShortSummaries!).Value);
        Assert.Equal(prefix + ", " + new string('b', 75) + "...", Assert.Single(manufacturer.BiographyPreviews!).Value);
        Assert.Equal("fr", Assert.Single(manufacturer.BiographyPreviews!).LanguageCode);
        Assert.Contains("<p>", full[0].Value);
    }

    [Fact]
    public void ManufacturerPreviews_PreserveImageOnlyFallbackEntriesAndRemoveEmptyOnes()
    {
        AttractionManufacturerDto source = new AttractionManufacturerDto
        {
            Biography = new()
            {
                new() { LanguageCode = "fr", Value = "<IMG src='photo'>" },
                new() { LanguageCode = "en", Value = "English text" },
                new() { LanguageCode = "de", Value = "<p>&nbsp;</p>" },
            },
        };
        AttractionManufacturerDto card = source.ToCardHttp();
        List<LocalizedTextDto> previews = Assert.IsType<List<LocalizedTextDto>>(card.BiographyPreviews);
        Assert.Equal(new[] { "fr", "en" }, previews.Select(text => text.LanguageCode));
        Assert.Null(previews[0].Value);
        Assert.Equal("English text", previews[1].Value);
    }

    [Fact]
    public void LegacyDtos_DoNotExposeProjectionFieldsUnlessOptedIn()
    {
        Assert.DoesNotContain("ShortDescriptions", JsonSerializer.Serialize(new ParkDto()));
        Assert.DoesNotContain("ShortDescriptions", JsonSerializer.Serialize(new HomeFeaturedParkDto()));
        Assert.DoesNotContain("ShortSummaries", JsonSerializer.Serialize(new HomeLatestArticleDto()));
        Assert.DoesNotContain("BiographyPreviews", JsonSerializer.Serialize(new AttractionManufacturerDto()));
    }
}
