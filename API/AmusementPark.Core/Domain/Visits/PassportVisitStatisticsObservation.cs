using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportVisitStatisticsObservation
{
    public PassportVisitStatisticsObservation(
        string visitId,
        string parkId,
        VisitDate visitDate,
        RatingValue? parkAssessment)
    {
        this.VisitId = IdentifierRules.NormalizeRequired(visitId, nameof(visitId));
        this.ParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        this.VisitDate = visitDate ?? throw new ArgumentNullException(nameof(visitDate));
        this.ParkAssessment = parkAssessment;
    }

    public string VisitId { get; }

    public string ParkId { get; }

    public VisitDate VisitDate { get; }

    public RatingValue? ParkAssessment { get; }
}
