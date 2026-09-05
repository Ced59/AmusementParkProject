namespace AmusementPark.Application.Features.Passport.Results;

public sealed record RideOccurrenceMomentResult(
    TimeOnly? LocalTime,
    bool IsApproximate);
