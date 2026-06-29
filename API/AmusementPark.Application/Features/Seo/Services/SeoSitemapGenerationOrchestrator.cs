using System.Diagnostics;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;

namespace AmusementPark.Application.Features.Seo.Services;

/// <summary>
/// Orchestre la génération complète du sitemap index et des sitemaps sectionnés.
/// </summary>
public sealed class SeoSitemapGenerationOrchestrator
{
    private const int MaxUrlsPerSitemapFile = 50000;
    private const int MaxIndexNowUrlsPerSitemapGeneration = 100;

    private readonly IReadOnlyCollection<ISitemapSectionProvider> sectionProviders;
    private readonly ISitemapXmlWriter sitemapXmlWriter;
    private readonly ISeoSitemapSnapshotRepository snapshotRepository;
    private readonly ISeoSitemapGenerationHistoryRepository historyRepository;
    private readonly ISeoSitemapSettingsRepository settingsRepository;
    private readonly IIndexNowSubmitter indexNowSubmitter;
    private readonly ISeoSitemapRuntimeStateStore runtimeStateStore;

    public SeoSitemapGenerationOrchestrator(
        IEnumerable<ISitemapSectionProvider> sectionProviders,
        ISitemapXmlWriter sitemapXmlWriter,
        ISeoSitemapSnapshotRepository snapshotRepository,
        ISeoSitemapGenerationHistoryRepository historyRepository,
        ISeoSitemapSettingsRepository settingsRepository,
        IIndexNowSubmitter indexNowSubmitter,
        ISeoSitemapRuntimeStateStore runtimeStateStore)
    {
        this.sectionProviders = sectionProviders.OrderBy(GetSectionOrder).ThenBy(static provider => provider.Key, StringComparer.OrdinalIgnoreCase).ToList();
        this.sitemapXmlWriter = sitemapXmlWriter;
        this.snapshotRepository = snapshotRepository;
        this.historyRepository = historyRepository;
        this.settingsRepository = settingsRepository;
        this.indexNowSubmitter = indexNowSubmitter;
        this.runtimeStateStore = runtimeStateStore;
    }

