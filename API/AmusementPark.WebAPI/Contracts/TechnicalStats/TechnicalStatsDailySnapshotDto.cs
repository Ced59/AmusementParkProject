namespace AmusementPark.WebAPI.Contracts.TechnicalStats;

public sealed class TechnicalStatsDailySnapshotDto
{
    public string Date { get; set; } = string.Empty;
    public long PageResponses { get; set; }
    public long CacheHitResponses { get; set; }
    public double HitRatePercent { get; set; }
    public long RobotPageResponses { get; set; }
    public long RobotCacheHitResponses { get; set; }
    public double RobotHitRatePercent { get; set; }
    public long TotalRenders { get; set; }
    public long AverageRenderMilliseconds { get; set; }
    public double SeoReadyRatePercent { get; set; }
    public double RobotSeoReadyRatePercent { get; set; }
    public long RobotCacheOnlyMissResponses { get; set; }
    public long QueueFullRejections { get; set; }
    public IReadOnlyCollection<TechnicalStatsRobotFamilyDto> RobotFamilies { get; set; } = Array.Empty<TechnicalStatsRobotFamilyDto>();
}
