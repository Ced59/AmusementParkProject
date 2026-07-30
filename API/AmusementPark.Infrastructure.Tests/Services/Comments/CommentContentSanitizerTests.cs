using AmusementPark.Infrastructure.Services.Comments;
using HtmlAgilityPack;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Comments;

public sealed class CommentContentSanitizerTests
{
    private readonly CommentContentSanitizer sanitizer = new CommentContentSanitizer();

    [Fact]
    public void SanitizeRichHtml_WhenMarkupContainsUnsafeContent_ShouldRemoveItAndKeepFormatting()
    {
        string result = this.sanitizer.SanitizeRichHtml(
            "<p><strong>Avis</strong><script>alert(1)</script><a href=\"javascript:alert(2)\">lien</a></p>");

        Assert.Contains("<strong>Avis</strong>", result);
        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(">lien</a>", result);
    }

    [Fact]
    public void SanitizeRichHtml_WhenLinkIsHttps_ShouldHardenExternalNavigation()
    {
        string result = this.sanitizer.SanitizeRichHtml("<a href=\"https://example.com/page\">Exemple</a>");

        Assert.Contains("href=\"https://example.com/page\"", result);
        Assert.Contains("target=\"_blank\"", result);
        Assert.Contains("rel=\"noopener noreferrer nofollow\"", result);
    }

    [Fact]
    public void ExtractPlainText_WhenValueContainsRichMarkup_ShouldReturnNormalizedText()
    {
        string result = this.sanitizer.ExtractPlainText("<p>Avis&nbsp;<strong>officiel</strong></p>");

        Assert.Equal("Avis officiel", result);
    }

    [Fact]
    public void SanitizeRichHtml_WhenCommentImageIsCanonical_ShouldKeepOnlySafeCanonicalAttributes()
    {
        string result = this.sanitizer.SanitizeRichHtml(
            "<p>Avant<img src=\"/images/ABCDEF0123456789ABCDEF0123456789\" alt=\" Vue &amp; test \" " +
            "class=\"ignored rich-text__image--right rich-text__image\" style=\"width:9999px\" onerror=\"alert(1)\">Après</p>");

        Assert.Contains("src=\"/images/abcdef0123456789abcdef0123456789\"", result);
        Assert.Contains("class=\"rich-text__image rich-text__image--right\"", result);
        HtmlDocument parsed = new HtmlDocument();
        parsed.LoadHtml(result);
        HtmlNode image = parsed.DocumentNode.Descendants("img").Single();
        Assert.Equal("Vue & test", HtmlEntity.DeEntitize(image.GetAttributeValue("alt", string.Empty)));
        Assert.DoesNotContain("style", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("data:image/png;base64,AAAA")]
    [InlineData("blob:https://example.com/id")]
    [InlineData("https://example.com/image.jpg")]
    [InlineData("//example.com/image.jpg")]
    [InlineData("/images/not-an-id")]
    [InlineData("/images/abcdef0123456789abcdef0123456789?width=100")]
    public void SanitizeRichHtml_WhenImageSourceIsNotCanonical_ShouldRemoveImage(string source)
    {
        string result = this.sanitizer.SanitizeRichHtml(
            $"<p>Texte<img src=\"{source}\" alt=\"interdit\"></p>");

        Assert.DoesNotContain("<img", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Texte", result);
    }

    [Fact]
    public void SanitizeRichHtml_WhenImageHasNoValidAlignment_ShouldDefaultToFull()
    {
        string result = this.sanitizer.SanitizeRichHtml(
            "<img src=\"/images/abcdef0123456789abcdef0123456789\" class=\"rich-text__image--unknown\">");

        Assert.Contains("class=\"rich-text__image rich-text__image--full\"", result);
    }

    [Fact]
    public void ExtractImageIds_WhenImagesRepeatAcrossLanguages_ShouldReturnDistinctCanonicalIds()
    {
        IReadOnlyCollection<string> result = this.sanitizer.ExtractImageIds(
            "<img src=\"/images/ABCDEF0123456789ABCDEF0123456789\">" +
            "<img src=\"/images/abcdef0123456789abcdef0123456789\">" +
            "<img src=\"https://example.com/image.jpg\">");

        Assert.Equal(
            new[] { "abcdef0123456789abcdef0123456789" },
            result);
    }

    [Fact]
    public void ExtractPlainText_WhenBodyContainsOnlyImage_ShouldRemainEmpty()
    {
        string result = this.sanitizer.ExtractPlainText(
            "<img src=\"/images/abcdef0123456789abcdef0123456789\" alt=\"Vue du parc\">");

        Assert.Empty(result);
    }

    [Fact]
    public void SanitizeRichHtml_WhenImageAltIsTooLong_ShouldTruncateItTo240Characters()
    {
        string result = this.sanitizer.SanitizeRichHtml(
            $"<img src=\"/images/abcdef0123456789abcdef0123456789\" alt=\"{new string('a', 241)}\">");
        HtmlDocument parsed = new HtmlDocument();
        parsed.LoadHtml(result);

        string alt = parsed.DocumentNode.Descendants("img").Single()
            .GetAttributeValue("alt", string.Empty);

        Assert.Equal(240, alt.Length);
    }
}
