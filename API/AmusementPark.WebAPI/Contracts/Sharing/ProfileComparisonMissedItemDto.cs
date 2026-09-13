namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class ProfileComparisonMissedItemDto
{
    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public long? CreatorOccurrenceCount { get; set; }

    public long? AcceptorOccurrenceCount { get; set; }
}
