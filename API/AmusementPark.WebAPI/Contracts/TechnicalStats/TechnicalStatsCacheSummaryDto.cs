namespace AmusementPark.WebAPI.Contracts.TechnicalStats;

public sealed class TechnicalStatsCacheSummaryDto
{
    public long PageResponses { get; set; }

    public long CacheablePageResponses { get; set; }

    public long CacheHitResponses { get; set; }

    public double HitRatePercent { get; set; }

    public long RobotPageResponses { get; set; }

    public long RobotCacheHitResponses { get; set; }

    public double RobotHitRatePercent { get; set; }

    public IReadOnlyCollection<TechnicalStatsCountDto> Statuses { get; set; } = Array.Empty<TechnicalStatsCountDto>();

    public IReadOnlyCollection<TechnicalStatsRobotFamilyDto> RobotFamilies { get; set; } = Array.Empty<TechnicalStatsRobotFamilyDto>();
}
