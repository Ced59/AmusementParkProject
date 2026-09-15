namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class UserCollectionEntryDto
{
    public string EntryId { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string TargetStatus { get; set; } = string.Empty;

    public string? TargetName { get; set; }

    public string? ParentParkId { get; set; }

    public string? ParentParkName { get; set; }

    public string? MainImageId { get; set; }

    public string? PrivateNote { get; set; }

    public int? Priority { get; set; }

    public DateOnly? PreferredStartsOn { get; set; }

    public DateOnly? PreferredEndsOn { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public long Version { get; set; }
}
