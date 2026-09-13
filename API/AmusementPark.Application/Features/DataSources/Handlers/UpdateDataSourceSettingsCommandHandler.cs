using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.DataSources.Commands;
using AmusementPark.Application.Features.DataSources.Ports;
using AmusementPark.Application.Features.DataSources.Queries;
using AmusementPark.Application.Features.DataSources.Results;

namespace AmusementPark.Application.Features.DataSources.Handlers;

public sealed class UpdateDataSourceSettingsCommandHandler : ICommandHandler<UpdateDataSourceSettingsCommand, ApplicationResult<DataSourceSettingsResult>>
{
    private readonly IDataSourceAdministrationService service;

    public UpdateDataSourceSettingsCommandHandler(IDataSourceAdministrationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<DataSourceSettingsResult>> HandleAsync(UpdateDataSourceSettingsCommand command, CancellationToken cancellationToken)
    {
        return this.service.UpdateSettingsAsync(command.SourceKey, command.Settings, cancellationToken);
    }
}
