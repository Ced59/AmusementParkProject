namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record PassportProfileShareSelectedParkSnapshot(
    string ParkId,
    string Name,
    string? CountryCode);
