namespace AmusementPark.Application.Features.ParkFit.Results;

/// <summary>
/// Synthèse anonyme des verdicts d'une personne, identifiée uniquement par son ordre dans la requête.
/// </summary>
public sealed class ParkFitSearchMemberSummaryResult
{
    public int MemberNumber { get; init; }

    public int CompatibleAloneAttractionCount { get; init; }

    public int CompatibleWithCompanionAttractionCount { get; init; }

    public int IncompatibleAttractionCount { get; init; }

    public int UnknownAttractionCount { get; init; }

    public int NotApplicableAttractionCount { get; init; }
}
