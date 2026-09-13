namespace AmusementPark.WebAPI.Contracts.TechnicalStats;

public sealed class TechnicalStatsSnapshotDto
{
    public bool IsAvailable { get; set; } = true;

    public string? UnavailableReason { get; set; }

    public DateTime GeneratedAtUtc { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public long UptimeSeconds { get; set; }

    public string BuildVersion { get; set; } = string.Empty;

    public IReadOnlyCollection<TechnicalStatsDailySnapshotDto> Daily { get; set; } = Array.Empty<TechnicalStatsDailySnapshotDto>();

    public TechnicalStatsCacheSummaryDto Cache { get; set; } = new TechnicalStatsCacheSummaryDto();

    public TechnicalStatsStorageSummaryDto Storage { get; set; } = new TechnicalStatsStorageSummaryDto();

    public TechnicalStatsSeoSummaryDto Seo { get; set; } = new TechnicalStatsSeoSummaryDto();

    public TechnicalStatsRenderingSummaryDto Rendering { get; set; } = new TechnicalStatsRenderingSummaryDto();

    public TechnicalStatsRefreshSummaryDto Refresh { get; set; } = new TechnicalStatsRefreshSummaryDto();

    public TechnicalStatsInvalidationSummaryDto Invalidation { get; set; } = new TechnicalStatsInvalidationSummaryDto();

    public TechnicalStatsRuntimeConfigDto Config { get; set; } = new TechnicalStatsRuntimeConfigDto();
}
