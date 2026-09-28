namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalParkDiagnostics
{
    public HistoricalParkDiagnostics(
        int factCount,
        int relationCount,
        IReadOnlyCollection<HistoricalParkDiagnosticIssue> issues,
        IReadOnlyCollection<HistoricalDecadeCoverage> decadeCoverage,
        IReadOnlyCollection<HistoricalWorkflowStageCount> workflow)
    {
        ArgumentNullException.ThrowIfNull(issues);
        ArgumentNullException.ThrowIfNull(decadeCoverage);
        ArgumentNullException.ThrowIfNull(workflow);
        if (factCount < 0 || relationCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(factCount));
        }

        this.FactCount = factCount;
        this.RelationCount = relationCount;
        this.Issues = Array.AsReadOnly(issues.ToArray());
        this.DecadeCoverage = Array.AsReadOnly(decadeCoverage.ToArray());
        this.Workflow = Array.AsReadOnly(workflow.ToArray());
    }

    public int FactCount { get; }

    public int RelationCount { get; }

    public IReadOnlyList<HistoricalParkDiagnosticIssue> Issues { get; }

    public IReadOnlyList<HistoricalDecadeCoverage> DecadeCoverage { get; }

    public IReadOnlyList<HistoricalWorkflowStageCount> Workflow { get; }

    public int BlockingIssueCount => this.Issues.Count(static issue =>
        issue.Severity == HistoricalDiagnosticSeverity.Error);
}
