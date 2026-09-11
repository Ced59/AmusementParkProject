namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class YearRecapShareParkDto
{
    public string Name { get; set; } = string.Empty;

    public long VisitCount { get; set; }

    public long? CompletedRideCount { get; set; }
}
