using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.DataSources.Results;

namespace AmusementPark.Application.Features.DataSources.Queries;

public sealed record GetDataSourceComparisonResultsQuery(
    string SourceKey,
    string? SessionId,
    string? EntityType,
    string? ChangeType,
    bool? IsApplied,
    int Page,
    int PageSize) : IQuery<ApplicationResult<DataSourceComparisonPageResult>>;
