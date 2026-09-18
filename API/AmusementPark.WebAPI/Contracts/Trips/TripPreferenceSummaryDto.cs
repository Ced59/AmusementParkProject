namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripPreferenceSummaryDto
{
    public string TripPlanId { get; set; } = string.Empty;

    public string TripTitle { get; set; } = string.Empty;

    public long PlanVersion { get; set; }

    public int ParticipantCount { get; set; }

    public bool CanDecide { get; set; }

    public List<TripItemPreferenceSummaryDto> Items { get; set; } = new();
}