    public async Task<SitemapGenerationResult> GenerateAsync(
        string publicBaseUrl,
        SitemapGenerationContext context,
        SitemapGenerationTrigger trigger,
        bool submitToIndexNow,
        string? triggeredByUserId,
        string? triggeredByUserEmail,
        CancellationToken cancellationToken)
    {
        string generationId = Guid.NewGuid().ToString("N");
        DateTime startedAtUtc = DateTime.UtcNow;
        Stopwatch stopwatch = Stopwatch.StartNew();

        if (!this.runtimeStateStore.TryStart("starting"))
        {
            return new SitemapGenerationResult
            {
                Id = generationId,
                StartedAtUtc = startedAtUtc,
                CompletedAtUtc = DateTime.UtcNow,
                DurationMs = 0,
                Status = SitemapGenerationStatus.Skipped,
                Trigger = trigger,
                Errors = new[] { "Une génération de sitemap est déjà en cours." },
            };
        }

        try
        {
            this.runtimeStateStore.Update("collecting-urls", 15, "Collecte des URLs publiques indexables.");
            List<SitemapSectionBuildResult> builtSections = new List<SitemapSectionBuildResult>();
            foreach (ISitemapSectionProvider provider in this.sectionProviders)
            {
                IReadOnlyCollection<SitemapUrlEntry> providerUrls = await provider.GetUrlsAsync(context, cancellationToken);
                IReadOnlyCollection<SitemapUrlEntry> distinctUrls = GetDistinctUrls(providerUrls);

                foreach (SitemapSectionBuildResult languageSection in SplitProviderSectionByLanguage(provider, distinctUrls, context.SupportedLanguages))
                {
                    builtSections.Add(languageSection);
                }
            }

            this.runtimeStateStore.Update("writing-xml", 45, "Écriture du sitemap index et des sections XML.");
            Dictionary<string, string> sectionXmlByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            List<SitemapSectionStats> sectionStats = new List<SitemapSectionStats>();
            foreach (SitemapSectionBuildResult section in builtSections)
            {
                sectionXmlByKey[section.Key] = this.sitemapXmlWriter.WriteUrlSet(publicBaseUrl, section.Urls);
                sectionStats.Add(new SitemapSectionStats(
                    section.Key,
                    section.FileName,
                    section.DisplayName,
                    section.Urls.Count,
                    ResolveLastModified(section.Urls)));
            }

            string indexXml = this.sitemapXmlWriter.WriteSitemapIndex(publicBaseUrl, sectionStats);
            SitemapSnapshot snapshot = new SitemapSnapshot
            {
                Id = "current",
                GeneratedAtUtc = DateTime.UtcNow,
                PublicBaseUrl = SitemapXmlWriter.NormalizePublicBaseUrl(publicBaseUrl),
                IndexXml = indexXml,
                SectionXmlByKey = sectionXmlByKey,
                Sections = sectionStats,
                TotalUrlCount = sectionStats.Sum(static stats => stats.UrlCount),
            };

            this.runtimeStateStore.Update("saving", 70, "Persistance du snapshot sitemap.");
            await this.snapshotRepository.SaveAsync(snapshot, cancellationToken);

            IndexNowSubmissionResult indexNowResult = await this.SubmitIndexNowIfRequiredAsync(
                publicBaseUrl,
                submitToIndexNow,
                builtSections,
                cancellationToken);

            stopwatch.Stop();
            SitemapGenerationResult result = new SitemapGenerationResult
            {
                Id = generationId,
                StartedAtUtc = startedAtUtc,
                CompletedAtUtc = DateTime.UtcNow,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Status = SitemapGenerationStatus.Succeeded,
                Trigger = trigger,
                TotalUrlCount = snapshot.TotalUrlCount,
                Sections = sectionStats,
                IndexNow = indexNowResult,
            };

            await this.historyRepository.WriteAsync(ToHistoryEntry(result, triggeredByUserId, triggeredByUserEmail), cancellationToken);
            this.runtimeStateStore.Complete("completed", $"Sitemap généré : {snapshot.TotalUrlCount} URLs.");
            return result;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            stopwatch.Stop();
            SitemapGenerationResult result = new SitemapGenerationResult
            {
                Id = generationId,
                StartedAtUtc = startedAtUtc,
                CompletedAtUtc = DateTime.UtcNow,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Status = SitemapGenerationStatus.Failed,
                Trigger = trigger,
                Errors = new[] { exception.Message },
            };

            await this.historyRepository.WriteAsync(ToHistoryEntry(result, triggeredByUserId, triggeredByUserEmail), cancellationToken);
            this.runtimeStateStore.Fail("failed", exception.Message);
            return result;
        }
    }

    private static IReadOnlyCollection<SitemapSectionBuildResult> SplitProviderSectionByLanguage(
        ISitemapSectionProvider provider,
        IReadOnlyCollection<SitemapUrlEntry> urls,
        IReadOnlyCollection<string> supportedLanguages)
    {
        List<string> languages = NormalizeLanguages(supportedLanguages);
        List<SitemapSectionBuildResult> sections = new List<SitemapSectionBuildResult>();

        string baseFileName = NormalizeBaseFileName(provider.FileName, provider.Key);
        Dictionary<string, List<SitemapUrlEntry>> urlsByLanguage = new Dictionary<string, List<SitemapUrlEntry>>(StringComparer.OrdinalIgnoreCase);
        foreach (string language in languages)
        {
            urlsByLanguage[language] = new List<SitemapUrlEntry>();
        }

        List<SitemapUrlEntry> unscopedUrls = new List<SitemapUrlEntry>();
        foreach (SitemapUrlEntry url in urls)
        {
            string normalizedPath = NormalizeRelativePath(url.RelativePath);
            string? language = ResolveUrlLanguage(normalizedPath, languages);
            if (language is null)
            {
                unscopedUrls.Add(url);
                continue;
            }

            urlsByLanguage[language].Add(url);
        }

        foreach (string language in languages)
        {
            List<SitemapUrlEntry> languageUrls = urlsByLanguage[language];
            languageUrls.Sort(CompareUrlsByRelativePath);

            if (languageUrls.Count == 0)
            {
                continue;
            }

            AddChunkedSections(
                sections,
                $"{provider.Key}-{language}",
                $"{baseFileName}-{language}",
                $"{provider.DisplayName} · {language.ToUpperInvariant()}",
                languageUrls);
        }

        unscopedUrls.Sort(CompareUrlsByRelativePath);

        if (unscopedUrls.Count > 0)
        {
            AddChunkedSections(
                sections,
                $"{provider.Key}-global",
                $"{baseFileName}-global",
                $"{provider.DisplayName} · Global",
                unscopedUrls);
        }

        return sections;
    }

