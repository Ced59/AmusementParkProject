using System.Xml.Linq;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Services;

public sealed class SitemapXmlWriterTests
{
    [Theory]
    [InlineData(null, "https://amusement-parks.fun")]
    [InlineData("", "https://amusement-parks.fun")]
    [InlineData(" https://example.com/ ", "https://example.com")]
    public void NormalizePublicBaseUrl_WhenValueProvided_ShouldNormalizeOrFallback(string? value, string expected)
    {
        string result = SitemapXmlWriter.NormalizePublicBaseUrl(value!);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteUrlSet_WhenUrlsProvided_ShouldWriteOrderedXmlWithNormalizedValues()
    {
        SitemapXmlWriter writer = new SitemapXmlWriter();
        SitemapUrlEntry[] urls = new[]
        {
            new SitemapUrlEntry("fr/b", new DateTime(2026, 6, 6, 12, 0, 0, DateTimeKind.Local), " Weekly ", 1.5m),
            new SitemapUrlEntry("/fr/a", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), "Daily", -1m),
        };

        string xml = writer.WriteUrlSet("https://example.com/", urls);
        XDocument document = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        List<XElement> urlElements = document.Root!.Elements(ns + "url").ToList();

        Assert.Equal("urlset", document.Root.Name.LocalName);
        Assert.Equal("https://example.com/fr/a", urlElements[0].Element(ns + "loc")!.Value);
        Assert.Equal("0.0", urlElements[0].Element(ns + "priority")!.Value);
        Assert.Equal("https://example.com/fr/b", urlElements[1].Element(ns + "loc")!.Value);
        Assert.Equal("weekly", urlElements[1].Element(ns + "changefreq")!.Value);
        Assert.Equal("1.0", urlElements[1].Element(ns + "priority")!.Value);
    }

    [Fact]
    public void WriteSitemapIndex_WhenSectionsProvided_ShouldWriteSitemapLocations()
    {
        SitemapXmlWriter writer = new SitemapXmlWriter();
        SitemapSectionStats[] sections = new[]
        {
            new SitemapSectionStats("static", "static.xml", "Static", 2, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc)),
            new SitemapSectionStats("parks", "parks.xml", "Parks", 3, null),
        };

        string xml = writer.WriteSitemapIndex("https://example.com/", sections);
        XDocument document = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        List<XElement> sitemapElements = document.Root!.Elements(ns + "sitemap").ToList();

        Assert.Equal("sitemapindex", document.Root.Name.LocalName);
        Assert.Equal("https://example.com/static.xml", sitemapElements[0].Element(ns + "loc")!.Value);
        Assert.Equal("2026-06-06", sitemapElements[0].Element(ns + "lastmod")!.Value);
        Assert.Equal("https://example.com/parks.xml", sitemapElements[1].Element(ns + "loc")!.Value);
        Assert.Null(sitemapElements[1].Element(ns + "lastmod"));
    }
}
