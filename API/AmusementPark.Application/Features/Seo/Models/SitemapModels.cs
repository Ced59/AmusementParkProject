namespace AmusementPark.Application.Features.Seo.Models;

/// <summary>
/// Clés stables des sections de sitemap exposées publiquement.
/// </summary>
public static class SitemapSectionKeys
{
    public const string Static = "static";
    public const string Parks = "parks";
    public const string ParkOpeningHours = "park-opening-hours";
    public const string History = "history";
    public const string HistoryArticles = "history-articles";
    public const string ParkImages = "park-images";
    public const string ParkVideos = "park-videos";
    public const string ParkItemLists = "park-item-lists";
    public const string ParkZones = "park-zones";
    public const string ParkItems = "park-items";
    public const string StandaloneAttractions = "standalone-attractions";
    public const string ParkItemImages = "park-item-images";
    public const string ParkItemVideos = "park-item-videos";
    public const string References = "references";
    public const string TechnicalPages = "technical-pages";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Static,
        Parks,
        ParkOpeningHours,
        History,
        HistoryArticles,
        ParkImages,
        ParkVideos,
        ParkItemLists,
        ParkZones,
        ParkItems,
        StandaloneAttractions,
        ParkItemImages,
        ParkItemVideos,
        References,
        TechnicalPages,
    };
}


/// <summary>
/// Contexte de génération partagé par les providers sitemap.
/// </summary>
public sealed class SitemapGenerationContext
{
    public IReadOnlyCollection<string> SupportedLanguages { get; init; } = Array.Empty<string>();
}

/// <summary>
/// URL publique candidate pour un sitemap.
/// </summary>
public sealed record SitemapUrlEntry(string RelativePath, DateTime? LastModifiedUtc = null, string? ChangeFrequency = null, decimal? Priority = null);

/// <summary>
/// Résultat de construction d'une section de sitemap.
/// </summary>
public sealed record SitemapSectionBuildResult(string Key, string FileName, string DisplayName, IReadOnlyCollection<SitemapUrlEntry> Urls);

/// <summary>
/// Statistiques d'une section de sitemap.
/// </summary>
public sealed record SitemapSectionStats(string Key, string FileName, string DisplayName, int UrlCount, DateTime? LastModifiedUtc);

/// <summary>
/// Type de déclenchement d'une génération sitemap.
/// </summary>
public enum SitemapGenerationTrigger
{
    Manual = 0,
    Automatic = 1,
    PublicFallback = 2,
}

/// <summary>
/// État d'une génération sitemap.
/// </summary>
public enum SitemapGenerationStatus
{
    Idle = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Skipped = 4,
}

/// <summary>
/// Réglages SEO administrables liés aux sitemaps et à IndexNow.
/// </summary>
public sealed class SeoSitemapSettings
{
    public bool IsIndexNowEnabled { get; init; }

    public bool SubmitToIndexNowAfterManualGeneration { get; init; }

    public bool SubmitToIndexNowAfterAutomaticGeneration { get; init; }

    public string IndexNowKey { get; init; } = string.Empty;

    public string IndexNowKeyLocation { get; init; } = string.Empty;

    public IReadOnlyCollection<string> IndexNowEndpoints { get; init; } = Array.Empty<string>();

    public DateTime UpdatedAtUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Contenu persistant du dernier sitemap généré.
/// </summary>
public sealed class SitemapSnapshot
{
    public string Id { get; init; } = "current";

    public DateTime GeneratedAtUtc { get; init; }

    public string PublicBaseUrl { get; init; } = string.Empty;

    public string IndexXml { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> SectionXmlByKey { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<SitemapSectionStats> Sections { get; init; } = Array.Empty<SitemapSectionStats>();

    public int TotalUrlCount { get; init; }
}

/// <summary>
/// Résultat d'une soumission IndexNow.
/// </summary>
public sealed class IndexNowSubmissionResult
{
    public bool WasRequested { get; init; }

    public bool IsEnabled { get; init; }

    public bool IsSuccess { get; init; }

    public int SubmittedUrlCount { get; init; }

    public IReadOnlyCollection<string> AcceptedEndpoints { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Résultat complet d'une génération sitemap.
/// </summary>
public sealed class SitemapGenerationResult
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public DateTime StartedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public long DurationMs { get; init; }

    public SitemapGenerationStatus Status { get; init; }

    public SitemapGenerationTrigger Trigger { get; init; }

    public int TotalUrlCount { get; init; }

    public IReadOnlyCollection<SitemapSectionStats> Sections { get; init; } = Array.Empty<SitemapSectionStats>();

    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

    public IndexNowSubmissionResult IndexNow { get; init; } = new IndexNowSubmissionResult();
}

/// <summary>
/// Trace persistée d'une génération sitemap.
/// </summary>
public sealed class SitemapGenerationHistoryEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public DateTime StartedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public long DurationMs { get; init; }

    public SitemapGenerationStatus Status { get; init; }

    public SitemapGenerationTrigger Trigger { get; init; }

    public string? TriggeredByUserId { get; init; }

    public string? TriggeredByUserEmail { get; init; }

    public int TotalUrlCount { get; init; }

    public IReadOnlyCollection<SitemapSectionStats> Sections { get; init; } = Array.Empty<SitemapSectionStats>();

    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

    public IndexNowSubmissionResult IndexNow { get; init; } = new IndexNowSubmissionResult();
}

/// <summary>
/// État runtime exposé au panneau admin.
/// </summary>
public sealed class SitemapRuntimeState
{
    public SitemapGenerationStatus Status { get; init; } = SitemapGenerationStatus.Idle;

    public string CurrentStep { get; init; } = "idle";

    public int ProgressPercentage { get; init; }

    public DateTime? StartedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public string? Message { get; init; }
}
