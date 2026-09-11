namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class SharedPassportProfileDto
{
    public DateTime PublishedAtUtc { get; set; }

    public PassportProfileSharePreviewDto PassportProfile { get; set; } =
        new PassportProfileSharePreviewDto();
}
