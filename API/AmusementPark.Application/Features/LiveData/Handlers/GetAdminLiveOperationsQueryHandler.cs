using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;

namespace AmusementPark.Application.Features.LiveData.Handlers;

public sealed class GetAdminLiveOperationsQueryHandler
    : IQueryHandler<GetAdminLiveOperationsQuery, ApplicationResult<LiveOperationsDashboardResult>>
{
    private readonly LiveOperationsDashboardReader reader;

    public GetAdminLiveOperationsQueryHandler(LiveOperationsDashboardReader reader)
    {
        this.reader = reader;
    }

    public Task<ApplicationResult<LiveOperationsDashboardResult>> HandleAsync(
        GetAdminLiveOperationsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.reader.ReadAsync(cancellationToken);
    }
}
