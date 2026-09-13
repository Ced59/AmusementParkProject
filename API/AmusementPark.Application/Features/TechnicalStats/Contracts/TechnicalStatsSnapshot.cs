namespace AmusementPark.Application.Features.TechnicalStats.Contracts;

public sealed class TechnicalStatsSnapshot
{
    public bool IsAvailable { get; set; } = true;

    public string? UnavailableReason { get; set; }

    public DateTime GeneratedAtUtc { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public long UptimeSeconds { get; set; }

    public string BuildVersion { get; set; } = string.Empty;

    public IReadOnlyCollection<TechnicalStatsDailySnapshot> Daily { get; set; } = Array.Empty<TechnicalStatsDailySnapshot>();

    public TechnicalStatsCacheSummary Cache { get; set; } = new TechnicalStatsCacheSummary();

    public TechnicalStatsStorageSummary Storage { get; set; } = new TechnicalStatsStorageSummary();

    public TechnicalStatsSeoSummary Seo { get; set; } = new TechnicalStatsSeoSummary();

    public TechnicalStatsRenderingSummary Rendering { get; set; } = new TechnicalStatsRenderingSummary();

    public TechnicalStatsRefreshSummary Refresh { get; set; } = new TechnicalStatsRefreshSummary();

    public TechnicalStatsInvalidationSummary Invalidation { get; set; } = new TechnicalStatsInvalidationSummary();

    public TechnicalStatsRuntimeConfig Config { get; set; } = new TechnicalStatsRuntimeConfig();
}
