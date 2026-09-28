using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportHistoricalVisitContextObservation
{
    public PassportHistoricalVisitContextObservation(
        string visitId,
        string parkId,
        VisitDate visitDate,
        HistoricalCoverageStatus coverageStatus,
        IReadOnlyCollection<PassportHistoricalTargetStateObservation> targets)
    {
        this.VisitId = IdentifierRules.NormalizeRequired(visitId, nameof(visitId));
        this.ParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        this.VisitDate = visitDate ?? throw new ArgumentNullException(nameof(visitDate));
        if (!Enum.IsDefined(coverageStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(coverageStatus));
        }

        ArgumentNullException.ThrowIfNull(targets);
        this.CoverageStatus = coverageStatus;
        this.Targets = targets.ToArray();
    }

    public string VisitId { get; }

    public string ParkId { get; }

    public VisitDate VisitDate { get; }

    public HistoricalCoverageStatus CoverageStatus { get; }

    public IReadOnlyCollection<PassportHistoricalTargetStateObservation> Targets { get; }
}
