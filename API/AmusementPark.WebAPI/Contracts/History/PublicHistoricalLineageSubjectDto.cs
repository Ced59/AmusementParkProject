namespace AmusementPark.WebAPI.Contracts.History;

public sealed class PublicHistoricalLineageSubjectDto
{
    public string Key { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public bool IsHistoricalOnly { get; init; }
}
