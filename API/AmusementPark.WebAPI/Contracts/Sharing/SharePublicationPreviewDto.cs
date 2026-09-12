namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class SharePublicationPreviewDto
{
    public string PublicationType { get; set; } = string.Empty;

    public long SourceVersion { get; set; }

    public string ApprovalToken { get; set; } = string.Empty;

    public ShareContentPolicyPreviewDto ContentPolicy { get; set; } = new ShareContentPolicyPreviewDto();

    public PersonalRankingSharePreviewDto? PersonalRanking { get; set; }

    public VisitRecapSharePreviewDto? VisitRecap { get; set; }

    public YearRecapSharePreviewDto? YearRecap { get; set; }

    public PassportProfileSharePreviewDto? PassportProfile { get; set; }
}
