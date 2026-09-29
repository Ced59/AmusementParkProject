using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Results;

public sealed record PublicLiveSourceResult(
    string Id,
    string DisplayName,
    LiveDataSourceType Type,
    string AttributionText,
    string AttributionUrl);
