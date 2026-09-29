using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;

namespace AmusementPark.Application.Features.LiveData.Handlers;

public sealed class GetPublicParkItemLiveForecastQueryHandler :
    IQueryHandler<GetPublicParkItemLiveForecastQuery, ApplicationResult<PublicLiveForecastResult>>
{
    private readonly PublicLiveForecastReader reader;

    public GetPublicParkItemLiveForecastQueryHandler(PublicLiveForecastReader reader)
    {
        this.reader = reader;
    }

    public Task<ApplicationResult<PublicLiveForecastResult>> HandleAsync(
        GetPublicParkItemLiveForecastQuery query,
        CancellationToken cancellationToken = default)
    {
        return this.reader.ReadParkItemAsync(query.ParkItemId, cancellationToken);
    }
}
