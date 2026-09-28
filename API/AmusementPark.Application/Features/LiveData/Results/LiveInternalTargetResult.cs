using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Results;

public sealed record LiveInternalTargetResult(
    LiveTargetType Type,
    string Id,
    string ParkId,
    string DisplayName,
    string ParkDisplayName,
    string CountryCode);
