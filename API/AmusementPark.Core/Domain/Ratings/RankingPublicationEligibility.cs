namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Verdict sur la possibilité de publier un tableau de classement.
/// </summary>
public sealed record RankingPublicationEligibility(
    bool IsEligible,
    RankingIneligibilityReason? IneligibilityReason);
