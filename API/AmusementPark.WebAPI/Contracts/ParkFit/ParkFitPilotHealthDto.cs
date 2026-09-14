namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitPilotHealthDto
{
    public decimal CompletionRatePercent { get; init; }

    public decimal NoResultRatePercent { get; init; }

    public decimal SignificantUnknownRatePercent { get; init; }

    public decimal ExplanationOpenRatePercent { get; init; }

    public decimal ComparisonOpenRatePercent { get; init; }

    public string Signal { get; init; } = string.Empty;

    public bool RequiresQualitativeReview { get; init; }
}
