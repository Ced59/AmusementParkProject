using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.DataSources.Commands;
using AmusementPark.Application.Features.DataSources.Ports;
using AmusementPark.Application.Features.DataSources.Queries;
using AmusementPark.Application.Features.DataSources.Results;

namespace AmusementPark.Application.Features.DataSources.Handlers;

public sealed class GetDataSourceStatusQueryHandler : IQueryHandler<GetDataSourceStatusQuery, ApplicationResult<DataSourceStatusResult>>
{
    private readonly IDataSourceAdministrationService service;

    public GetDataSourceStatusQueryHandler(IDataSourceAdministrationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<DataSourceStatusResult>> HandleAsync(GetDataSourceStatusQuery query, CancellationToken cancellationToken)
    {
        return this.service.GetStatusAsync(query.SourceKey, cancellationToken);
    }
}
