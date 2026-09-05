using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RetireRankingPublicationRequest(
    RankingScopeKey ScopeKey,
    RatingMethodologyVersion MethodologyVersion,
    long SourceRevision);
