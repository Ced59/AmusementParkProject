namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalParkDiagnosticIssue
{
    public HistoricalParkDiagnosticIssue(
        HistoricalDiagnosticCode code,
        HistoricalDiagnosticSeverity severity,
        HistoricalSubject? subject,
        Guid? factId,
        Guid? relationId)
    {
        if (!Enum.IsDefined(code)
            || !Enum.IsDefined(severity)
            || factId == Guid.Empty
            || relationId == Guid.Empty
            || factId.HasValue && relationId.HasValue)
        {
            throw new ArgumentException("The historical diagnostic issue is invalid.");
        }

        this.Code = code;
        this.Severity = severity;
        this.Subject = subject;
        this.FactId = factId;
        this.RelationId = relationId;
    }

    public HistoricalDiagnosticCode Code { get; }

    public HistoricalDiagnosticSeverity Severity { get; }

    public HistoricalSubject? Subject { get; }

    public Guid? FactId { get; }

    public Guid? RelationId { get; }
}
