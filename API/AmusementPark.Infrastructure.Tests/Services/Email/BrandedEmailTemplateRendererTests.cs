using AmusementPark.Infrastructure.Services.Email;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Email;

public sealed class BrandedEmailTemplateRendererTests
{
    [Fact]
    public void Render_WhenContentContainsMarkup_ShouldEscapeDynamicValues()
    {
        BrandedEmailTemplateRenderer renderer = new BrandedEmailTemplateRenderer();

        string html = renderer.Render(new BrandedEmailTemplateModel
        {
            Preheader = "Preview <script>",
            Badge = "Badge <b>",
            Title = "Hello <world>",
            Paragraphs = new[] { "Paragraph <strong>unsafe</strong>" },
            Action = new BrandedEmailAction("Open <now>", "https://example.test/path?a=1&b=2"),
            Metrics = new[] { new BrandedEmailMetric("Metric <x>", "Value <y>") },
            Highlight = new BrandedEmailHighlight("Message", "Line <script>alert(1)</script>"),
            FooterNote = "Footer <unsafe>",
        });

        Assert.Contains("AMUSEMENT-PARKS", html);
        Assert.Contains("Hello &lt;world&gt;", html);
        Assert.Contains("Paragraph &lt;strong&gt;unsafe&lt;/strong&gt;", html);
        Assert.Contains("https://example.test/path?a=1&amp;b=2", html);
        Assert.Contains("Line &lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
    }
}
