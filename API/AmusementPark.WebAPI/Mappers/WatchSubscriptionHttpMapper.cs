using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.WebAPI.Contracts.Watchlists;

namespace AmusementPark.WebAPI.Mappers;

public static class WatchSubscriptionHttpMapper
{
    public static bool TryToApplication(
        this WatchSubscriptionWriteRequestDto request,
        out WatchSubscriptionPreferenceInput? input)
    {
        ArgumentNullException.ThrowIfNull(request);
        input = null;
        if (!TryParse(request.TargetType, out CollectionTargetType targetType)
            || !TryParse(request.Frequency, out NotificationFrequency frequency)
            || !TryParseMany(request.EventTypes, out FactualEventType[] eventTypes)
            || !TryParseMany(request.Channels, out NotificationChannel[] channels)
            || string.IsNullOrWhiteSpace(request.TargetId))
        {
            return false;
        }

        input = new WatchSubscriptionPreferenceInput(
            targetType,
            request.TargetId.Trim(),
            eventTypes,
            frequency,
            channels);
        return true;
    }

    public static bool TryToApplication(
        this WatchSubscriptionUpdateRequestDto request,
        out WatchSubscriptionSettingsInput? input)
    {
        ArgumentNullException.ThrowIfNull(request);
        input = null;
        if (!TryParse(request.Frequency, out NotificationFrequency frequency)
            || !TryParseMany(request.EventTypes, out FactualEventType[] eventTypes)
            || !TryParseMany(request.Channels, out NotificationChannel[] channels))
        {
            return false;
        }

        input = new WatchSubscriptionSettingsInput(eventTypes, frequency, channels);
        return true;
    }

    public static WatchSubscriptionDto ToHttp(this WatchSubscriptionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new WatchSubscriptionDto
        {
            SubscriptionId = result.SubscriptionId,
            TargetType = result.TargetType.ToString(),
            TargetId = result.TargetId,
            TargetName = result.TargetName,
            ParentParkId = result.ParentParkId,
            ParentParkName = result.ParentParkName,
            MainImageId = result.MainImageId,
            EventTypes = result.EventTypes.Select(static type => type.ToString()).ToArray(),
            Frequency = result.Frequency.ToString(),
            Channels = result.Channels.Select(static channel => channel.ToString()).ToArray(),
            IsPaused = result.IsPaused,
            CreatedAtUtc = result.CreatedAtUtc,
            UpdatedAtUtc = result.UpdatedAtUtc,
            Version = result.Version,
        };
    }

    public static bool TryParseTargetType(string? value, out CollectionTargetType targetType)
    {
        return TryParse(value, out targetType);
    }

    private static bool TryParse<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value?.Trim(), true, out parsed) && Enum.IsDefined(parsed);
    }

    private static bool TryParseMany<TEnum>(
        IReadOnlyCollection<string> values,
        out TEnum[] parsed)
        where TEnum : struct, Enum
    {
        parsed = Array.Empty<TEnum>();
        if (values is null)
        {
            return false;
        }

        List<TEnum> result = new();
        foreach (string value in values)
        {
            if (!TryParse(value, out TEnum item))
            {
                return false;
            }

            result.Add(item);
        }

        parsed = result.Distinct().ToArray();
        return true;
    }
}
