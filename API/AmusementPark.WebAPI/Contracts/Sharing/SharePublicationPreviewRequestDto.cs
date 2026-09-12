namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class SharePublicationPreviewRequestDto
{
    public string PublicationType { get; set; } = string.Empty;

    public string? SourceId { get; set; }

    public string DatePrecision { get; set; } = string.Empty;

    public List<string> IncludedFields { get; set; } = new();

    public VisitRecapShareInputDto? VisitRecap { get; set; }

    public YearRecapShareInputDto? YearRecap { get; set; }

    public PassportProfileShareInputDto? PassportProfile { get; set; }
}
