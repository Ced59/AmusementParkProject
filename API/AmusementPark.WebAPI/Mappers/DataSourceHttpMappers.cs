using System;
using System.Collections.Generic;
using System.Linq;
using AmusementPark.Application.Features.DataSources.Contracts;
using AmusementPark.Application.Features.DataSources.Results;
using AmusementPark.WebAPI.Contracts.DataSources;

namespace AmusementPark.WebAPI.Mappers;

internal static class DataSourceHttpMappers
{
    public static DataSourceStatusDto ToHttp(this DataSourceStatusResult result)
    {
        return new DataSourceStatusDto
        {
            SourceKey = result.SourceKey,
            DisplayName = result.DisplayName,
            IsEnabled = result.IsEnabled,
            LastSuccessfulImportUtc = result.LastSuccessfulImportUtc,
            TotalSessionsCount = result.TotalSessionsCount,
        };
    }

    public static DataSourceSettingsDto ToHttp(this DataSourceSettingsResult result)
    {
        return new DataSourceSettingsDto
        {
            SourceKey = result.SourceKey,
            DisplayName = result.DisplayName,
            IsEnabled = result.IsEnabled,
            Options = new Dictionary<string, string?>(result.Options, StringComparer.OrdinalIgnoreCase),
        };
    }

    public static DataSourceSettingsResult ToApplication(this UpdateDataSourceSettingsDto dto, string sourceKey)
    {
        return new DataSourceSettingsResult
        {
            SourceKey = sourceKey,
            DisplayName = string.Empty,
            IsEnabled = dto.IsEnabled,
            Options = new Dictionary<string, string?>(dto.Options, StringComparer.OrdinalIgnoreCase),
        };
    }

    public static DataSourceImportDescriptor ToApplication(this StartDataSourceImportRequestDto dto, string workingDirectoryPath)
    {
        return new DataSourceImportDescriptor
        {
            ImportKind = string.IsNullOrWhiteSpace(dto.ImportKind) ? "sitemap" : dto.ImportKind.Trim(),
            WorkingDirectoryPath = workingDirectoryPath,
            Urls = dto.Urls
                .Where(static item => !string.IsNullOrWhiteSpace(item))
                .Select(static item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Options = new Dictionary<string, string?>(dto.Options, StringComparer.OrdinalIgnoreCase),
            ResumeSessionId = string.IsNullOrWhiteSpace(dto.ResumeSessionId) ? null : dto.ResumeSessionId.Trim(),
        };
    }

    public static DataSourceSessionDto ToHttp(this DataSourceSessionResult result)
    {
        return new DataSourceSessionDto
        {
            SessionId = result.SessionId,
            SourceKey = result.SourceKey,
            Status = result.Status,
            ImportKind = result.ImportKind,
            ProgressPercentage = result.ProgressPercentage,
            CurrentStep = result.CurrentStep,
            LastCompletedStep = result.LastCompletedStep,
            Message = result.Message,
            CanResume = result.CanResume,
            AvailableSteps = result.AvailableSteps,
            StartedAtUtc = result.StartedAtUtc,
            CompletedAtUtc = result.CompletedAtUtc,
            Metrics = new DataSourceMetricsDto
            {
                ItemsFetchedPrimary = result.Metrics.ItemsFetchedPrimary,
                ItemsFetchedSecondary = result.Metrics.ItemsFetchedSecondary,
                ComparisonResults = result.Metrics.ComparisonResults,
                AppliedChanges = result.Metrics.AppliedChanges,
                DuplicateConflicts = result.Metrics.DuplicateConflicts,
                DiscoveredItems = result.Metrics.DiscoveredItems,
                ProcessedItems = result.Metrics.ProcessedItems,
                FailedItems = result.Metrics.FailedItems,
                SkippedItems = result.Metrics.SkippedItems,
            },
            Logs = result.Logs.Select(log => new DataSourceLogDto
            {
                OccurredAtUtc = log.OccurredAtUtc,
                Level = log.Level,
                Message = log.Message,
            }).ToList(),
        };
    }

    public static DataSourceComparisonPageDto ToHttp(this DataSourceComparisonPageResult result)
    {
        return new DataSourceComparisonPageDto
        {
            Items = result.Items.Select(ToHttp).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            SessionUpdatedCount = result.SessionUpdatedCount,
            SessionMissingCount = result.SessionMissingCount,
            SessionDuplicateCount = result.SessionDuplicateCount,
            SessionAppliedCount = result.SessionAppliedCount,
        };
    }

    public static DataSourceComparisonItemDto ToHttp(this DataSourceComparisonItemResult result)
    {
        return new DataSourceComparisonItemDto
        {
            Id = result.Id,
            EntityType = result.EntityType,
            ChangeType = result.ChangeType,
            DisplayName = result.DisplayName,
            LocalEntityId = result.LocalEntityId,
            ExternalEntityId = result.ExternalEntityId,
            MatchConfidence = result.MatchConfidence,
            IsApplied = result.IsApplied,
            HasExternalDuplicates = result.HasExternalDuplicates,
            RequiresManualResolution = result.RequiresManualResolution,
            ResolutionStatus = result.ResolutionStatus,
            AppliedExternalVariantId = result.AppliedExternalVariantId,
            Changes = result.Changes.Select(change => new DataSourceComparisonFieldChangeDto
            {
                Field = change.Field,
                LocalValue = change.LocalValue,
                ExternalValue = change.ExternalValue,
                IsDifferent = change.IsDifferent,
            }).ToList(),
            ExternalVariants = result.ExternalVariants.Select(variant => new DataSourceComparisonVariantDto
            {
                ExternalVariantId = variant.ExternalVariantId,
                DisplayLabel = variant.DisplayLabel,
                CandidateLocalEntityId = variant.CandidateLocalEntityId,
                SourceUrl = variant.SourceUrl,
                IsSuggested = variant.IsSuggested,
                Changes = variant.Changes.Select(change => new DataSourceComparisonFieldChangeDto
                {
                    Field = change.Field,
                    LocalValue = change.LocalValue,
                    ExternalValue = change.ExternalValue,
                    IsDifferent = change.IsDifferent,
                }).ToList(),
            }).ToList(),
        };
    }

    public static DataSourceApplyRequest ToApplication(this ApplyDataSourceComparisonRequestDto dto)
    {
        return new DataSourceApplyRequest
        {
            SessionId = dto.SessionId,
            ComparisonResultIds = dto.ComparisonResultIds,
            ApplyAll = dto.ApplyAll,
            EntityTypeFilter = dto.EntityTypeFilter,
            ChangeTypeFilter = dto.ChangeTypeFilter,
            DuplicateResolutions = dto.DuplicateResolutions.Select(item => new DataSourceDuplicateResolution
            {
                ComparisonResultId = item.ComparisonResultId,
                Strategy = item.Strategy,
                SelectedExternalVariantId = item.SelectedExternalVariantId,
                FieldResolutions = item.FieldResolutions.Select(field => new DataSourceFieldResolution
                {
                    Field = field.Field,
                    SourceType = field.SourceType,
                    ExternalVariantId = field.ExternalVariantId,
                }).ToList(),
            }).ToList(),
        };
    }
}
