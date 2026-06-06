namespace AmusementPark.WebAPI.Contracts.Seo;

public sealed class GenerateSeoSitemapRequestDto
{
    public bool SubmitToIndexNow { get; init; } = true;
}

public sealed class UpdateSeoSitemapSettingsRequestDto
{
    public bool IsIndexNowEnabled { get; init; }

    public bool SubmitToIndexNowAfterManualGeneration { get; init; }

    public bool SubmitToIndexNowAfterAutomaticGeneration { get; init; }

    public string? IndexNowKey { get; init; }

    public string? IndexNowKeyLocation { get; init; }

    public IReadOnlyCollection<string> IndexNowEndpoints { get; init; } = Array.Empty<string>();
}

public sealed class SeoSitemapSettingsDto
{
    public bool IsIndexNowEnabled { get; init; }

    public bool SubmitToIndexNowAfterManualGeneration { get; init; }

    public bool SubmitToIndexNowAfterAutomaticGeneration { get; init; }

    public string IndexNowKey { get; init; } = string.Empty;

    public string IndexNowKeyLocation { get; init; } = string.Empty;

    public IReadOnlyCollection<string> IndexNowEndpoints { get; init; } = Array.Empty<string>();

    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class SeoSitemapOverviewDto
{
    public SeoSitemapRuntimeDto Runtime { get; init; } = new SeoSitemapRuntimeDto();

    public DateTime? LastGeneratedAtUtc { get; init; }

    public string PublicBaseUrl { get; init; } = string.Empty;

    public int TotalUrlCount { get; init; }

    public IReadOnlyCollection<SeoSitemapSectionStatsDto> Sections { get; init; } = Array.Empty<SeoSitemapSectionStatsDto>();

    public SeoSitemapSettingsDto Settings { get; init; } = new SeoSitemapSettingsDto();

    public string SitemapIndexUrl { get; init; } = string.Empty;

    public string RobotsUrl { get; init; } = string.Empty;

    public string IndexNowKeyFileUrl { get; init; } = string.Empty;

    public IReadOnlyCollection<string> PublicSitemapUrls { get; init; } = Array.Empty<string>();
}

public sealed class SeoSitemapRuntimeDto
{
    public string Status { get; init; } = string.Empty;

    public string CurrentStep { get; init; } = string.Empty;

    public int ProgressPercentage { get; init; }

    public DateTime? StartedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public string? Message { get; init; }
}

public sealed class SeoSitemapSectionStatsDto
{
    public string Key { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public int UrlCount { get; init; }

    public DateTime? LastModifiedUtc { get; init; }

    public string PublicUrl { get; init; } = string.Empty;
}

public class SeoSitemapGenerationResultDto
{
    public string Id { get; init; } = string.Empty;

    public DateTime StartedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public long DurationMs { get; init; }

    public string Status { get; init; } = string.Empty;

    public string Trigger { get; init; } = string.Empty;

    public int TotalUrlCount { get; init; }

    public IReadOnlyCollection<SeoSitemapSectionStatsDto> Sections { get; init; } = Array.Empty<SeoSitemapSectionStatsDto>();

    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

    public SeoIndexNowSubmissionDto IndexNow { get; init; } = new SeoIndexNowSubmissionDto();
}

public sealed class SeoSitemapGenerationHistoryDto : SeoSitemapGenerationResultDto
{
    public string? TriggeredByUserId { get; init; }

    public string? TriggeredByUserEmail { get; init; }
}

public sealed class SeoIndexNowSubmissionDto
{
    public bool WasRequested { get; init; }

    public bool IsEnabled { get; init; }

    public bool IsSuccess { get; init; }

    public int SubmittedUrlCount { get; init; }

    public IReadOnlyCollection<string> AcceptedEndpoints { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();
}
