using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingSummaryDto
{
    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    /// <summary>Alias historique du nombre d'observations retenues pour cette cible simple.</summary>
    public long RatingCount { get; set; }

    public long RatingObservationCount { get; set; }

    public long? UniqueContributorCount { get; set; }

    public double AverageRating { get; set; }

    public double BayesianScore { get; set; }

    public int? Rank { get; set; }

    public DateTime? GeneratedAtUtc { get; set; }

    public RankingEvidenceDto? Evidence { get; set; }

    public string? MethodologyVersion { get; set; }
}
