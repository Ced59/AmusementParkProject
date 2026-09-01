using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RatingRankingSourceRevision(
    RankingScopeKey ScopeKey,
    long Revision,
    DateTime UpdatedAtUtc);
