using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Results;

public sealed record LiveExternalTargetResult(
    LiveTargetType Type,
    string Id,
    string? ParentId,
    string DisplayName,
    string? ParentDisplayName,
    string CountryCode);
