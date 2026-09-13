using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.DataSources.Commands;
using AmusementPark.Application.Features.DataSources.Ports;
using AmusementPark.Application.Features.DataSources.Queries;
using AmusementPark.Application.Features.DataSources.Results;

namespace AmusementPark.Application.Features.DataSources.Handlers;

public sealed class StartDataSourceImportCommandHandler : ICommandHandler<StartDataSourceImportCommand, ApplicationResult<DataSourceSessionResult>>
{
    private readonly IDataSourceAdministrationService service;

    public StartDataSourceImportCommandHandler(IDataSourceAdministrationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<DataSourceSessionResult>> HandleAsync(StartDataSourceImportCommand command, CancellationToken cancellationToken)
    {
        return this.service.StartImportAsync(command.SourceKey, command.ImportDescriptor, cancellationToken);
    }
}
