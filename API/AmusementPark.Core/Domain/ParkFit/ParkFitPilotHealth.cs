namespace AmusementPark.Core.Domain.ParkFit;

public sealed record ParkFitPilotHealth(
    decimal CompletionRatePercent,
    decimal NoResultRatePercent,
    decimal SignificantUnknownRatePercent,
    decimal ExplanationOpenRatePercent,
    decimal ComparisonOpenRatePercent,
    ParkFitPilotSignal Signal,
    bool RequiresQualitativeReview);
