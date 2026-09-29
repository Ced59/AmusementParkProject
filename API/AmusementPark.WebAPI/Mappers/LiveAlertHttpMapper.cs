using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.WebAPI.Contracts.Watchlists;

namespace AmusementPark.WebAPI.Mappers;

internal static class LiveAlertHttpMapper
{
    public static bool TryToApplication(
        this CreateLiveAlertRequestDto request,
        out LiveAlertPreferenceInput? input)
    {
        input = null;
        if (string.IsNullOrWhiteSpace(request.TargetId)
            || !Enum.TryParse(request.Type, true, out LiveAlertType type)
            || !Enum.IsDefined(type))
        {
            return false;
        }

        input = new LiveAlertPreferenceInput(
            request.TargetId.Trim(),
            type,
            request.ThresholdMinutes,
            request.DurationMinutes);
        return true;
    }

    public static LiveAlertDashboardDto ToHttp(this LiveAlertDashboardResult result)
    {
        return new LiveAlertDashboardDto(
            result.Subscriptions.Select(static item => item.ToHttp()).ToArray(),
            result.Notifications.Select(static item => item.ToHttp()).ToArray(),
            result.UnreadCount,
            result.RetentionDays);
    }

    public static LiveAlertSubscriptionDto ToHttp(this LiveAlertSubscriptionResult result)
    {
        return new LiveAlertSubscriptionDto(
            result.SubscriptionId,
            result.TargetId,
            result.ParkId,
            result.TargetName,
            result.ParkName,
            result.MainImageId,
            result.Type.ToString(),
            result.ThresholdMinutes,
            result.CreatedAtUtc,
            result.ExpiresAtUtc,
            result.LastObservedAtUtc,
            result.LastStatus?.ToString(),
            result.LastWaitMinutes,
            result.CooldownMinutes,
            result.HysteresisMinutes,
            result.Version);
    }

    private static LiveAlertNotificationDto ToHttp(this LiveAlertNotificationResult result)
    {
        return new LiveAlertNotificationDto(
            result.NotificationId,
            result.SubscriptionId,
            result.TargetId,
            result.ParkId,
            result.TargetName,
            result.ParkName,
            result.MainImageId,
            result.Type.ToString(),
            result.ThresholdMinutes,
            result.PreviousStatus?.ToString(),
            result.CurrentStatus.ToString(),
            result.PreviousWaitMinutes,
            result.CurrentWaitMinutes,
            result.SourceId,
            result.SourceName,
            result.AttributionText,
            result.AttributionUrl,
            result.ObservedAtUtc,
            result.DeliveredAtUtc,
            result.AgeSecondsAtDelivery,
            result.Status.ToString(),
            result.ReadAtUtc,
            result.ExpiresAtUtc,
            result.Version);
    }
}
