using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportParkAssessmentPoint(
    string VisitId,
    VisitDate VisitDate,
    RatingValue Rating);
