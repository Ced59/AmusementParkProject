using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.WebAPI.Contracts.Watchlists;

namespace AmusementPark.WebAPI.Mappers;

internal static class NotificationDigestHttpMapper
{
    internal static NotificationDigestPreviewDto ToHttp(
        this NotificationDigestPreviewResult result)
    {
        return new NotificationDigestPreviewDto(
            result.Channel.ToString(),
            result.Frequency.ToString(),
            result.PeriodStartUtc,
            result.PeriodEndUtc,
            result.ObservedNotificationCount,
            result.GroupedItemCount,
            result.EligibleItemCount,
            result.SuppressedItemCount,
            result.MergedCorrectionCount,
            result.UpdatedAtUtc);
    }
}
