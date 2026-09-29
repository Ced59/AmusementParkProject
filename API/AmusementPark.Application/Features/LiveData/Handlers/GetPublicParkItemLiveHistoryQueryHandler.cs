using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;

namespace AmusementPark.Application.Features.LiveData.Handlers;

public sealed class GetPublicParkItemLiveHistoryQueryHandler :
    IQueryHandler<GetPublicParkItemLiveHistoryQuery, ApplicationResult<PublicLiveHistoryResult>>
{
    private readonly PublicLiveHistoryReader reader;

    public GetPublicParkItemLiveHistoryQueryHandler(PublicLiveHistoryReader reader)
    {
        this.reader = reader;
    }

    public Task<ApplicationResult<PublicLiveHistoryResult>> HandleAsync(
        GetPublicParkItemLiveHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        return this.reader.ReadParkItemAsync(
            query.ParkItemId,
            query.From,
            query.To,
            query.Bucket,
            cancellationToken);
    }
}
