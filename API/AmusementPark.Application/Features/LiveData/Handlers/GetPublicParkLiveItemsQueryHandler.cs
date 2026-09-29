using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;

namespace AmusementPark.Application.Features.LiveData.Handlers;

public sealed class GetPublicParkLiveItemsQueryHandler :
    IQueryHandler<GetPublicParkLiveItemsQuery, ApplicationResult<PublicParkLiveItemsResult>>
{
    private readonly PublicLiveLatestReader reader;

    public GetPublicParkLiveItemsQueryHandler(PublicLiveLatestReader reader)
    {
        this.reader = reader;
    }

    public Task<ApplicationResult<PublicParkLiveItemsResult>> HandleAsync(
        GetPublicParkLiveItemsQuery query,
        CancellationToken cancellationToken = default)
    {
        return this.reader.ReadParkItemsAsync(query.ParkId, cancellationToken);
    }
}
