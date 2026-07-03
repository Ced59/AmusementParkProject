using AmusementPark.Application.Features.TechnicalStats.Contracts;
using AmusementPark.WebAPI.Contracts.TechnicalStats;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class TechnicalStatsHttpMappersTests
{
    [Fact]
    public void ToHttp_WhenSnapshotContainsSeoStats_ShouldMapSeoAndRobotFamilyDetails()
    {
        TechnicalStatsSnapshot snapshot = new TechnicalStatsSnapshot
        {
            BuildVersion = "3.2.7",
            Seo = new TechnicalStatsSeoSummary
            {
                RobotNoJsHtmlEnabled = true,
                HtmlResponses = 100,
                SeoReadyHtmlResponses = 95,
                SeoNotReadyHtmlResponses = 5,
                SeoReadyRatePercent = 95,
                RobotHtmlResponses = 20,
                RobotSeoReadyHtmlResponses = 18,
                RobotSeoNotReadyHtmlResponses = 2,
                RobotSeoReadyRatePercent = 90,
                RobotNoJsHtmlResponses = 18,
                RobotHtmlBlockedNotSeoReady = 2,
                RobotHtmlNotAllowed = 1,
                RobotSsrUnavailableResponses = 3,
                RobotPageResponses = 22,
                RobotCacheHitResponses = 17,
                RobotHitRatePercent = 77.3,
                SeoDocumentRequests = 12,
                SeoDocumentHits = 9,
                SeoDocumentMisses = 3,
                SeoDocumentHitRatePercent = 75,
                QueueFullRejections = 4
            },
            Cache = new TechnicalStatsCacheSummary
            {
                RobotFamilies = new[]
                {
                    new TechnicalStatsRobotFamily
                    {
                        Key = "Bingbot",
                        Category = "bing",
                        Count = 10,
                        CacheHits = 8,
                        HitRatePercent = 80,
                        SeoReadyResponses = 9,
                        SeoNotReadyResponses = 1,
                        SeoReadyRatePercent = 90,
                        NoJsResponses = 9,
                        BlockedNotSeoReadyResponses = 1,
                        HtmlNotAllowedResponses = 0,
                        SsrUnavailableResponses = 2
                    }
                }
            }
        };

        TechnicalStatsSnapshotDto dto = snapshot.ToHttp();

        Assert.Equal("3.2.7", dto.BuildVersion);
        Assert.True(dto.Seo.RobotNoJsHtmlEnabled);
        Assert.Equal(100, dto.Seo.HtmlResponses);
        Assert.Equal(95, dto.Seo.SeoReadyHtmlResponses);
        Assert.Equal(5, dto.Seo.SeoNotReadyHtmlResponses);
        Assert.Equal(95, dto.Seo.SeoReadyRatePercent);
        Assert.Equal(20, dto.Seo.RobotHtmlResponses);
        Assert.Equal(18, dto.Seo.RobotSeoReadyHtmlResponses);
        Assert.Equal(2, dto.Seo.RobotSeoNotReadyHtmlResponses);
        Assert.Equal(90, dto.Seo.RobotSeoReadyRatePercent);
        Assert.Equal(18, dto.Seo.RobotNoJsHtmlResponses);
        Assert.Equal(2, dto.Seo.RobotHtmlBlockedNotSeoReady);
        Assert.Equal(1, dto.Seo.RobotHtmlNotAllowed);
        Assert.Equal(3, dto.Seo.RobotSsrUnavailableResponses);
        Assert.Equal(22, dto.Seo.RobotPageResponses);
        Assert.Equal(17, dto.Seo.RobotCacheHitResponses);
        Assert.Equal(77.3, dto.Seo.RobotHitRatePercent);
        Assert.Equal(12, dto.Seo.SeoDocumentRequests);
        Assert.Equal(9, dto.Seo.SeoDocumentHits);
        Assert.Equal(3, dto.Seo.SeoDocumentMisses);
        Assert.Equal(75, dto.Seo.SeoDocumentHitRatePercent);
        Assert.Equal(4, dto.Seo.QueueFullRejections);

        TechnicalStatsRobotFamilyDto family = Assert.Single(dto.Cache.RobotFamilies);
        Assert.Equal("Bingbot", family.Key);
        Assert.Equal("bing", family.Category);
        Assert.Equal(10, family.Count);
        Assert.Equal(8, family.CacheHits);
        Assert.Equal(80, family.HitRatePercent);
        Assert.Equal(9, family.SeoReadyResponses);
        Assert.Equal(1, family.SeoNotReadyResponses);
        Assert.Equal(90, family.SeoReadyRatePercent);
        Assert.Equal(9, family.NoJsResponses);
        Assert.Equal(1, family.BlockedNotSeoReadyResponses);
        Assert.Equal(0, family.HtmlNotAllowedResponses);
        Assert.Equal(2, family.SsrUnavailableResponses);
    }
}
