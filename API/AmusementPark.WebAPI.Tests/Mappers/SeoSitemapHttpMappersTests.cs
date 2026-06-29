using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Results;
using AmusementPark.WebAPI.Contracts.Seo;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class SeoSitemapHttpMappersTests
{
    [Fact]
    public void ToHttp_WhenSettingsProvided_ShouldCopyAllValues()
    {
        DateTime updatedAtUtc = new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc);
        SeoSitemapSettings settings = new SeoSitemapSettings
        {
            IsIndexNowEnabled = true,
            SubmitToIndexNowAfterManualGeneration = true,
            SubmitToIndexNowAfterAutomaticGeneration = false,
            IndexNowKey = "key",
            IndexNowKeyLocation = "https://example.com/key.txt",
            IndexNowEndpoints = new[] { "https://api.indexnow.org/indexnow" },
            UpdatedAtUtc = updatedAtUtc,
        };

        SeoSitemapSettingsDto dto = settings.ToHttp();

        Assert.True(dto.IsIndexNowEnabled);
        Assert.True(dto.SubmitToIndexNowAfterManualGeneration);
        Assert.False(dto.SubmitToIndexNowAfterAutomaticGeneration);
        Assert.Equal("key", dto.IndexNowKey);
        Assert.Equal("https://example.com/key.txt", dto.IndexNowKeyLocation);
        Assert.Equal(new[] { "https://api.indexnow.org/indexnow" }, dto.IndexNowEndpoints);
        Assert.Equal(updatedAtUtc, dto.UpdatedAtUtc);
    }

    [Fact]
    public void ToHttp_WhenGenerationResultProvided_ShouldMapStatusTriggerSectionsErrorsAndIndexNow()
    {
        SitemapGenerationResult result = new SitemapGenerationResult
        {
            Id = "gen-1",
            StartedAtUtc = new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc),
            CompletedAtUtc = new DateTime(2026, 6, 6, 10, 0, 1, DateTimeKind.Utc),
            DurationMs = 1000,
            Status = SitemapGenerationStatus.Succeeded,
            Trigger = SitemapGenerationTrigger.Manual,
            TotalUrlCount = 12,
            Sections = new[] { new SitemapSectionStats("static", "static.xml", "Static", 4, null) },
            Errors = new[] { "warning" },
            IndexNow = new IndexNowSubmissionResult
            {
                WasRequested = true,
                IsEnabled = true,
                IsSuccess = true,
                SubmittedUrlCount = 12,
                AcceptedEndpoints = new[] { "endpoint" },
            },
        };

        SeoSitemapGenerationResultDto dto = result.ToHttp("https://example.com/");

        Assert.Equal("gen-1", dto.Id);
        Assert.Equal("Succeeded", dto.Status);
        Assert.Equal("Manual", dto.Trigger);
        Assert.Equal(12, dto.TotalUrlCount);
        SeoSitemapSectionStatsDto section = Assert.Single(dto.Sections);
        Assert.Equal("https://example.com/static.xml", section.PublicUrl);
        Assert.Equal(new[] { "warning" }, dto.Errors);
        Assert.True(dto.IndexNow.WasRequested);
        Assert.True(dto.IndexNow.IsSuccess);
        Assert.Equal(12, dto.IndexNow.SubmittedUrlCount);
    }

    [Fact]
    public void ToHttp_WhenOverviewProvided_ShouldMapRuntimeSnapshotSectionsAndSettings()
    {
        SeoSitemapOverviewResult result = new SeoSitemapOverviewResult
        {
            Runtime = new SitemapRuntimeState
            {
                Status = SitemapGenerationStatus.Running,
                CurrentStep = "writing",
                ProgressPercentage = 42,
            },
            Snapshot = new SitemapSnapshot { GeneratedAtUtc = new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc) },
            TotalUrlCount = 4,
            Sections = new[] { new SitemapSectionStats("static", "static.xml", "Static", 4, null) },
            Settings = new SeoSitemapSettings { IsIndexNowEnabled = true },
            SitemapIndexUrl = "https://example.com/sitemap.xml",
            RobotsUrl = "https://example.com/robots.txt",
            IndexNowKeyFileUrl = "https://example.com/key.txt",
            PublicSitemapUrls = new[] { "https://example.com/static.xml" },
        };

        SeoSitemapOverviewDto dto = result.ToHttp("https://example.com/");

        Assert.Equal("Running", dto.Runtime.Status);
        Assert.Equal("writing", dto.Runtime.CurrentStep);
        Assert.Equal(42, dto.Runtime.ProgressPercentage);
        Assert.Equal(result.Snapshot.GeneratedAtUtc, dto.LastGeneratedAtUtc);
        Assert.True(dto.Settings.IsIndexNowEnabled);
        Assert.Equal(new[] { "https://example.com/static.xml" }, dto.PublicSitemapUrls);
    }
}
