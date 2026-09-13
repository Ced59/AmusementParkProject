namespace AmusementPark.Application.Features.TechnicalStats.Contracts;

public sealed class TechnicalStatsRobotFamily
{
    public string Key { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public long Count { get; set; }

    public long CacheHits { get; set; }

    public double HitRatePercent { get; set; }

    public long SeoReadyResponses { get; set; }

    public long SeoNotReadyResponses { get; set; }

    public double SeoReadyRatePercent { get; set; }

    public long NoJsResponses { get; set; }

    public long BlockedNotSeoReadyResponses { get; set; }

    public long HtmlNotAllowedResponses { get; set; }

    public long SsrUnavailableResponses { get; set; }
}
