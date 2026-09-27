using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportHistoricalRideContextObservation
{
    public PassportHistoricalRideContextObservation(
        string rideOccurrenceId,
        string visitId,
        string parkId,
        string parkItemId,
        VisitDate visitDate,
        RideOccurrenceStatus status,
        PassportHistoricalTargetStateObservation? targetAtVisit,
        PassportHistoricalTargetStateObservation? currentTarget)
    {
        this.RideOccurrenceId = IdentifierRules.NormalizeRequired(
            rideOccurrenceId,
            nameof(rideOccurrenceId));
        this.VisitId = IdentifierRules.NormalizeRequired(visitId, nameof(visitId));
        this.ParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        this.ParkItemId = IdentifierRules.NormalizeRequired(parkItemId, nameof(parkItemId));
        this.VisitDate = visitDate ?? throw new ArgumentNullException(nameof(visitDate));
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        this.Status = status;
        this.TargetAtVisit = targetAtVisit;
        this.CurrentTarget = currentTarget;
    }

    public string RideOccurrenceId { get; }

    public string VisitId { get; }

    public string ParkId { get; }

    public string ParkItemId { get; }

    public VisitDate VisitDate { get; }

    public RideOccurrenceStatus Status { get; }

    public PassportHistoricalTargetStateObservation? TargetAtVisit { get; }

    public PassportHistoricalTargetStateObservation? CurrentTarget { get; }
}
