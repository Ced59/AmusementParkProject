using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.DataSources.Contracts;
using AmusementPark.Application.Features.DataSources.Results;

namespace AmusementPark.Application.Features.DataSources.Commands;

public sealed record UpdateDataSourceSettingsCommand(string SourceKey, DataSourceSettingsResult Settings) : ICommand<ApplicationResult<DataSourceSettingsResult>>;

public sealed record StartDataSourceImportCommand(string SourceKey, DataSourceImportDescriptor ImportDescriptor) : ICommand<ApplicationResult<DataSourceSessionResult>>;

public sealed record ApplyDataSourceComparisonCommand(string SourceKey, DataSourceApplyRequest Request) : ICommand<ApplicationResult<DataSourceApplyResult>>;
