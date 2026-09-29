using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;

namespace AmusementPark.Application.Features.LiveData.Handlers;

public sealed class GetPublicParkItemLiveQueryHandler :
    IQueryHandler<GetPublicParkItemLiveQuery, ApplicationResult<PublicLiveTargetResult>>
{
    private readonly PublicLiveLatestReader reader;

    public GetPublicParkItemLiveQueryHandler(PublicLiveLatestReader reader)
    {
        this.reader = reader;
    }

    public Task<ApplicationResult<PublicLiveTargetResult>> HandleAsync(
        GetPublicParkItemLiveQuery query,
        CancellationToken cancellationToken = default)
    {
        return this.reader.ReadParkItemAsync(query.ParkItemId, cancellationToken);
    }
}
