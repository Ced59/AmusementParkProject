using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.WebAPI.Contracts.Watchlists;

namespace AmusementPark.WebAPI.Mappers;

public static class UserNotificationHttpMapper
{
    public static bool TryToApplication(
        this UserNotificationSearchRequestDto request,
        out UserNotificationSearchCriteria? criteria)
    {
        ArgumentNullException.ThrowIfNull(request);
        criteria = null;
        FactualEventType? eventType = null;
        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            if (!Enum.TryParse(request.EventType.Trim(), true, out FactualEventType parsed)
                || !Enum.IsDefined(parsed))
            {
                return false;
            }

            eventType = parsed;
        }

        criteria = new UserNotificationSearchCriteria(
            new PagedQuery(request.Page, request.Size),
            request.UnreadOnly,
            string.IsNullOrWhiteSpace(request.ParkId) ? null : request.ParkId.Trim(),
            eventType);
        return true;
    }

    public static UserNotificationPageDto ToHttp(this UserNotificationPageResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new UserNotificationPageDto
        {
            Items = result.Items.Select(static notification => notification.ToHttp()).ToArray(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems,
            TotalPages = result.PageSize <= 0
                ? 0
                : (int)Math.Ceiling(result.TotalItems / (double)result.PageSize),
            UnreadCount = result.UnreadCount,
            RetentionDays = result.RetentionDays,
            ParkFilters = result.ParkFilters.Select(static park => new UserNotificationParkFilterDto
            {
                ParkId = park.ParkId,
                ParkName = park.ParkName,
            }).ToArray(),
        };
    }

    private static UserNotificationDto ToHttp(this UserNotificationResult result)
    {
        return new UserNotificationDto
        {
            NotificationId = result.NotificationId,
            EventType = result.EventType.ToString(),
            Status = result.Status.ToString(),
            Target = new UserNotificationTargetDto
            {
                Type = result.Target.Type.ToString(),
                TargetId = result.Target.TargetId,
                ParkId = result.Target.ParkId,
                Name = result.Target.Name,
                ParentParkName = result.Target.ParentParkName,
                MainImageId = result.Target.MainImageId,
            },
            PreviousValue = ToHttp(result.PreviousValue),
            NewValue = ToHttp(result.NewValue),
            Source = new UserNotificationSourceDto
            {
                Type = result.Source.Type.ToString(),
                PublisherName = result.Source.PublisherName,
                Title = result.Source.Title,
                Url = result.Source.Url,
                PublishedAtUtc = result.Source.PublishedAtUtc,
                VerifiedAtUtc = result.Source.VerifiedAtUtc,
            },
            OccurredAtUtc = result.OccurredAtUtc,
            DeliveredAtUtc = result.DeliveredAtUtc,
            ReadAtUtc = result.ReadAtUtc,
            ExpiresAtUtc = result.ExpiresAtUtc,
            Version = result.Version,
            CanManageSubscription = result.CanManageSubscription,
            SubscriptionVersion = result.SubscriptionVersion,
        };
    }

    private static UserNotificationFactValueDto? ToHttp(UserNotificationFactValueResult? value)
    {
        return value is null
            ? null
            : new UserNotificationFactValueDto
            {
                Kind = value.Kind.ToString(),
                CanonicalValue = value.CanonicalValue,
                UnitCode = value.UnitCode,
            };
    }
}
