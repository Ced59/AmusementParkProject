namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Niveau de preuve associé à une note communautaire.
/// </summary>
public enum RankingEvidenceLevel
{
    NoEvidence = 0,
    Insufficient = 1,
    Provisional = 2,
    Eligible = 3,
    Established = 4,
    StrongEvidence = 5,
    Excluded = 6,
}
