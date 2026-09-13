using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class ParkRatingRankingDto
{
    public int? Rank { get; set; }

    public string ParkId { get; set; } = string.Empty;

    public string ParkName { get; set; } = string.Empty;

    /// <summary>Alias historique du nombre d'observations retenues dans le score composé du parc.</summary>
    public long RatingCount { get; set; }

    public long RatingObservationCount { get; set; }

    public long? UniqueContributorCount { get; set; }

    public double Score { get; set; }

    public long ParkRatingCount { get; set; }

    public double ParkAverageRating { get; set; }

    public long ItemsRatingCount { get; set; }

    public double ItemsAverageRating { get; set; }

    public RankingEvidenceDto? Evidence { get; set; }

    public string? MethodologyVersion { get; set; }

    public DateTime? GeneratedAtUtc { get; set; }

    public IReadOnlyCollection<ParkRatingRankingCategoryDto> Categories { get; set; } = Array.Empty<ParkRatingRankingCategoryDto>();
}
