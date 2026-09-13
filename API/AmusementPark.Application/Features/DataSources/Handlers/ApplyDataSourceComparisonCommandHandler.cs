using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.DataSources.Commands;
using AmusementPark.Application.Features.DataSources.Ports;
using AmusementPark.Application.Features.DataSources.Queries;
using AmusementPark.Application.Features.DataSources.Results;

namespace AmusementPark.Application.Features.DataSources.Handlers;

public sealed class ApplyDataSourceComparisonCommandHandler : ICommandHandler<ApplyDataSourceComparisonCommand, ApplicationResult<DataSourceApplyResult>>
{
    private readonly IDataSourceAdministrationService service;

    public ApplyDataSourceComparisonCommandHandler(IDataSourceAdministrationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<DataSourceApplyResult>> HandleAsync(ApplyDataSourceComparisonCommand command, CancellationToken cancellationToken)
    {
        return this.service.ApplyComparisonAsync(command.SourceKey, command.Request, cancellationToken);
    }
}
