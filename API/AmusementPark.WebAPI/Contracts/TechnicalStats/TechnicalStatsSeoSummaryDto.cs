namespace AmusementPark.WebAPI.Contracts.TechnicalStats;

public sealed class TechnicalStatsSeoSummaryDto
{
    public bool RobotNoJsHtmlEnabled { get; set; }

    public long HtmlResponses { get; set; }

    public long SeoReadyHtmlResponses { get; set; }

    public long SeoNotReadyHtmlResponses { get; set; }

    public double SeoReadyRatePercent { get; set; }

    public long RobotHtmlResponses { get; set; }

    public long RobotSeoReadyHtmlResponses { get; set; }

    public long RobotSeoNotReadyHtmlResponses { get; set; }

    public double RobotSeoReadyRatePercent { get; set; }

    public long RobotNoJsHtmlResponses { get; set; }

    public long RobotHtmlBlockedNotSeoReady { get; set; }

    public long RobotHtmlNotAllowed { get; set; }

    public long RobotSsrUnavailableResponses { get; set; }

    public long RobotCacheOnlyMissResponses { get; set; }

    public long RobotPageResponses { get; set; }

    public long RobotCacheHitResponses { get; set; }

    public double RobotHitRatePercent { get; set; }

    public long SeoDocumentRequests { get; set; }

    public long SeoDocumentHits { get; set; }

    public long SeoDocumentMisses { get; set; }

    public double SeoDocumentHitRatePercent { get; set; }

    public long QueueFullRejections { get; set; }
}
