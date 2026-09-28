namespace AmusementPark.WebAPI.Contracts.History;

public sealed class AdminHistoricalSubjectDto
{
    public string Type { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string PublicationPolicy { get; init; } = string.Empty;
}
