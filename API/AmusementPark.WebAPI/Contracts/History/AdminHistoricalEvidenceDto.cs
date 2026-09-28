namespace AmusementPark.WebAPI.Contracts.History;

public sealed class AdminHistoricalEvidenceDto
{
    public string SourceId { get; init; } = string.Empty;

    public int Revision { get; init; }

    public string Position { get; init; } = string.Empty;
}
