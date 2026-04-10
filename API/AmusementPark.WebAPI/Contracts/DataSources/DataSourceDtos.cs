using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.DataSources;

public sealed class DataSourceStatusDto
{
    public string SourceKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public DateTime? LastSuccessfulImportUtc { get; set; }

    public int TotalSessionsCount { get; set; }
}

public sealed class DataSourceSettingsDto
{
    public string SourceKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public Dictionary<string, string?> Options { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}

public sealed class UpdateDataSourceSettingsDto
{
    public bool IsEnabled { get; set; }

    public Dictionary<string, string?> Options { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}

public sealed class DataSourceSessionDto
{
    public string SessionId { get; set; } = string.Empty;

    public string SourceKey { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int ProgressPercentage { get; set; }

    public string CurrentStep { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DataSourceMetricsDto Metrics { get; set; } = new DataSourceMetricsDto();

    public IReadOnlyCollection<DataSourceLogDto> Logs { get; set; } = Array.Empty<DataSourceLogDto>();
}

public sealed class DataSourceMetricsDto
{
    public int ItemsFetchedPrimary { get; set; }

    public int ItemsFetchedSecondary { get; set; }

    public int ComparisonResults { get; set; }

    public int AppliedChanges { get; set; }

    public int DuplicateConflicts { get; set; }
}

public sealed class DataSourceLogDto
{
    public DateTime OccurredAtUtc { get; set; }

    public string Level { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

public sealed class DataSourceComparisonPageDto
{
    public IReadOnlyCollection<DataSourceComparisonItemDto> Items { get; set; } = Array.Empty<DataSourceComparisonItemDto>();

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int SessionUpdatedCount { get; set; }

    public int SessionMissingCount { get; set; }

    public int SessionDuplicateCount { get; set; }

    public int SessionAppliedCount { get; set; }
}

public sealed class DataSourceComparisonItemDto
{
    public string Id { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string ChangeType { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? LocalEntityId { get; set; }

    public string? ExternalEntityId { get; set; }

    public string MatchConfidence { get; set; } = string.Empty;

    public bool IsApplied { get; set; }

    public bool HasExternalDuplicates { get; set; }

    public bool RequiresManualResolution { get; set; }

    public string ResolutionStatus { get; set; } = string.Empty;

    public string? AppliedExternalVariantId { get; set; }

    public IReadOnlyCollection<DataSourceComparisonFieldChangeDto> Changes { get; set; } = Array.Empty<DataSourceComparisonFieldChangeDto>();

    public IReadOnlyCollection<DataSourceComparisonVariantDto> ExternalVariants { get; set; } = Array.Empty<DataSourceComparisonVariantDto>();
}

public sealed class DataSourceComparisonVariantDto
{
    public string ExternalVariantId { get; set; } = string.Empty;

    public string DisplayLabel { get; set; } = string.Empty;

    public string? CandidateLocalEntityId { get; set; }

    public string? SourceUrl { get; set; }

    public bool IsSuggested { get; set; }

    public IReadOnlyCollection<DataSourceComparisonFieldChangeDto> Changes { get; set; } = Array.Empty<DataSourceComparisonFieldChangeDto>();
}

public sealed class DataSourceComparisonFieldChangeDto
{
    public string Field { get; set; } = string.Empty;

    public string? LocalValue { get; set; }

    public string? ExternalValue { get; set; }

    public bool IsDifferent { get; set; }
}

public sealed class ApplyDataSourceComparisonRequestDto
{
    public string? SessionId { get; set; }

    public IReadOnlyCollection<string> ComparisonResultIds { get; set; } = Array.Empty<string>();

    public bool ApplyAll { get; set; }

    public string? EntityTypeFilter { get; set; }

    public string? ChangeTypeFilter { get; set; }

    public IReadOnlyCollection<DataSourceDuplicateResolutionDto> DuplicateResolutions { get; set; } = Array.Empty<DataSourceDuplicateResolutionDto>();
}

public sealed class DataSourceDuplicateResolutionDto
{
    [Required]
    public string ComparisonResultId { get; set; } = string.Empty;

    public string Strategy { get; set; } = "SelectVariant";

    public string? SelectedExternalVariantId { get; set; }

    public IReadOnlyCollection<DataSourceFieldResolutionDto> FieldResolutions { get; set; } = Array.Empty<DataSourceFieldResolutionDto>();
}

public sealed class DataSourceFieldResolutionDto
{
    [Required]
    public string Field { get; set; } = string.Empty;

    public string SourceType { get; set; } = "Variant";

    public string? ExternalVariantId { get; set; }
}

public sealed class DataSourceApplyResultDto
{
    public int AppliedCount { get; set; }
}
