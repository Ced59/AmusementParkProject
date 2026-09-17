using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record PassportWatchlistTargetSnapshot(
    CollectionTargetType TargetType,
    CollectionTargetStatus Status,
    string? Name,
    string? ParkName);
