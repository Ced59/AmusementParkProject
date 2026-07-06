using System.Xml.Linq;
using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Services;

public static class SitemapSectionChunker
{
    public const int MaxUrlsPerPublicSitemapFile = 200;

    private static readonly XNamespace SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    public static IReadOnlyCollection<SitemapSectionStats> ExpandSections(IReadOnlyCollection<SitemapSectionStats> sections)
    {
        List<SitemapSectionStats> expandedSections = new List<SitemapSectionStats>();

        foreach (SitemapSectionStats section in sections)
        {
            if (section.UrlCount <= MaxUrlsPerPublicSitemapFile)
            {
                expandedSections.Add(section);
                continue;
            }

            int chunkCount = CalculateChunkCount(section.UrlCount);
            for (int chunkIndex = 1; chunkIndex <= chunkCount; chunkIndex++)
            {
                expandedSections.Add(CreateChunkStats(section, chunkIndex));
            }
        }

        return expandedSections;
    }

    public static IReadOnlyCollection<IReadOnlyCollection<SitemapUrlEntry>> SplitUrls(IReadOnlyCollection<SitemapUrlEntry> urls)
    {
        if (urls.Count <= MaxUrlsPerPublicSitemapFile)
        {
            return new[] { urls };
        }

        return urls
            .Select((url, index) => new { url, index })
            .GroupBy(item => item.index / MaxUrlsPerPublicSitemapFile)
            .Select(group => (IReadOnlyCollection<SitemapUrlEntry>)group.Select(item => item.url).ToList())
            .ToList();
    }

    public static bool TryResolveChunkRequest(
        string normalizedSectionKey,
        IReadOnlyCollection<SitemapSectionStats> sections,
        out SitemapSectionStats baseSection,
        out int chunkIndex)
    {
        baseSection = null!;
        chunkIndex = 0;

        int separatorIndex = normalizedSectionKey.LastIndexOf('-');
        if (separatorIndex <= 0 || separatorIndex == normalizedSectionKey.Length - 1)
        {
            return false;
        }

        string chunkIndexValue = normalizedSectionKey[(separatorIndex + 1)..];
        if (!int.TryParse(chunkIndexValue, out int parsedChunkIndex) || parsedChunkIndex <= 0)
        {
            return false;
        }

        string baseKey = normalizedSectionKey[..separatorIndex];
        SitemapSectionStats? matchingSection = sections.FirstOrDefault(section =>
            string.Equals(section.Key, baseKey, StringComparison.OrdinalIgnoreCase));

        if (matchingSection is null || matchingSection.UrlCount <= MaxUrlsPerPublicSitemapFile)
        {
            return false;
        }

        int chunkCount = CalculateChunkCount(matchingSection.UrlCount);
        if (parsedChunkIndex > chunkCount)
        {
            return false;
        }

        baseSection = matchingSection;
        chunkIndex = parsedChunkIndex;
        return true;
    }

    public static string BuildChunkXml(string sectionXml, int chunkIndex)
    {
        XDocument document = XDocument.Parse(sectionXml, LoadOptions.None);
        List<XElement> urlElements = document
            .Descendants(SitemapNamespace + "url")
            .Skip((chunkIndex - 1) * MaxUrlsPerPublicSitemapFile)
            .Take(MaxUrlsPerPublicSitemapFile)
            .Select(static element => new XElement(element))
            .ToList();

        XDocument chunkDocument = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(SitemapNamespace + "urlset", urlElements));

        string body = chunkDocument.ToString(SaveOptions.DisableFormatting);
        return chunkDocument.Declaration is null
            ? body
            : $"{chunkDocument.Declaration}{Environment.NewLine}{body}";
    }

    private static int CalculateChunkCount(int urlCount)
    {
        return (int)Math.Ceiling(urlCount / (double)MaxUrlsPerPublicSitemapFile);
    }

    private static SitemapSectionStats CreateChunkStats(SitemapSectionStats section, int chunkIndex)
    {
        int urlsBeforeChunk = (chunkIndex - 1) * MaxUrlsPerPublicSitemapFile;
        int remainingUrls = Math.Max(0, section.UrlCount - urlsBeforeChunk);
        int chunkUrlCount = Math.Min(MaxUrlsPerPublicSitemapFile, remainingUrls);

        return new SitemapSectionStats(
            $"{section.Key}-{chunkIndex}",
            $"{BuildFileNameWithoutExtension(section.FileName, section.Key)}-{chunkIndex}.xml",
            $"{section.DisplayName} partie {chunkIndex}",
            chunkUrlCount,
            section.LastModifiedUtc);
    }

    private static string BuildFileNameWithoutExtension(string fileName, string fallbackKey)
    {
        string value = string.IsNullOrWhiteSpace(fileName) ? fallbackKey : fileName.Trim();
        if (value.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^4];
        }

        return string.IsNullOrWhiteSpace(value) ? "sitemap" : value.ToLowerInvariant();
    }
}
