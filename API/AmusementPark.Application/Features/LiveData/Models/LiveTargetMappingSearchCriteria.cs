using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Models;

public sealed record LiveTargetMappingSearchCriteria(
    int Page,
    int PageSize,
    string? SourceId,
    LiveMappingStatus? Status,
    LiveTargetType? TargetType,
    string? Search);
