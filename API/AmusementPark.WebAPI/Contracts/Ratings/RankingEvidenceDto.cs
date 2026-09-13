using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RankingEvidenceDto
{
    public string Level { get; set; } = string.Empty;

    public bool IsEligibleForMainRanking { get; set; }

    public long? DirectParkContributorCount { get; set; }

    public long? ItemContributorCount { get; set; }

    public int? EligibleItemCount { get; set; }

    public int? EligibleCategoryCount { get; set; }

    public string? IneligibilityReason { get; set; }

    public int? NextThreshold { get; set; }
}
