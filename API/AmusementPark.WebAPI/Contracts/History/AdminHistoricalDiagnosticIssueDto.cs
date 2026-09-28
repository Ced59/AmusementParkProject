namespace AmusementPark.WebAPI.Contracts.History;

public sealed class AdminHistoricalDiagnosticIssueDto
{
    public string Code { get; init; } = string.Empty;

    public string Severity { get; init; } = string.Empty;

    public string? SubjectType { get; init; }

    public string? SubjectId { get; init; }

    public string? SubjectLabel { get; init; }

    public string? FactId { get; init; }

    public string? RelationId { get; init; }
}
