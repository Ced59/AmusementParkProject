using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.DataSources.Results;

namespace AmusementPark.Application.Features.DataSources.Queries;

public sealed record ListDataSourcesQuery : IQuery<ApplicationResult<IReadOnlyCollection<DataSourceStatusResult>>>;

public sealed record GetDataSourceStatusQuery(string SourceKey) : IQuery<ApplicationResult<DataSourceStatusResult>>;

public sealed record GetDataSourceSettingsQuery(string SourceKey) : IQuery<ApplicationResult<DataSourceSettingsResult>>;

public sealed record GetLatestDataSourceSessionQuery(string SourceKey) : IQuery<ApplicationResult<DataSourceSessionResult?>>;

public sealed record GetDataSourceSessionQuery(string SourceKey, string SessionId) : IQuery<ApplicationResult<DataSourceSessionResult>>;

public sealed record GetDataSourceComparisonResultsQuery(
    string SourceKey,
    string? SessionId,
    string? EntityType,
    string? ChangeType,
    bool? IsApplied,
    int Page,
    int PageSize) : IQuery<ApplicationResult<DataSourceComparisonPageResult>>;
