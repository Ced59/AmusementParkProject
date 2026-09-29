namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed class LiveQualityReplayDto
{
    public int ExaminedCount { get; set; }

    public int ResolvedCount { get; set; }

    public int StillBlockedCount { get; set; }

    public int PersistedCount { get; set; }

    public int IgnoredAsOlderCount { get; set; }
}
