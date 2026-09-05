using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record ParkItemRatingRankingResult(
    int? Rank,
    string TargetId,
    string TargetName,
    string ParkId,
    string ParkName,
    ParkItemCategory ParkItemCategory,
    ParkItemType? ParkItemType,
    long RatingCount,
    double AverageRating,
    double BayesianScore)
{
    public long RatingObservationCount => this.Evidence?.RatingObservationCount ?? this.RatingCount;

    public long? UniqueContributorCount => this.Evidence?.UniqueContributorCount;

    public RankingEvidenceResult? Evidence { get; init; }

    public RatingMethodologyVersion? MethodologyVersion => this.Evidence?.MethodologyVersion;

    public DateTime? GeneratedAtUtc { get; init; }
}
