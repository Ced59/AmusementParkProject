namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class PassportProfileShareParkDto
{
    public string Name { get; set; } = string.Empty;

    public string? CountryCode { get; set; }

    public long VisitCount { get; set; }

    public int FirstVisitYear { get; set; }

    public int LastVisitYear { get; set; }

    public long? CompletedRideCount { get; set; }

    public PassportProfileShareRatingSummaryDto? VisitRatings { get; set; }
}
