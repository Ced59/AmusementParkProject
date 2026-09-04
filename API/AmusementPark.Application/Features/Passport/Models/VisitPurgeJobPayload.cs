namespace AmusementPark.Application.Features.Passport.Models;

public sealed record VisitPurgeJobPayload(
    string VisitId,
    string UserId);
