using System.Xml.Linq;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Commands;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.Seo.Results;
using AmusementPark.Application.Features.Seo.Services;

namespace AmusementPark.Application.Features.Seo.Handlers;

public sealed class GetPublicSitemapDocumentQueryHandler : IQueryHandler<GetPublicSitemapDocumentQuery, ApplicationResult<SitemapDocumentResult>>
{
    private readonly ISeoSitemapSnapshotRepository snapshotRepository;
    private readonly SeoSitemapGenerationOrchestrator orchestrator;
    private readonly ISitemapXmlWriter sitemapXmlWriter;

    public GetPublicSitemapDocumentQueryHandler(
        ISeoSitemapSnapshotRepository snapshotRepository,
        SeoSitemapGenerationOrchestrator orchestrator,
        ISitemapXmlWriter sitemapXmlWriter)
    {
        this.snapshotRepository = snapshotRepository;
        this.orchestrator = orchestrator;
        this.sitemapXmlWriter = sitemapXmlWriter;
    }

    public async Task<ApplicationResult<SitemapDocumentResult>> HandleAsync(GetPublicSitemapDocumentQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SitemapSnapshot? snapshot = await this.snapshotRepository.GetLatestAsync(cancellationToken);
        bool wasGeneratedOnDemand = false;

        if (snapshot is null)
        {
            SitemapGenerationResult fallbackResult = await this.orchestrator.GenerateAsync(
                query.PublicBaseUrl,
                new SitemapGenerationContext
                {
                    SupportedLanguages = query.SupportedLanguages,
                },
                SitemapGenerationTrigger.PublicFallback,
                triggeredByUserId: null,
                triggeredByUserEmail: null,
                cancellationToken);

            if (fallbackResult.Status == SitemapGenerationStatus.Succeeded)
            {
                snapshot = await this.snapshotRepository.GetLatestAsync(cancellationToken);
                wasGeneratedOnDemand = true;
            }

            if (snapshot is null)
            {
                return ApplicationResult<SitemapDocumentResult>.Failure(ApplicationError.NotFound("seo.sitemap.not-found", "Aucun sitemap généré n'est disponible."));
            }
        }

        if (string.IsNullOrWhiteSpace(query.SectionKey))
        {
            IReadOnlyCollection<SitemapSectionStats> publicSections = RemoveLegacyStaticLastModifiedDates(snapshot.Sections);
            return ApplicationResult<SitemapDocumentResult>.Success(new SitemapDocumentResult
            {
                Content = this.sitemapXmlWriter.WriteSitemapIndex(
                    query.PublicBaseUrl,
                    SitemapSectionChunker.ExpandSections(publicSections)),
                WasGeneratedOnDemand = wasGeneratedOnDemand,
            });
        }

        string normalizedSectionKey = NormalizeSectionKey(query.SectionKey);
        SitemapSectionStats? directSection = FindSection(snapshot.Sections, normalizedSectionKey);
        string? sectionXml = await this.snapshotRepository.GetSectionXmlAsync(normalizedSectionKey, cancellationToken);
        if (sectionXml is not null)
        {
            if (directSection is not null && directSection.UrlCount > SitemapSectionChunker.MaxUrlsPerPublicSitemapFile)
            {
                return ApplicationResult<SitemapDocumentResult>.Success(new SitemapDocumentResult
                {
                    Content = this.sitemapXmlWriter.WriteSitemapIndex(
                        query.PublicBaseUrl,
                        SitemapSectionChunker.ExpandSections(new[] { directSection })),
                    WasGeneratedOnDemand = wasGeneratedOnDemand,
                });
            }

            return ApplicationResult<SitemapDocumentResult>.Success(new SitemapDocumentResult
            {
                Content = IsStaticSection(normalizedSectionKey)
                    ? RemoveLastModifiedElements(sectionXml)
                    : sectionXml,
                WasGeneratedOnDemand = wasGeneratedOnDemand,
            });
        }

        if (SitemapSectionChunker.TryResolveChunkRequest(
                normalizedSectionKey,
                snapshot.Sections,
                out SitemapSectionStats baseSection,
                out int chunkIndex))
        {
            string? baseSectionXml = await this.snapshotRepository.GetSectionXmlAsync(baseSection.Key, cancellationToken);
            if (baseSectionXml is not null)
            {
                string publicSectionXml = IsStaticSection(baseSection.Key)
                    ? RemoveLastModifiedElements(baseSectionXml)
                    : baseSectionXml;
                return ApplicationResult<SitemapDocumentResult>.Success(new SitemapDocumentResult
                {
                    Content = SitemapSectionChunker.BuildChunkXml(publicSectionXml, chunkIndex),
                    WasGeneratedOnDemand = wasGeneratedOnDemand,
                });
            }
        }

        return ApplicationResult<SitemapDocumentResult>.Failure(ApplicationError.NotFound("seo.sitemap-section.not-found", $"La section sitemap '{query.SectionKey}' est introuvable."));
    }

    private static string NormalizeSectionKey(string sectionKeyOrFileName)
    {
        string value = sectionKeyOrFileName.Trim();
        if (value.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^4];
        }

        return value.ToLowerInvariant();
    }

    private static SitemapSectionStats? FindSection(IReadOnlyCollection<SitemapSectionStats> sections, string normalizedSectionKey)
    {
        return sections.FirstOrDefault(section =>
            string.Equals(section.Key, normalizedSectionKey, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyCollection<SitemapSectionStats> RemoveLegacyStaticLastModifiedDates(
        IReadOnlyCollection<SitemapSectionStats> sections)
    {
        return sections
            .Select(static section => IsStaticSection(section.Key) && section.LastModifiedUtc.HasValue
                ? section with { LastModifiedUtc = null }
                : section)
            .ToList();
    }

    private static bool IsStaticSection(string sectionKey)
    {
        return string.Equals(sectionKey, SitemapSectionKeys.Static, StringComparison.OrdinalIgnoreCase)
            || sectionKey.StartsWith($"{SitemapSectionKeys.Static}-", StringComparison.OrdinalIgnoreCase);
    }

    private static string RemoveLastModifiedElements(string sitemapXml)
    {
        XDocument document = XDocument.Parse(sitemapXml, LoadOptions.PreserveWhitespace);
        document
            .Descendants()
            .Where(static element => string.Equals(element.Name.LocalName, "lastmod", StringComparison.OrdinalIgnoreCase))
            .Remove();
        return document.ToString(SaveOptions.DisableFormatting);
    }

}
