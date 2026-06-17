namespace AmusementPark.WebAPI.Contracts.SocialShare;

public sealed class SocialShareEventRequestDto
{
    public string? TargetType { get; set; }

    public string? TargetId { get; set; }

    public string? TargetTitle { get; set; }

    public string? Url { get; set; }

    public string? LanguageCode { get; set; }

    public string? Channel { get; set; }
}

public sealed class SocialShareEventResponseDto
{
    public bool Accepted { get; set; }

    public DateTime? OccurredAtUtc { get; set; }
}

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

public sealed class SocialShareDailyStatsPointDto
{
    public string Date { get; set; } = string.Empty;

    public long Count { get; set; }
}

public sealed class SocialShareDimensionCountDto
{
    public string Key { get; set; } = string.Empty;

    public long Count { get; set; }
}

public sealed class SocialShareTopTargetDto
{
    public string TargetType { get; set; } = string.Empty;

    public string? TargetId { get; set; }

    public string? TargetTitle { get; set; }

    public string Url { get; set; } = string.Empty;

    public long Count { get; set; }
}
