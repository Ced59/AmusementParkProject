using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.WebAPI.Contracts.Trips;

namespace AmusementPark.WebAPI.Mappers;

public static class TripPassportTransitionHttpMapper
{
    public static TripPassportTransitionDto ToHttp(
        this TripPassportTransitionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripPassportTransitionDto
        {
            Title = result.Title,
            DestinationToday = result.DestinationToday,
            Days = result.Days.Select(static day => new TripPassportTransitionDayDto
            {
                LocalDate = day.LocalDate,
                ParkId = day.ParkId,
                ParkName = day.ParkName,
                IsParkAvailable = day.IsParkAvailable,
                CanConfirm = day.CanConfirm,
                CanResume = day.CanResume,
                ExistingVisitId = day.ExistingVisitId,
                ExistingVisitStatus = day.ExistingVisitStatus?.ToString(),
                Attractions = day.Attractions.Select(static item =>
                    new TripPassportTransitionItemDto
                    {
                        ParkItemId = item.ParkItemId,
                        Name = item.Name,
                        MainImageId = item.MainImageId,
                        OwnPreference = item.OwnPreference.ToString(),
                        HistoricalConsistency = item.HistoricalConsistency.ToString(),
                    }).ToArray(),
            }).ToArray(),
        };
    }

    public static ConfirmTripPassportTransitionDto ToHttp(
        this ConfirmTripPassportTransitionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new ConfirmTripPassportTransitionDto
        {
            VisitId = result.VisitId,
            WasReplayed = result.WasReplayed,
            AddedRideCount = result.AddedRideCount,
        };
    }
}
