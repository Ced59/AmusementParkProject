namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class SharePublicationSettingsDto
{
    public bool IsPublic { get; set; }

    public bool IsModerationSuspended { get; set; }

    public string? PublicationId { get; set; }

    public long? PublicationVersion { get; set; }

    public string? ShareId { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public int? PolicySchemaVersion { get; set; }

    public string? DatePrecision { get; set; }

    public List<string> IncludedFields { get; set; } = new();

    public string? Visibility { get; set; }
}
