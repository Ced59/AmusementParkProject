namespace AmusementPark.WebAPI.Contracts.SocialShare;

public sealed class SocialShareDailyStatsPointDto
{
    public string Date { get; set; } = string.Empty;

    public long Count { get; set; }
}
