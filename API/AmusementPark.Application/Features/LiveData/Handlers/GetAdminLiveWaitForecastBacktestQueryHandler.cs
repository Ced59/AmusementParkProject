using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;

namespace AmusementPark.Application.Features.LiveData.Handlers;

public sealed class GetAdminLiveWaitForecastBacktestQueryHandler
    : IQueryHandler<
        GetAdminLiveWaitForecastBacktestQuery,
        ApplicationResult<LiveWaitForecastBacktestResult>>
{
    private readonly LiveWaitForecastBacktestReader reader;

    public GetAdminLiveWaitForecastBacktestQueryHandler(LiveWaitForecastBacktestReader reader)
    {
        this.reader = reader;
    }

    public Task<ApplicationResult<LiveWaitForecastBacktestResult>> HandleAsync(
        GetAdminLiveWaitForecastBacktestQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.reader.ReadAsync(
            query.ParkItemId,
            query.From,
            query.To,
            cancellationToken);
    }
}
