using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Results;
using AmusementPark.WebAPI.Contracts.Seo;

namespace AmusementPark.WebAPI.Mappers;

public static class SeoSitemapHttpMappers
{
    public static SeoSitemapOverviewDto ToHttp(this SeoSitemapOverviewResult result, string publicBaseUrl)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new SeoSitemapOverviewDto
        {
            Runtime = result.Runtime.ToHttp(),
            LastGeneratedAtUtc = result.Snapshot?.GeneratedAtUtc,
            PublicBaseUrl = publicBaseUrl,
            TotalUrlCount = result.TotalUrlCount,
            Sections = result.Sections.Select(section => section.ToHttp(publicBaseUrl)).ToList(),
            Settings = result.Settings.ToHttp(),
            SitemapIndexUrl = result.SitemapIndexUrl,
            RobotsUrl = result.RobotsUrl,
            IndexNowKeyFileUrl = result.IndexNowKeyFileUrl,
            PublicSitemapUrls = result.PublicSitemapUrls.ToList(),
        };
    }

    public static SeoSitemapSettingsDto ToHttp(this SeoSitemapSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new SeoSitemapSettingsDto
        {
            IsIndexNowEnabled = settings.IsIndexNowEnabled,
            SubmitToIndexNowAfterManualGeneration = settings.SubmitToIndexNowAfterManualGeneration,
            SubmitToIndexNowAfterAutomaticGeneration = settings.SubmitToIndexNowAfterAutomaticGeneration,
            IndexNowKey = settings.IndexNowKey,
            IndexNowKeyLocation = settings.IndexNowKeyLocation,
            IndexNowEndpoints = settings.IndexNowEndpoints.ToList(),
            UpdatedAtUtc = settings.UpdatedAtUtc,
        };
    }

    public static SeoSitemapGenerationResultDto ToHttp(this SitemapGenerationResult result, string publicBaseUrl)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new SeoSitemapGenerationResultDto
        {
            Id = result.Id,
            StartedAtUtc = result.StartedAtUtc,
            CompletedAtUtc = result.CompletedAtUtc,
            DurationMs = result.DurationMs,
            Status = result.Status.ToString(),
            Trigger = result.Trigger.ToString(),
            TotalUrlCount = result.TotalUrlCount,
            Sections = result.Sections.Select(section => section.ToHttp(publicBaseUrl)).ToList(),
            Errors = result.Errors.ToList(),
            IndexNow = result.IndexNow.ToHttp(),
        };
    }

    public static SeoSitemapGenerationHistoryDto ToHttp(this SitemapGenerationHistoryEntry result, string publicBaseUrl)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new SeoSitemapGenerationHistoryDto
        {
            Id = result.Id,
            StartedAtUtc = result.StartedAtUtc,
            CompletedAtUtc = result.CompletedAtUtc,
            DurationMs = result.DurationMs,
            Status = result.Status.ToString(),
            Trigger = result.Trigger.ToString(),
            TriggeredByUserId = result.TriggeredByUserId,
            TriggeredByUserEmail = result.TriggeredByUserEmail,
            TotalUrlCount = result.TotalUrlCount,
            Sections = result.Sections.Select(section => section.ToHttp(publicBaseUrl)).ToList(),
            Errors = result.Errors.ToList(),
            IndexNow = result.IndexNow.ToHttp(),
        };
    }

    private static SeoSitemapRuntimeDto ToHttp(this SitemapRuntimeState runtime)
    {
        return new SeoSitemapRuntimeDto
        {
            Status = runtime.Status.ToString(),
            CurrentStep = runtime.CurrentStep,
            ProgressPercentage = runtime.ProgressPercentage,
            StartedAtUtc = runtime.StartedAtUtc,
            UpdatedAtUtc = runtime.UpdatedAtUtc,
            Message = runtime.Message,
        };
    }

    private static SeoSitemapSectionStatsDto ToHttp(this SitemapSectionStats stats, string publicBaseUrl)
    {
        string normalizedBaseUrl = publicBaseUrl.TrimEnd('/');
        return new SeoSitemapSectionStatsDto
        {
            Key = stats.Key,
            FileName = stats.FileName,
            DisplayName = stats.DisplayName,
            UrlCount = stats.UrlCount,
            LastModifiedUtc = stats.LastModifiedUtc,
            PublicUrl = $"{normalizedBaseUrl}/{stats.FileName}",
        };
    }

    private static SeoIndexNowSubmissionDto ToHttp(this IndexNowSubmissionResult result)
    {
        return new SeoIndexNowSubmissionDto
        {
            WasRequested = result.WasRequested,
            IsEnabled = result.IsEnabled,
            IsSuccess = result.IsSuccess,
            SubmittedUrlCount = result.SubmittedUrlCount,
            AcceptedEndpoints = result.AcceptedEndpoints.ToList(),
            Errors = result.Errors.ToList(),
        };
    }
}
