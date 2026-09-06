namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class ShareContentPolicyPreviewDto
{
    public int SchemaVersion { get; set; }

    public string DatePrecision { get; set; } = string.Empty;

    public List<string> IncludedFields { get; set; } = new();
}
