namespace AmusementPark.Application.Features.Passport.Models;

public sealed record VisitExportInvalidationClaim(
    string Token,
    DateTime FenceAtUtc);
