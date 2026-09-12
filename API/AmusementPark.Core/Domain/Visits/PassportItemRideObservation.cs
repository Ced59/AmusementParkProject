using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Observation active d'un tour terminé, suffisante pour les statistiques privées d'un élément.
/// </summary>
public sealed record PassportItemRideObservation
{
    public PassportItemRideObservation(
        string rideOccurrenceId,
        string visitId,
        VisitDate visitDate,
        long sortPosition,
        RatingValue? assessment)
    {
        this.RideOccurrenceId = IdentifierRules.NormalizeRequired(
            rideOccurrenceId,
            nameof(rideOccurrenceId));
        this.VisitId = IdentifierRules.NormalizeRequired(visitId, nameof(visitId));
        this.VisitDate = visitDate ?? throw new ArgumentNullException(nameof(visitDate));
        this.SortPosition = sortPosition;
        this.Assessment = assessment;
    }

    public string RideOccurrenceId { get; }

    public string VisitId { get; }

    public VisitDate VisitDate { get; }

    public long SortPosition { get; }

    public RatingValue? Assessment { get; }
}
