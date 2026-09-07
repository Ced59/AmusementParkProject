namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class UserRankingShareSettingsDto
{
    public bool IsPublic { get; set; }

    public string? ShareId { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public int? PolicySchemaVersion { get; set; }

    public string? DatePrecision { get; set; }

    public List<string> IncludedFields { get; set; } = new();
}
