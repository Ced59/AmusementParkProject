using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.WebAPI.Contracts.Trips;

namespace AmusementPark.WebAPI.Mappers;

public static class TripPreferenceHttpMapper
{
    public static TripPreferenceBoardDto ToHttp(this TripPreferenceBoardResult result)
    {
        return new TripPreferenceBoardDto
        {
            TripPlanId = result.TripPlanId,
            TripTitle = result.TripTitle,
            PlanVersion = result.PlanVersion,
            CanVote = result.CanVote,
            Items = result.Items.Select(static item => new TripItemPreferenceDto
            {
                ParkId = item.ParkId,
                ParkName = item.ParkName,
                ParkItemId = item.ParkItemId,
                ParkItemName = item.ParkItemName,
                MainImageId = item.MainImageId,
                Level = item.Level.ToString(),
                Reason = item.Reason?.ToString(),
                Version = item.Version,
            }).ToList(),
        };
    }

    public static bool TryToApplication(
        this SetTripItemPreferenceRequestDto request,
        string parkItemId,
        out TripItemPreferenceInput? input)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!TryParse(request.Level, out TripItemPreferenceLevel level)
            || !TryParseOptional(request.Reason, out TripItemPreferenceReason? reason))
        {
            input = null;
            return false;
        }

        input = new TripItemPreferenceInput(
            parkItemId,
            request.ExpectedPreferenceVersion,
            level,
            reason);
        return true;
    }

    public static bool TryToApplication(
        this BulkTripItemPreferenceRequestDto request,
        out TripItemPreferenceInput? input)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!TryParse(request.Level, out TripItemPreferenceLevel level)
            || !TryParseOptional(request.Reason, out TripItemPreferenceReason? reason))
        {
            input = null;
            return false;
        }

        input = new TripItemPreferenceInput(
            request.ParkItemId,
            request.ExpectedPreferenceVersion,
            level,
            reason);
        return true;
    }

    private static bool TryParse<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value?.Trim(), true, out parsed) && Enum.IsDefined(parsed);
    }

    private static bool TryParseOptional<TEnum>(string? value, out TEnum? parsed)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsed = null;
            return true;
        }

        if (TryParse(value, out TEnum result))
        {
            parsed = result;
            return true;
        }

        parsed = null;
        return false;
    }
}
