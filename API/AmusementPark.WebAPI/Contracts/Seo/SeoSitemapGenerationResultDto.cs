namespace AmusementPark.WebAPI.Contracts.Seo;

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