    private static IReadOnlyCollection<SitemapUrlEntry> GetDistinctUrls(IReadOnlyCollection<SitemapUrlEntry> urls)
    {
        Dictionary<string, SitemapUrlEntry> urlsByPath = new Dictionary<string, SitemapUrlEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (SitemapUrlEntry url in urls)
        {
            if (string.IsNullOrWhiteSpace(url.RelativePath))
            {
                continue;
            }

            string normalizedPath = NormalizeRelativePath(url.RelativePath);
            if (!urlsByPath.TryGetValue(normalizedPath, out SitemapUrlEntry? existingUrl) || IsNewerUrl(url, existingUrl))
            {
                urlsByPath[normalizedPath] = url;
            }
        }

        List<SitemapUrlEntry> distinctUrls = urlsByPath.Values.ToList();
        distinctUrls.Sort(CompareUrlsByRelativePath);
        return distinctUrls;
    }

    private static bool IsNewerUrl(SitemapUrlEntry candidate, SitemapUrlEntry existing)
    {
        if (!candidate.LastModifiedUtc.HasValue)
        {
            return false;
        }

        if (!existing.LastModifiedUtc.HasValue)
        {
            return true;
        }

        return candidate.LastModifiedUtc.Value > existing.LastModifiedUtc.Value;
    }

    private static void AddChunkedSections(
        List<SitemapSectionBuildResult> sections,
        string baseKey,
        string baseFileNameWithoutExtension,
        string displayName,
        IReadOnlyCollection<SitemapUrlEntry> urls)
    {
        if (urls.Count <= MaxUrlsPerSitemapFile)
        {
            sections.Add(new SitemapSectionBuildResult(
                baseKey,
                $"{baseFileNameWithoutExtension}.xml",
                displayName,
                urls));
            return;
        }

        int chunkIndex = 1;
        foreach (IReadOnlyCollection<SitemapUrlEntry> chunk in urls
                     .Select((url, index) => new { url, index })
                     .GroupBy(item => item.index / MaxUrlsPerSitemapFile)
                     .Select(group => (IReadOnlyCollection<SitemapUrlEntry>)group.Select(item => item.url).ToList()))
        {
            sections.Add(new SitemapSectionBuildResult(
                $"{baseKey}-{chunkIndex}",
                $"{baseFileNameWithoutExtension}-{chunkIndex}.xml",
                $"{displayName} · partie {chunkIndex}",
                chunk));

            chunkIndex++;
        }
    }

    private static string NormalizeBaseFileName(string fileName, string fallbackKey)
    {
        string value = string.IsNullOrWhiteSpace(fileName) ? fallbackKey : fileName.Trim();
        if (value.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^4];
        }

