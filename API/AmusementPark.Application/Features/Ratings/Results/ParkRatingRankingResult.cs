using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record ParkRatingRankingResult(
    int? Rank,
    string ParkId,
    string ParkName,
    long RatingCount,
    double Score,
    long ParkRatingCount,
    double ParkAverageRating,
    long ItemsRatingCount,
    double ItemsAverageRating,
    IReadOnlyCollection<ParkRatingRankingCategoryResult> Categories)
{
    public long RatingObservationCount => this.Evidence?.RatingObservationCount ?? this.RatingCount;

    public long? UniqueContributorCount => this.Evidence?.UniqueContributorCount;

    public RankingEvidenceResult? Evidence { get; init; }

    public RatingMethodologyVersion? MethodologyVersion => this.Evidence?.MethodologyVersion;

    public DateTime? GeneratedAtUtc { get; init; }
}
