namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class SharedVisitRecapDto
{
    public DateTime PublishedAtUtc { get; set; }

    public SharedVisitRecapContentDto VisitRecap { get; set; } = new SharedVisitRecapContentDto();
}
