using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.DataSources.Contracts;
using AmusementPark.Application.Features.DataSources.Results;

namespace AmusementPark.Application.Features.DataSources.Commands;

public sealed record ApplyDataSourceComparisonCommand(string SourceKey, DataSourceApplyRequest Request) : ICommand<ApplicationResult<DataSourceApplyResult>>;
