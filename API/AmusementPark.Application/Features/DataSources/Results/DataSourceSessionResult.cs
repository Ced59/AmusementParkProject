namespace AmusementPark.Application.Features.DataSources.Results;

/// <summary>
/// Session d'import/synchronisation.
/// </summary>
public sealed class DataSourceSessionResult
{
    public string SessionId { get; init; } = string.Empty;

    public string SourceKey { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string ImportKind { get; init; } = string.Empty;

    public int ProgressPercentage { get; init; }

    public string CurrentStep { get; init; } = string.Empty;

    public string? LastCompletedStep { get; init; }

    public string Message { get; init; } = string.Empty;

    public bool CanResume { get; init; }

    public IReadOnlyCollection<string> AvailableSteps { get; init; } = Array.Empty<string>();

    public DateTime StartedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public DataSourceMetricsResult Metrics { get; init; } = new DataSourceMetricsResult();

    public IReadOnlyCollection<DataSourceLogResult> Logs { get; init; } = Array.Empty<DataSourceLogResult>();
}