        return string.IsNullOrWhiteSpace(value) ? "sitemap" : value.ToLowerInvariant();
    }

    private static List<string> NormalizeLanguages(IReadOnlyCollection<string> languages)
    {
        List<string> normalizedLanguages = languages
            .Where(static language => !string.IsNullOrWhiteSpace(language))
            .Select(static language => language.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalizedLanguages.Count > 0 ? normalizedLanguages : new List<string> { "en" };
    }

    private static bool IsUrlForLanguage(string relativePath, string language)
    {
        string normalizedPath = NormalizeRelativePath(relativePath);
        string normalizedLanguage = language.Trim().ToLowerInvariant();

        return normalizedPath.StartsWith($"/{normalizedLanguage}/", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedPath, $"/{normalizedLanguage}", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveUrlLanguage(string normalizedPath, IReadOnlyCollection<string> languages)
    {
        foreach (string language in languages)
        {
            if (normalizedPath.StartsWith($"/{language}/", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedPath, $"/{language}", StringComparison.OrdinalIgnoreCase))
            {
                return language;
            }
        }

        return null;
    }

    private static int CompareUrlsByRelativePath(SitemapUrlEntry first, SitemapUrlEntry second)
    {
        return string.Compare(first.RelativePath, second.RelativePath, StringComparison.OrdinalIgnoreCase);
    }


    private static int GetSectionOrder(ISitemapSectionProvider provider)
    {
        string key = provider.Key;
        if (string.Equals(key, SitemapSectionKeys.Static, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (string.Equals(key, SitemapSectionKeys.Parks, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(key, SitemapSectionKeys.ParkImages, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (string.Equals(key, SitemapSectionKeys.ParkVideos, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (string.Equals(key, SitemapSectionKeys.ParkItemLists, StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (string.Equals(key, SitemapSectionKeys.ParkZones, StringComparison.OrdinalIgnoreCase))
        {
            return 5;
        }

        if (string.Equals(key, SitemapSectionKeys.ParkItems, StringComparison.OrdinalIgnoreCase))
        {
            return 6;
        }

        if (string.Equals(key, SitemapSectionKeys.ParkItemImages, StringComparison.OrdinalIgnoreCase))
        {
            return 7;
        }

        if (string.Equals(key, SitemapSectionKeys.ParkItemVideos, StringComparison.OrdinalIgnoreCase))
        {
            return 8;
        }

        if (string.Equals(key, SitemapSectionKeys.References, StringComparison.OrdinalIgnoreCase))
        {
            return 9;
        }

        if (string.Equals(key, SitemapSectionKeys.TechnicalPages, StringComparison.OrdinalIgnoreCase))
        {
            return 10;
        }

        return 100;
    }

    private async Task<IndexNowSubmissionResult> SubmitIndexNowIfRequiredAsync(
        string publicBaseUrl,
        bool submitToIndexNow,
        IReadOnlyCollection<SitemapSectionBuildResult> builtSections,
        CancellationToken cancellationToken)
    {
        SeoSitemapSettings settings = await this.settingsRepository.GetAsync(cancellationToken);
        if (!submitToIndexNow || !settings.IsIndexNowEnabled)
        {
            return new IndexNowSubmissionResult
            {
                WasRequested = submitToIndexNow,
                IsEnabled = settings.IsIndexNowEnabled,
                IsSuccess = !submitToIndexNow,
            };
        }

        this.runtimeStateStore.Update("indexnow", 86, "Soumission IndexNow des URLs générées.");
        string normalizedPublicBaseUrl = SitemapXmlWriter.NormalizePublicBaseUrl(publicBaseUrl);
        List<string> absoluteUrls = builtSections
            .SelectMany(static section => section.Urls)
            .Select(url => $"{normalizedPublicBaseUrl}{NormalizeRelativePath(url.RelativePath)}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (absoluteUrls.Count > MaxIndexNowUrlsPerSitemapGeneration)
        {
            return new IndexNowSubmissionResult
            {
                WasRequested = true,
                IsEnabled = true,
                IsSuccess = false,
                SubmittedUrlCount = 0,
                Errors = new[]
                {
                    $"Soumission IndexNow ignoree pour {absoluteUrls.Count} URLs de sitemap : les mises a jour publiques sont soumises selectivement.",
                },
            };
        }

        return await this.indexNowSubmitter.SubmitAsync(settings, normalizedPublicBaseUrl, absoluteUrls, cancellationToken);
    }

    private static SitemapGenerationHistoryEntry ToHistoryEntry(SitemapGenerationResult result, string? triggeredByUserId, string? triggeredByUserEmail)
    {
        return new SitemapGenerationHistoryEntry
        {
            Id = result.Id,
            StartedAtUtc = result.StartedAtUtc,
            CompletedAtUtc = result.CompletedAtUtc,
            DurationMs = result.DurationMs,
            Status = result.Status,
            Trigger = result.Trigger,
            TriggeredByUserId = triggeredByUserId,
            TriggeredByUserEmail = triggeredByUserEmail,
            TotalUrlCount = result.TotalUrlCount,
            Sections = result.Sections,
            Errors = result.Errors,
            IndexNow = result.IndexNow,
        };
    }

    private static DateTime? ResolveLastModified(IReadOnlyCollection<SitemapUrlEntry> urls)
    {
        DateTime? lastModifiedUtc = null;
        foreach (SitemapUrlEntry url in urls)
        {
            if (!url.LastModifiedUtc.HasValue)
            {
                continue;
            }

            DateTime candidateUtc = url.LastModifiedUtc.Value.ToUniversalTime();
            if (!lastModifiedUtc.HasValue || candidateUtc > lastModifiedUtc.Value)
            {
                lastModifiedUtc = candidateUtc;
            }
        }

        return lastModifiedUtc ?? DateTime.UtcNow;
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        string value = relativePath.Trim();
        return value.StartsWith('/') ? value : $"/{value}";
    }
}
