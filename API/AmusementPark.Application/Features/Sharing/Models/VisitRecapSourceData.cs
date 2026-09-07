using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record VisitRecapSourceData(
    string ParkId,
    VisitDate Date,
    RatingValue? ParkRating,
    VisitRecapSourceRevision Revision,
    IReadOnlyCollection<VisitRecapSourceOccurrence> Occurrences,
    bool IsComplete = true);
