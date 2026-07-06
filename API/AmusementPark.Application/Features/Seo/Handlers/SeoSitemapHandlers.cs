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

public sealed class GenerateSitemapCommandHandler : ICommandHandler<GenerateSitemapCommand, ApplicationResult<SitemapGenerationResult>>
{
    private readonly SeoSitemapGenerationOrchestrator orchestrator;
    private readonly ISeoSitemapSettingsRepository settingsRepository;

    public GenerateSitemapCommandHandler(SeoSitemapGenerationOrchestrator orchestrator, ISeoSitemapSettingsRepository settingsRepository)
    {
        this.orchestrator = orchestrator;
        this.settingsRepository = settingsRepository;
    }

    public async Task<ApplicationResult<SitemapGenerationResult>> HandleAsync(GenerateSitemapCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        SeoSitemapSettings settings = await this.settingsRepository.GetAsync(cancellationToken);
        bool shouldSubmitIndexNow = command.SubmitToIndexNow &&
                                    settings.IsIndexNowEnabled &&
                                    ShouldSubmitForTrigger(command.Trigger, settings);

        SitemapGenerationResult result = await this.orchestrator.GenerateAsync(
            command.PublicBaseUrl,
            new SitemapGenerationContext
            {
                SupportedLanguages = command.SupportedLanguages,
            },
            command.Trigger,
            shouldSubmitIndexNow,
            command.TriggeredByUserId,
            command.TriggeredByUserEmail,
            cancellationToken);

        return ApplicationResult<SitemapGenerationResult>.Success(result);
    }

    private static bool ShouldSubmitForTrigger(SitemapGenerationTrigger trigger, SeoSitemapSettings settings)
    {
        return trigger switch
        {
            SitemapGenerationTrigger.Manual => settings.SubmitToIndexNowAfterManualGeneration,
            SitemapGenerationTrigger.Automatic => settings.SubmitToIndexNowAfterAutomaticGeneration,
            _ => false,
        };
    }
}

public sealed class UpdateSeoSitemapSettingsCommandHandler : ICommandHandler<UpdateSeoSitemapSettingsCommand, ApplicationResult<SeoSitemapSettings>>
{
    private readonly ISeoSitemapSettingsRepository settingsRepository;

    public UpdateSeoSitemapSettingsCommandHandler(ISeoSitemapSettingsRepository settingsRepository)
    {
        this.settingsRepository = settingsRepository;
    }

    public async Task<ApplicationResult<SeoSitemapSettings>> HandleAsync(UpdateSeoSitemapSettingsCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        string normalizedKey = Normalize(command.IndexNowKey);
        string normalizedKeyLocation = Normalize(command.IndexNowKeyLocation);
        if (command.IsIndexNowEnabled && string.IsNullOrWhiteSpace(normalizedKey))
        {
            return ApplicationResult<SeoSitemapSettings>.Failure(ApplicationErrors.Required("indexNowKey"));
        }

        SeoSitemapSettings settings = new SeoSitemapSettings
        {
            IsIndexNowEnabled = command.IsIndexNowEnabled,
            SubmitToIndexNowAfterManualGeneration = command.SubmitToIndexNowAfterManualGeneration,
            SubmitToIndexNowAfterAutomaticGeneration = command.SubmitToIndexNowAfterAutomaticGeneration,
            IndexNowKey = normalizedKey,
            IndexNowKeyLocation = normalizedKeyLocation,
            IndexNowEndpoints = NormalizeEndpoints(command.IndexNowEndpoints),
            UpdatedAtUtc = DateTime.UtcNow,
        };

        await this.settingsRepository.SaveAsync(settings, cancellationToken);
        return ApplicationResult<SeoSitemapSettings>.Success(settings);
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static IReadOnlyCollection<string> NormalizeEndpoints(IReadOnlyCollection<string> endpoints)
    {
        List<string> normalized = endpoints
            .Where(static endpoint => !string.IsNullOrWhiteSpace(endpoint))
            .Select(static endpoint => endpoint.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
        {
            normalized.Add("https://api.indexnow.org/indexnow");
            normalized.Add("https://www.bing.com/indexnow");
        }

        return normalized;
    }
}

public sealed class GetSeoSitemapOverviewQueryHandler : IQueryHandler<GetSeoSitemapOverviewQuery, ApplicationResult<SeoSitemapOverviewResult>>
{
    private readonly ISeoSitemapSnapshotRepository snapshotRepository;
    private readonly ISeoSitemapSettingsRepository settingsRepository;
    private readonly ISeoSitemapRuntimeStateStore runtimeStateStore;

    public GetSeoSitemapOverviewQueryHandler(
        ISeoSitemapSnapshotRepository snapshotRepository,
        ISeoSitemapSettingsRepository settingsRepository,
        ISeoSitemapRuntimeStateStore runtimeStateStore)
    {
        this.snapshotRepository = snapshotRepository;
        this.settingsRepository = settingsRepository;
        this.runtimeStateStore = runtimeStateStore;
    }

    public async Task<ApplicationResult<SeoSitemapOverviewResult>> HandleAsync(GetSeoSitemapOverviewQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SitemapSnapshot? snapshot = await this.snapshotRepository.GetLatestAsync(cancellationToken);
        SeoSitemapSettings settings = await this.settingsRepository.GetAsync(cancellationToken);
        string publicBaseUrl = SitemapXmlWriter.NormalizePublicBaseUrl(query.PublicBaseUrl);
        IReadOnlyCollection<SitemapSectionStats> sections = snapshot?.Sections ?? Array.Empty<SitemapSectionStats>();

        SeoSitemapOverviewResult result = new SeoSitemapOverviewResult
        {
            Runtime = this.runtimeStateStore.GetCurrent(),
            Snapshot = snapshot,
            Settings = settings,
            Sections = sections,
            TotalUrlCount = snapshot?.TotalUrlCount ?? 0,
            SitemapIndexUrl = $"{publicBaseUrl}/sitemap.xml",
            RobotsUrl = $"{publicBaseUrl}/robots.txt",
            IndexNowKeyFileUrl = BuildKeyFileUrl(publicBaseUrl, settings),
            PublicSitemapUrls = sections.Select(section => $"{publicBaseUrl}/{section.FileName}").ToList(),
        };

        return ApplicationResult<SeoSitemapOverviewResult>.Success(result);
    }

    private static string BuildKeyFileUrl(string publicBaseUrl, SeoSitemapSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.IndexNowKey))
        {
            return string.Empty;
        }

        string keyLocation = string.IsNullOrWhiteSpace(settings.IndexNowKeyLocation)
            ? $"/{settings.IndexNowKey}.txt"
            : settings.IndexNowKeyLocation.Trim();
        string normalizedLocation = keyLocation.StartsWith('/') ? keyLocation : $"/{keyLocation}";
        return $"{publicBaseUrl}{normalizedLocation}";
    }
}

