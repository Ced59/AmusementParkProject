namespace AmusementPark.WebAPI.Contracts.Parks;

public sealed class ParkOfficialMapFileCreatedDto
{
    public required string StorageKey { get; set; }

    public required string OriginalFileName { get; set; }

    public required string ContentType { get; set; }

    public long SizeInBytes { get; set; }

    public required string SuggestedFormat { get; set; }
}
