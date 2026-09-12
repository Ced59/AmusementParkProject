namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record PassportProfileShareRatingCandidateResult(
    string SelectionKey,
    string ParkId,
    string Name,
    string? ParkName,
    double Rating);
