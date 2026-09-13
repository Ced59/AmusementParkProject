using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.DataSources.Results;

namespace AmusementPark.Application.Features.DataSources.Queries;

public sealed record ListDataSourcesQuery : IQuery<ApplicationResult<IReadOnlyCollection<DataSourceStatusResult>>>;
