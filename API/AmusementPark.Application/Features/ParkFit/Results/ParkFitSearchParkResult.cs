using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFit.Results;

/// <summary>
/// Résultat structuré d'un parc sans phrase localisée ni donnée de profil.
/// </summary>
public sealed class ParkFitSearchParkResult
{
    public required Park Park { get; init; }

    public required ParkFitDataQualityAssessment DataQuality { get; init; }

    public required ParkFitScore Score { get; init; }

    public int EveryoneTogetherAttractionCount { get; init; }

    public int SplitRequiredAttractionCount { get; init; }

    public int PartialAttractionCount { get; init; }

    public int NoCompatibleMemberAttractionCount { get; init; }

    public int UnknownAttractionCount { get; init; }

    public IReadOnlyCollection<ParkFitSearchMemberSummaryResult> MemberSummaries { get; init; } =
        Array.Empty<ParkFitSearchMemberSummaryResult>();

    public IReadOnlyCollection<AttractionCompatibilitySourceReference> CriticalSources { get; init; } =
        Array.Empty<AttractionCompatibilitySourceReference>();
}
