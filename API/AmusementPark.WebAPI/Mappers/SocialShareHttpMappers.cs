using AmusementPark.Application.Features.SocialShare.Contracts;
using AmusementPark.WebAPI.Contracts.SocialShare;

namespace AmusementPark.WebAPI.Mappers;

public static class SocialShareHttpMappers
{
    public static SocialShareEventCapture ToApplication(this SocialShareEventRequestDto request, string? userId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SocialShareEventCapture(
            request.TargetType,
            request.TargetId,
            request.TargetTitle,
            request.Url,
            request.LanguageCode,
            request.Channel,
            userId);
    }

    public static SocialShareEventResponseDto ToHttp(this SocialShareEventCaptureResult result)
    {
        return new SocialShareEventResponseDto
        {
            Accepted = result.Accepted,
            OccurredAtUtc = result.OccurredAtUtc,
        };
    }

    public static SocialShareStatsDto ToHttp(this SocialShareStatsResult result)
    {
        return new SocialShareStatsDto
        {
            FromUtc = result.FromUtc,
            ToUtc = result.ToUtc,
            TotalEvents = result.TotalEvents,
            AnonymousEvents = result.AnonymousEvents,
            AuthenticatedEvents = result.AuthenticatedEvents,
            Daily = result.Daily.Select(static item => item.ToHttp()).ToList(),
            Channels = result.Channels.Select(static item => item.ToHttp()).ToList(),
            TargetTypes = result.TargetTypes.Select(static item => item.ToHttp()).ToList(),
            VisitorKinds = result.VisitorKinds.Select(static item => item.ToHttp()).ToList(),
            TopTargets = result.TopTargets.Select(static item => item.ToHttp()).ToList(),
        };
    }

    private static SocialShareDailyStatsPointDto ToHttp(this SocialShareDailyStatsPoint result)
    {
        return new SocialShareDailyStatsPointDto
        {
            Date = result.Date,
            Count = result.Count,
        };
    }

    private static SocialShareDimensionCountDto ToHttp(this SocialShareDimensionCount result)
    {
        return new SocialShareDimensionCountDto
        {
            Key = result.Key,
            Count = result.Count,
        };
    }

    private static SocialShareTopTargetDto ToHttp(this SocialShareTopTarget result)
    {
        return new SocialShareTopTargetDto
        {
            TargetType = result.TargetType,
            TargetId = result.TargetId,
            TargetTitle = result.TargetTitle,
            Url = result.Url,
            Count = result.Count,
        };
    }
}
