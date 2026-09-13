using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.DataSources.Commands;
using AmusementPark.Application.Features.DataSources.Ports;
using AmusementPark.Application.Features.DataSources.Queries;
using AmusementPark.Application.Features.DataSources.Results;

namespace AmusementPark.Application.Features.DataSources.Handlers;

public sealed class GetLatestDataSourceSessionQueryHandler : IQueryHandler<GetLatestDataSourceSessionQuery, ApplicationResult<DataSourceSessionResult?>>
{
    private readonly IDataSourceAdministrationService service;

    public GetLatestDataSourceSessionQueryHandler(IDataSourceAdministrationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<DataSourceSessionResult?>> HandleAsync(GetLatestDataSourceSessionQuery query, CancellationToken cancellationToken)
    {
        return this.service.GetLatestSessionAsync(query.SourceKey, cancellationToken);
    }
}
