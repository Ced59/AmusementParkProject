using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.DataSources.Commands;
using AmusementPark.Application.Features.DataSources.Ports;
using AmusementPark.Application.Features.DataSources.Queries;
using AmusementPark.Application.Features.DataSources.Results;

namespace AmusementPark.Application.Features.DataSources.Handlers;

public sealed class GetDataSourceSettingsQueryHandler : IQueryHandler<GetDataSourceSettingsQuery, ApplicationResult<DataSourceSettingsResult>>
{
    private readonly IDataSourceAdministrationService service;

    public GetDataSourceSettingsQueryHandler(IDataSourceAdministrationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<DataSourceSettingsResult>> HandleAsync(GetDataSourceSettingsQuery query, CancellationToken cancellationToken)
    {
        return this.service.GetSettingsAsync(query.SourceKey, cancellationToken);
    }
}
