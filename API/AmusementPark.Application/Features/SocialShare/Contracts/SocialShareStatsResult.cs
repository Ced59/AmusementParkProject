namespace AmusementPark.Application.Features.SocialShare.Contracts;

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
