namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record PassportProfileShareRatingCandidateResult(
    string SelectionKey,
    string Name,
    string? ParkName,
    double Rating);
