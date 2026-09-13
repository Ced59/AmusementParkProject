namespace AmusementPark.Application.Features.TechnicalStats.Contracts;

public sealed class TechnicalStatsCacheSummary
{
    public long PageResponses { get; set; }

    public long CacheablePageResponses { get; set; }

    public long CacheHitResponses { get; set; }

    public double HitRatePercent { get; set; }

    public long RobotPageResponses { get; set; }

    public long RobotCacheHitResponses { get; set; }

    public double RobotHitRatePercent { get; set; }

    public IReadOnlyCollection<TechnicalStatsCount> Statuses { get; set; } = Array.Empty<TechnicalStatsCount>();

    public IReadOnlyCollection<TechnicalStatsRobotFamily> RobotFamilies { get; set; } = Array.Empty<TechnicalStatsRobotFamily>();
}
