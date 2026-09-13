namespace AmusementPark.Application.Features.DataSources.Results;

/// <summary>
/// Entrée de log métier d'une session.
/// </summary>
public sealed class DataSourceLogResult
{
    public DateTime OccurredAtUtc { get; init; }

    public string Level { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}
