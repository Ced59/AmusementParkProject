namespace AmusementPark.WebAPI.Contracts.History;

public sealed class AdminHistoricalParkWorkbenchDto
{
    public string ParkId { get; init; } = string.Empty;

    public string ParkName { get; init; } = string.Empty;

    public IReadOnlyCollection<AdminHistoricalSubjectDto> Subjects { get; init; } =
        Array.Empty<AdminHistoricalSubjectDto>();

    public IReadOnlyCollection<AdminHistoricalFactDto> Facts { get; init; } =
        Array.Empty<AdminHistoricalFactDto>();

    public IReadOnlyCollection<AdminHistoricalRelationDto> Relations { get; init; } =
        Array.Empty<AdminHistoricalRelationDto>();

    public IReadOnlyCollection<AdminHistoricalSourceDto> Sources { get; init; } =
        Array.Empty<AdminHistoricalSourceDto>();

    public AdminHistoricalParkDiagnosticsDto Diagnostics { get; init; } = new();
}
