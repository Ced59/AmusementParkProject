namespace AmusementPark.WebAPI.Contracts.SocialShare;

public sealed class SocialShareStatsDto
{
    public DateTime FromUtc { get; set; }

    public DateTime ToUtc { get; set; }

    public long TotalEvents { get; set; }

    public long AnonymousEvents { get; set; }

    public long AuthenticatedEvents { get; set; }

    public IReadOnlyCollection<SocialShareDailyStatsPointDto> Daily { get; set; } = Array.Empty<SocialShareDailyStatsPointDto>();

    public IReadOnlyCollection<SocialShareDimensionCountDto> Channels { get; set; } = Array.Empty<SocialShareDimensionCountDto>();

    public IReadOnlyCollection<SocialShareDimensionCountDto> TargetTypes { get; set; } = Array.Empty<SocialShareDimensionCountDto>();

    public IReadOnlyCollection<SocialShareDimensionCountDto> VisitorKinds { get; set; } = Array.Empty<SocialShareDimensionCountDto>();

    public IReadOnlyCollection<SocialShareTopTargetDto> TopTargets { get; set; } = Array.Empty<SocialShareTopTargetDto>();
}
