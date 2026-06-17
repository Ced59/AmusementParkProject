namespace AmusementPark.Application.Features.SocialShare.Contracts;

public sealed record SocialShareEventCapture(
    string? TargetType,
    string? TargetId,
    string? TargetTitle,
    string? Url,
    string? LanguageCode,
    string? Channel,
    string? UserId);

public sealed record SocialShareEventCaptureResult(bool Accepted, DateTime? OccurredAtUtc);

public sealed record SocialShareStatsCriteria(DateTime? FromUtc, DateTime? ToUtc);

public sealed record SocialShareStatsResult(
    DateTime FromUtc,
    DateTime ToUtc,
    long TotalEvents,
    long AnonymousEvents,
    long AuthenticatedEvents,
    IReadOnlyCollection<SocialShareDailyStatsPoint> Daily,
    IReadOnlyCollection<SocialShareDimensionCount> Channels,
    IReadOnlyCollection<SocialShareDimensionCount> TargetTypes,
    IReadOnlyCollection<SocialShareDimensionCount> VisitorKinds,
    IReadOnlyCollection<SocialShareTopTarget> TopTargets);

public sealed record SocialShareDailyStatsPoint(string Date, long Count);

public sealed record SocialShareDimensionCount(string Key, long Count);

public sealed record SocialShareTopTarget(
    string TargetType,
    string? TargetId,
    string? TargetTitle,
    string Url,
    long Count);
