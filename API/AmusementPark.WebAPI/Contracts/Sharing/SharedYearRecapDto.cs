namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class SharedYearRecapDto
{
    public DateTime PublishedAtUtc { get; set; }

    public long PublicationVersion { get; set; }

    public YearRecapSharePreviewDto YearRecap { get; set; } = new YearRecapSharePreviewDto();
}
