namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitScoreComponentDto
{
    public string Kind { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;

    public decimal? Value { get; init; }

    public decimal CoveragePercent { get; init; }

    public string Confidence { get; init; } = string.Empty;

    public decimal BaseWeightPercent { get; init; }

    public decimal ApplicableWeightPercent { get; init; }

    public decimal? KnownScoreWeightPercent { get; init; }

    public decimal? Contribution { get; init; }

    public IReadOnlyCollection<string> Reasons { get; init; } = Array.Empty<string>();
}
