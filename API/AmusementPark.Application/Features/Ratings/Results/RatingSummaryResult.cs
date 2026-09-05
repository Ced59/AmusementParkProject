using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingSummaryResult(
    RatingTargetType TargetType,
    string TargetId,
    long RatingCount,
    double AverageRating,
    double BayesianScore)
{
    private RatingMethodologyVersion? methodologyVersion;

    public int? Rank { get; init; }

    public DateTime? GeneratedAtUtc { get; init; }

    public long RatingObservationCount => this.Evidence?.RatingObservationCount ?? this.RatingCount;

    public long? UniqueContributorCount => this.Evidence?.UniqueContributorCount;

    public RankingEvidenceResult? Evidence { get; init; }

    public RatingMethodologyVersion? MethodologyVersion
    {
        get => this.methodologyVersion ?? this.Evidence?.MethodologyVersion;
        init => this.methodologyVersion = value;
    }
}
