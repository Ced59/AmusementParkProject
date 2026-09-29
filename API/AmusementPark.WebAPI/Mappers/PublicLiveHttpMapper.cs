using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.WebAPI.Contracts.LiveData;

namespace AmusementPark.WebAPI.Mappers;

public static class PublicLiveHttpMapper
{
    public static PublicLiveTargetDto ToHttp(this PublicLiveTargetResult result)
    {
        return new PublicLiveTargetDto(
            result.TargetId,
            result.TargetType.ToString(),
            result.DisplayName,
            result.ParkId,
            result.ParkDisplayName,
            result.Availability.ToString(),
            result.Status?.ToString(),
            result.Queues.Select(static queue => new PublicLiveQueueDto(
                queue.Kind.ToString(),
                queue.WaitTimeMinutes,
                queue.IsEstimated,
                queue.Availability.ToString(),
                queue.ReturnStartUtc,
                queue.ReturnEndUtc,
                queue.CurrentGroupStart,
                queue.CurrentGroupEnd,
                queue.NextAllocationUtc,
                queue.PriceMinorUnits,
                queue.CurrencyCode)).ToList().AsReadOnly(),
            result.AsOfUtc,
            result.ObservedAtUtc,
            result.ReceivedAtUtc,
            result.AgeSeconds,
            result.Freshness?.ToString(),
            result.ExpiresAtUtc,
            result.Source is null
                ? null
                : new PublicLiveSourceDto(
                    result.Source.Id,
                    result.Source.DisplayName,
                    result.Source.Type.ToString(),
                    result.Source.AttributionText,
                    result.Source.AttributionUrl),
            result.Confidence?.ToString());
    }

    public static PublicParkLiveItemsDto ToHttp(this PublicParkLiveItemsResult result)
    {
        return new PublicParkLiveItemsDto(
            result.ParkId,
            result.ParkDisplayName,
            result.AsOfUtc,
            result.Items.Select(static item => item.ToHttp()).ToList().AsReadOnly());
    }
}
