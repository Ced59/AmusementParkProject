using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.DataSources.Commands;
using AmusementPark.Application.Features.DataSources.Ports;
using AmusementPark.Application.Features.DataSources.Queries;
using AmusementPark.Application.Features.DataSources.Results;

namespace AmusementPark.Application.Features.DataSources.Handlers;

public sealed class GetDataSourceComparisonResultsQueryHandler : IQueryHandler<GetDataSourceComparisonResultsQuery, ApplicationResult<DataSourceComparisonPageResult>>
{
    private readonly IDataSourceAdministrationService service;

    public GetDataSourceComparisonResultsQueryHandler(IDataSourceAdministrationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<DataSourceComparisonPageResult>> HandleAsync(GetDataSourceComparisonResultsQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 0 || query.PageSize <= 0)
        {
            return Task.FromResult(ApplicationResult<DataSourceComparisonPageResult>.Failure(ApplicationErrors.InvalidPagination()));
        }

        return this.service.GetComparisonResultsAsync(
            query.SourceKey,
            query.SessionId,
            query.EntityType,
            query.ChangeType,
            query.IsApplied,
            query.Page,
            query.PageSize,
            cancellationToken);
    }
}
