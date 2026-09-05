using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RatingPublishedRank(
    int Rank,
    RatingMethodologyVersion MethodologyVersion,
    DateTime? GeneratedAtUtc);
