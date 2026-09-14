namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Résultat individuel explicable, daté et sourcé.
/// </summary>
public sealed class AttractionCompatibility
{
    public required string MethodVersion { get; init; }

    public required AttractionCompatibilityState State { get; init; }

    public required IReadOnlyCollection<AttractionCompatibilityReason> Reasons { get; init; }

    public required ParkFitDataConfidence Confidence { get; init; }

    public DateTime? LastVerifiedAtUtc { get; init; }

    public required DateTime EvaluatedAtUtc { get; init; }

    public required DateOnly EvaluationDate { get; init; }

    public required IReadOnlyCollection<AttractionCompatibilitySourceReference> Sources { get; init; }
}
