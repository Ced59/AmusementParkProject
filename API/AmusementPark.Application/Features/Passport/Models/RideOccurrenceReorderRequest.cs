using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record RideOccurrenceReorderRequest(
    VisitId VisitId,
    string UserId,
    RideOccurrenceId OccurrenceId,
    long ExpectedVersion,
    RideOccurrenceId? AnchorOccurrenceId,
    RideOccurrencePlacement Placement,
    long? ContentFenceToken = null);
