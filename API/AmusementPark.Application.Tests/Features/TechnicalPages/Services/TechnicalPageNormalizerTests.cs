using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.TechnicalPages.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.TechnicalPages;
using AmusementPark.Core.Localization;
using Xunit;

namespace AmusementPark.Application.Tests.Features.TechnicalPages.Services;

public sealed class TechnicalPageNormalizerTests
{
    [Fact]
    public void NormalizeForSave_WhenPageIsComplete_ShouldNormalizeIdentifiersAndRichContent()
    {
        TechnicalPage page = new TechnicalPage
        {
            CategoryKey = " Lift Systems ",
            Slug = string.Empty,
            CategoryNames = LocalizedValues(" Lifts "),
            Titles = LocalizedValues(" Chain Lift "),
            Summaries = LocalizedValues(" Detailed chain lift explanation. "),
            AdminReviewStatus = AdminReviewStatus.Validated,
            Aliases = new List<TechnicalPageAlias>
            {
                new TechnicalPageAlias
                {
                    CategoryKey = " Lift ",
                    Labels = LocalizedValues(" Chain lift "),
                },
                new TechnicalPageAlias
                {
                    CategoryKey = " ",
                    Labels = LocalizedValues("Ignored"),
                },
            },
            ContentBlocks = new List<TechnicalContentBlock>
            {
                new TechnicalContentBlock
                {
                    BlockType = " diagram ",
                    Tone = " info ",
                    ImageUrl = " ",
                    DiagramKey = " chain-lift ",
                    Titles = LocalizedValues("Force path"),
                    Links = new List<TechnicalContentLink>
                    {
                        new TechnicalContentLink
                        {
                            Url = " https://example.com/source ",
                            Label = LocalizedValues("Source"),
                        },
                        new TechnicalContentLink
                        {
                            Url = " ",
                            Label = LocalizedValues("Ignored"),
                        },
                    },
                    Columns = new List<TechnicalContentBlock>
                    {
                        new TechnicalContentBlock
                        {
                            BlockType = " ",
                            Bodies = LocalizedValues("Nested explanation"),
                        },
                    },
                },
            },
        };

        ApplicationResult<TechnicalPage> result = TechnicalPageNormalizer.NormalizeForSave(page);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        TechnicalPage normalizedPage = result.Value!;
        Assert.Equal("lift-systems", normalizedPage.CategoryKey);
        Assert.Equal("chain-lift", normalizedPage.Slug);
        Assert.Equal(AdminReviewStatus.Validated, normalizedPage.AdminReviewStatus);
        TechnicalPageAlias alias = Assert.Single(normalizedPage.Aliases);
        Assert.Equal("lift", alias.CategoryKey);
        Assert.Equal(8, alias.Labels.Count);
        TechnicalContentBlock block = Assert.Single(normalizedPage.ContentBlocks);
        Assert.Equal("diagram", block.BlockType);
        Assert.Equal("info", block.Tone);
        Assert.Null(block.ImageUrl);
        Assert.Equal("chain-lift", block.DiagramKey);
        TechnicalContentLink link = Assert.Single(block.Links);
        Assert.Equal("https://example.com/source", link.Url);
        TechnicalContentBlock column = Assert.Single(block.Columns);
        Assert.Equal("richText", column.BlockType);
    }

    [Fact]
    public void NormalizeForSave_WhenRequiredLocalizedTextIsMissing_ShouldReturnValidationErrors()
    {
        TechnicalPage page = new TechnicalPage
        {
            CategoryKey = "restraint",
            Slug = "lap-bar",
            CategoryNames = new List<LocalizedText> { new LocalizedText("fr", "Retenues") },
            Titles = LocalizedValues("Lap bar"),
            Summaries = LocalizedValues("Summary"),
        };

        ApplicationResult<TechnicalPage> result = TechnicalPageNormalizer.NormalizeForSave(page);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "technical-page.localized.required" && error.Message.Contains("CategoryNames.en", StringComparison.Ordinal));
    }

    private static List<LocalizedText> LocalizedValues(string value)
    {
        return new List<LocalizedText>
        {
            new LocalizedText("fr", value),
            new LocalizedText("en", value),
            new LocalizedText("de", value),
            new LocalizedText("nl", value),
            new LocalizedText("it", value),
            new LocalizedText("es", value),
            new LocalizedText("pl", value),
            new LocalizedText("pt", value),
        };
    }
}
