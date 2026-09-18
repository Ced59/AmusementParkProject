namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripPreferenceBoardDto
{
    public string TripPlanId { get; set; } = string.Empty;

    public string TripTitle { get; set; } = string.Empty;

    public long PlanVersion { get; set; }

    public bool CanVote { get; set; }

    public List<TripItemPreferenceDto> Items { get; set; } = new();
}
