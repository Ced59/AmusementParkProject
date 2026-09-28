namespace AmusementPark.WebAPI.Contracts.History;

public sealed class AdminHistoricalVisitDiagnosticsDto
{
    public int PotentiallyInconsistentVisitCount { get; init; }

    public int ConfirmedConflictVisitCount { get; init; }

    public int UnverifiedVisitCount { get; init; }
}
