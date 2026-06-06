using System.Globalization;
using System.Text;
using System.Xml;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;

namespace AmusementPark.Application.Features.Seo.Services;

/// <summary>
/// Écriture XML stricte des sitemaps et sitemap index.
/// </summary>
public sealed class SitemapXmlWriter : ISitemapXmlWriter
{
    public string WriteUrlSet(string publicBaseUrl, IReadOnlyCollection<SitemapUrlEntry> urls)
    {
        string normalizedBaseUrl = NormalizePublicBaseUrl(publicBaseUrl);
        XmlWriterSettings settings = CreateSettings();
        using StringWriterWithEncoding stringWriter = new StringWriterWithEncoding(Encoding.UTF8);
        using (XmlWriter writer = XmlWriter.Create(stringWriter, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            foreach (SitemapUrlEntry url in urls.OrderBy(static value => value.RelativePath, StringComparer.OrdinalIgnoreCase))
            {
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", $"{normalizedBaseUrl}{NormalizeRelativePath(url.RelativePath)}");

                if (url.LastModifiedUtc.HasValue)
                {
                    writer.WriteElementString("lastmod", url.LastModifiedUtc.Value.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }

                if (!string.IsNullOrWhiteSpace(url.ChangeFrequency))
                {
                    writer.WriteElementString("changefreq", url.ChangeFrequency.Trim().ToLowerInvariant());
                }

                if (url.Priority.HasValue)
                {
                    decimal priority = Math.Clamp(url.Priority.Value, 0m, 1m);
                    writer.WriteElementString("priority", priority.ToString("0.0", CultureInfo.InvariantCulture));
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return stringWriter.ToString();
    }

    public string WriteSitemapIndex(string publicBaseUrl, IReadOnlyCollection<SitemapSectionStats> sections)
    {
        string normalizedBaseUrl = NormalizePublicBaseUrl(publicBaseUrl);
        XmlWriterSettings settings = CreateSettings();
        using StringWriterWithEncoding stringWriter = new StringWriterWithEncoding(Encoding.UTF8);
        using (XmlWriter writer = XmlWriter.Create(stringWriter, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("sitemapindex", "http://www.sitemaps.org/schemas/sitemap/0.9");

            foreach (SitemapSectionStats section in sections)
            {
                writer.WriteStartElement("sitemap");
                writer.WriteElementString("loc", $"{normalizedBaseUrl}/sitemaps/{section.FileName}");

                if (section.LastModifiedUtc.HasValue)
                {
                    writer.WriteElementString("lastmod", section.LastModifiedUtc.Value.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return stringWriter.ToString();
    }

    public static string NormalizePublicBaseUrl(string publicBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            return "https://amusement-parks.fun";
        }

        return publicBaseUrl.Trim().TrimEnd('/');
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return "/";
        }

        string value = relativePath.Trim();
        return value.StartsWith('/') ? value : $"/{value}";
    }

    private static XmlWriterSettings CreateSettings()
    {
        return new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            OmitXmlDeclaration = false,
        };
    }

    private sealed class StringWriterWithEncoding : StringWriter
    {
        private readonly Encoding encoding;

        public StringWriterWithEncoding(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public override Encoding Encoding => this.encoding;
    }
}
