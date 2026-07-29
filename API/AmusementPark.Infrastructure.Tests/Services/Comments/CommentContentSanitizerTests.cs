using AmusementPark.Infrastructure.Services.Comments;
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
}
