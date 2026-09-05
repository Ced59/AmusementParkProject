using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Ratings.Ports;

public sealed record PublicParkItemEvidenceFact(
    string ParkId,
    string TargetId,
    ParkItemCategory Category);
