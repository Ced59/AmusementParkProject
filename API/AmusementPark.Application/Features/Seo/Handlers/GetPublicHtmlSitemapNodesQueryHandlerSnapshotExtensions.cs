using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;

namespace AmusementPark.Application.Features.Seo.Handlers;

internal static class GetPublicHtmlSitemapNodesQueryHandlerSnapshotExtensions
{
    internal static async Task<ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>> BuildSnapshotBranchAsync(
        this GetPublicHtmlSitemapNodesQueryHandler handler,
        string language,
        string parentNodeId,
        IReadOnlyCollection<string> supportedLanguages,
        CancellationToken cancellationToken)
    {
        SitemapSnapshot? snapshot = await handler.sitemapSnapshotRepository.GetLatestAsync(cancellationToken);
        if (snapshot is null)
        {
            return ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>.Failure(
                ApplicationError.Technical("seo.html-sitemap.snapshot.unavailable", "The sitemap is temporarily unavailable."));
        }

        IReadOnlyCollection<string> languages = GetPublicHtmlSitemapNodesQueryHandler.NormalizeSupportedLanguages(supportedLanguages);
        List<SitemapSectionStats> sections = snapshot.Sections
            .Where(section => section.UrlCount > 0
                && section.UrlCount <= SitemapSectionChunker.MaxUrlsPerPublicSitemapFile
                && GetPublicHtmlSitemapNodesQueryHandler.IsSnapshotSectionRelevantForLanguage(section.Key, language, languages))
            .ToList();

        if (parentNodeId == "snapshot-sections")
        {
            IReadOnlyCollection<PublicHtmlSitemapNode> nodes = sections
                .Select(section => new PublicHtmlSitemapNode
                {
                    Id = $"sitemap-section:{section.Key}",
                    Label = PublicHtmlSitemapSnapshotLabels.Section(language, section.Key),
                    HasChildren = true,
                })
                .OrderBy(static node => node.Label, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static node => node.Id, StringComparer.Ordinal)
                .ToList();
            return ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>.Success(nodes);
        }

        string sectionKey = parentNodeId["sitemap-section:".Length..];
        SitemapSectionStats? selectedSection = sections.FirstOrDefault(section => string.Equals(section.Key, sectionKey, StringComparison.Ordinal));
        if (selectedSection is null)
        {
            return ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>.Failure(
                ApplicationError.NotFound("seo.html-sitemap.section.not-found", "The requested sitemap section is unavailable."));
        }

        // Read exactly one generated section; listing sections only reads metadata.
        string? sectionXml = await handler.sitemapSnapshotRepository.GetSectionXmlAsync(selectedSection.Key, cancellationToken);
        if (string.IsNullOrWhiteSpace(sectionXml))
        {
            return ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>.Failure(
                ApplicationError.Technical("seo.html-sitemap.section.unavailable", "The sitemap section is temporarily unavailable."));
        }

        IReadOnlyCollection<PublicHtmlSitemapNode> links = GetPublicHtmlSitemapNodesQueryHandler.ExtractSitemapLinkNodes(
            selectedSection.Key, sectionXml, language, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        if (links.Count == 0 || links.Count > SitemapSectionChunker.MaxUrlsPerPublicSitemapFile)
        {
            return ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>.Failure(
                ApplicationError.Technical("seo.html-sitemap.section.invalid", "The sitemap section is temporarily unavailable."));
        }

        IReadOnlyCollection<PublicHtmlSitemapNode> labelledLinks = links.Select(node => new PublicHtmlSitemapNode
        {
            Id = node.Id,
            Label = PublicHtmlSitemapSnapshotLabels.Link(language, node.RelativeUrl!, selectedSection.Key),
            RelativeUrl = node.RelativeUrl,
            HasChildren = false,
        }).ToList();
        return ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>.Success(labelledLinks);
    }
}
