using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public sealed record RatingEvidenceTarget(
    RatingTargetType TargetType,
    string TargetId,
    string ParkId);
