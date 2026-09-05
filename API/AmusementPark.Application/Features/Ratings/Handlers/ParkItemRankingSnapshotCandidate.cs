using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Handlers;

internal sealed record ParkItemRankingSnapshotCandidate(
    ParkItemRatingRankingResult Ranking,
    RankingEvidence? Evidence);
