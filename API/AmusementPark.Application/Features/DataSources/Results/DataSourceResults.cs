namespace AmusementPark.Application.Features.DataSources.Results;

/// <summary>
/// Statut synthétique d'une source externe.
/// </summary>
public sealed class DataSourceStatusResult
{
    public string SourceKey { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsEnabled { get; init; }

    public DateTime? LastSuccessfulImportUtc { get; init; }

    public int TotalSessionsCount { get; init; }
}

/// <summary>
/// Paramètres modifiables d'une source externe.
/// </summary>
public sealed class DataSourceSettingsResult
{
    public string SourceKey { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsEnabled { get; init; }

    public IReadOnlyDictionary<string, string?> Options { get; init; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Session d'import/synchronisation.
/// </summary>
public sealed class DataSourceSessionResult
{
    public string SessionId { get; init; } = string.Empty;

    public string SourceKey { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int ProgressPercentage { get; init; }

    public string CurrentStep { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public DateTime StartedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public DataSourceMetricsResult Metrics { get; init; } = new DataSourceMetricsResult();

    public IReadOnlyCollection<DataSourceLogResult> Logs { get; init; } = Array.Empty<DataSourceLogResult>();
}

/// <summary>
/// Compteurs fonctionnels d'une session.
/// </summary>
public sealed class DataSourceMetricsResult
{
    public int ItemsFetchedPrimary { get; init; }

    public int ItemsFetchedSecondary { get; init; }

    public int ComparisonResults { get; init; }

    public int AppliedChanges { get; init; }

    public int DuplicateConflicts { get; init; }
}

/// <summary>
/// Entrée de log métier d'une session.
/// </summary>
public sealed class DataSourceLogResult
{
    public DateTime OccurredAtUtc { get; init; }

    public string Level { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Page de résultats de comparaison.
/// </summary>
public sealed class DataSourceComparisonPageResult
{
    public IReadOnlyCollection<DataSourceComparisonItemResult> Items { get; init; } = Array.Empty<DataSourceComparisonItemResult>();

    public int TotalCount { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int SessionUpdatedCount { get; init; }

    public int SessionMissingCount { get; init; }

    public int SessionDuplicateCount { get; init; }

    public int SessionAppliedCount { get; init; }
}

/// <summary>
/// Résultat unitaire de comparaison.
/// </summary>
public sealed class DataSourceComparisonItemResult
{
    public string Id { get; init; } = string.Empty;

    public string EntityType { get; init; } = string.Empty;

    public string ChangeType { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string? LocalEntityId { get; init; }

    public string? ExternalEntityId { get; init; }

    public string MatchConfidence { get; init; } = string.Empty;

    public bool IsApplied { get; init; }

    public bool HasExternalDuplicates { get; init; }

    public bool RequiresManualResolution { get; init; }

    public string ResolutionStatus { get; init; } = string.Empty;

    public string? AppliedExternalVariantId { get; init; }

    public IReadOnlyCollection<DataSourceComparisonFieldChangeResult> Changes { get; init; } = Array.Empty<DataSourceComparisonFieldChangeResult>();

    public IReadOnlyCollection<DataSourceComparisonVariantResult> ExternalVariants { get; init; } = Array.Empty<DataSourceComparisonVariantResult>();
}

/// <summary>
/// Variation externe candidate.
/// </summary>
public sealed class DataSourceComparisonVariantResult
{
    public string ExternalVariantId { get; init; } = string.Empty;

    public string DisplayLabel { get; init; } = string.Empty;

    public string? CandidateLocalEntityId { get; init; }

    public string? SourceUrl { get; init; }

    public bool IsSuggested { get; init; }

    public IReadOnlyCollection<DataSourceComparisonFieldChangeResult> Changes { get; init; } = Array.Empty<DataSourceComparisonFieldChangeResult>();
}

/// <summary>
/// Différence de champ élémentaire.
/// </summary>
public sealed class DataSourceComparisonFieldChangeResult
{
    public string Field { get; init; } = string.Empty;

    public string? LocalValue { get; init; }

    public string? ExternalValue { get; init; }

    public bool IsDifferent { get; init; }
}

/// <summary>
/// Résultat d'application d'une comparaison.
/// </summary>
public sealed class DataSourceApplyResult
{
    public int AppliedCount { get; init; }
}
