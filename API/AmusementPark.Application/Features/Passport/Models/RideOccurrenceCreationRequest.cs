using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record RideOccurrenceCreationRequest(
    VisitId VisitId,
    string UserId,
    IReadOnlyList<RideOccurrenceCreationRequestItem> Items,
    long? ContentFenceToken = null);
