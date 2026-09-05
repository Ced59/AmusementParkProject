using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Ratings.Services;

internal sealed record ParkItemRankingMetadata(ParkItem Item, string ParkName);
