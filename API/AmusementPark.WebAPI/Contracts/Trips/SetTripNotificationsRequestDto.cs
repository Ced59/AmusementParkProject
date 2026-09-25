namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed record SetTripNotificationsRequestDto(bool Enabled, long ExpectedVersion);