public sealed class GetSeoSitemapSettingsQueryHandler : IQueryHandler<GetSeoSitemapSettingsQuery, ApplicationResult<SeoSitemapSettings>>
{
    private readonly ISeoSitemapSettingsRepository settingsRepository;

    public GetSeoSitemapSettingsQueryHandler(ISeoSitemapSettingsRepository settingsRepository)
    {
        this.settingsRepository = settingsRepository;
    }

    public async Task<ApplicationResult<SeoSitemapSettings>> HandleAsync(GetSeoSitemapSettingsQuery query, CancellationToken cancellationToken = default)
    {
        SeoSitemapSettings settings = await this.settingsRepository.GetAsync(cancellationToken);
        return ApplicationResult<SeoSitemapSettings>.Success(settings);
    }
}

public sealed class GetSeoSitemapHistoryQueryHandler : IQueryHandler<GetSeoSitemapHistoryQuery, ApplicationResult<PagedResult<SitemapGenerationHistoryEntry>>>
{
    private readonly ISeoSitemapGenerationHistoryRepository historyRepository;

    public GetSeoSitemapHistoryQueryHandler(ISeoSitemapGenerationHistoryRepository historyRepository)
    {
        this.historyRepository = historyRepository;
    }

    public async Task<ApplicationResult<PagedResult<SitemapGenerationHistoryEntry>>> HandleAsync(GetSeoSitemapHistoryQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0 || query.PageSize <= 0 || query.PageSize > 100)
        {
            return ApplicationResult<PagedResult<SitemapGenerationHistoryEntry>>.Failure(ApplicationErrors.InvalidPagination());
        }

        PagedResult<SitemapGenerationHistoryEntry> page = await this.historyRepository.SearchAsync(new PagedQuery(query.Page, query.PageSize), cancellationToken);
        return ApplicationResult<PagedResult<SitemapGenerationHistoryEntry>>.Success(page);
    }
}

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
                submitToIndexNow: false,
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
            return ApplicationResult<SitemapDocumentResult>.Success(new SitemapDocumentResult
            {
                Content = this.sitemapXmlWriter.WriteSitemapIndex(
                    query.PublicBaseUrl,
                    SitemapSectionChunker.ExpandSections(snapshot.Sections)),
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
                Content = sectionXml,
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
                return ApplicationResult<SitemapDocumentResult>.Success(new SitemapDocumentResult
                {
                    Content = SitemapSectionChunker.BuildChunkXml(baseSectionXml, chunkIndex),
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

}
