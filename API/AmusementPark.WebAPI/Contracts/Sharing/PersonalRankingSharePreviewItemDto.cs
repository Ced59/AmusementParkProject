namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class PersonalRankingSharePreviewItemDto
{
    public string TargetType { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public string? ParkName { get; set; }

    public string? ParkItemCategory { get; set; }

    public string? ParkItemType { get; set; }

    public double Rating { get; set; }
}
