using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class ParkItemRatingRankingDto
{
    public int? Rank { get; set; }

    public string TargetId { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public string ParkName { get; set; } = string.Empty;

    public string ParkItemCategory { get; set; } = string.Empty;

    public string? ParkItemType { get; set; }

    /// <summary>Alias historique du nombre d'observations retenues pour cet élément.</summary>
    public long RatingCount { get; set; }

    public long RatingObservationCount { get; set; }

    public long? UniqueContributorCount { get; set; }

    public double AverageRating { get; set; }

    public double BayesianScore { get; set; }

    public RankingEvidenceDto? Evidence { get; set; }

    public string? MethodologyVersion { get; set; }

    public DateTime? GeneratedAtUtc { get; set; }
}
