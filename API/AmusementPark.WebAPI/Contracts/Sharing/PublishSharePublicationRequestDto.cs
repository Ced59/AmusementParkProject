namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class PublishSharePublicationRequestDto
{
    public string PublicationType { get; set; } = string.Empty;

    public string? SourceId { get; set; }

    public long ApprovedSourceVersion { get; set; }

    public int ApprovedPolicySchemaVersion { get; set; }

    public string ApprovedDatePrecision { get; set; } = string.Empty;

    public List<string> ApprovedIncludedFields { get; set; } = new();

    public string ApprovalToken { get; set; } = string.Empty;

    public VisitRecapShareInputDto? VisitRecap { get; set; }

    public YearRecapShareInputDto? YearRecap { get; set; }

    public PassportProfileShareInputDto? PassportProfile { get; set; }
}
