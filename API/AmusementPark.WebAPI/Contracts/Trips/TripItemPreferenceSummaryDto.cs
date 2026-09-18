namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripItemPreferenceSummaryDto
{
    public string ParkId { get; set; } = string.Empty;

    public string ParkName { get; set; } = string.Empty;

    public string ParkItemId { get; set; } = string.Empty;

    public string ParkItemName { get; set; } = string.Empty;

    public string? MainImageId { get; set; }

    public int MustDoCount { get; set; }

    public int WantToDoCount { get; set; }

    public int OptionalCount { get; set; }

    public int NotForMeCount { get; set; }

    public int UnansweredCount { get; set; }

    public string Compatibility { get; set; } = string.Empty;

    public bool IsCompatibilityKnown { get; set; }

    public bool HasIndividualConstraint { get; set; }

    public bool IsGroupPriority { get; set; }

    public string? OfficialStatus { get; set; }

    public string? OfficialSourceUrl { get; set; }

    public DateTime? OfficialStatusVerifiedAtUtc { get; set; }

    public TripItemDecisionDto? Decision { get; set; }
}
