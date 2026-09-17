namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripFitRecommendationSnapshotDto
{
    public string MethodVersion { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public DateTime CalculatedAtUtc { get; set; }
}
