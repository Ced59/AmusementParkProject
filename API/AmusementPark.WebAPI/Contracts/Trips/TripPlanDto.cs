namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripPlanDto
{
    public string TripPlanId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public TripDateProposalDto DateProposal { get; set; } = new();

    public string? DestinationTimeZoneId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string AccessScope { get; set; } = string.Empty;

    public int MemberCount { get; set; }

    public bool IsOwner { get; set; }

    public string EffectiveRole { get; set; } = string.Empty;

    public bool CanEditPlan { get; set; }

    public bool CanEditProgram { get; set; }

    public bool CanInvite { get; set; }

    public bool CanChangeRoles { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public long Version { get; set; }
}
