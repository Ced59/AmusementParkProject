using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Models;

public sealed record LiveDataSourcePresentation(
    LiveDataSource Source,
    int Priority,
    string AttributionText,
    string AttributionUrl);
