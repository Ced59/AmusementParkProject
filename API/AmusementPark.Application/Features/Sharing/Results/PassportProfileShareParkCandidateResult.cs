namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record PassportProfileShareParkCandidateResult(
    string ParkId,
    string Name,
    string? CountryCode,
    long VisitCount);
