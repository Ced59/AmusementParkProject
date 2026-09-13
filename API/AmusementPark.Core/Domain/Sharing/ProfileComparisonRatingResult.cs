namespace AmusementPark.Core.Domain.Sharing;

public sealed record ProfileComparisonRatingResult(
    string TargetType,
    string Name,
    string? ParkName,
    string? Category,
    double CreatorRating,
    double AcceptorRating,
    double AbsoluteDifference,
    ProfileComparisonRatingAffinity Affinity);
