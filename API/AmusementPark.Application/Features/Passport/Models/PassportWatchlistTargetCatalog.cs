namespace AmusementPark.Application.Features.Passport.Models;

public sealed record PassportWatchlistTargetCatalog(
    IReadOnlyDictionary<string, PassportWatchlistTargetSnapshot> ParkTargets,
    IReadOnlyDictionary<string, PassportWatchlistTargetSnapshot> ParkItemTargets);
