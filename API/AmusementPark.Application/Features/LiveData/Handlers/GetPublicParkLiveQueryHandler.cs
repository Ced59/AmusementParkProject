using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;

namespace AmusementPark.Application.Features.LiveData.Handlers;

public sealed class GetPublicParkLiveQueryHandler :
    IQueryHandler<GetPublicParkLiveQuery, ApplicationResult<PublicLiveTargetResult>>
{
    private readonly PublicLiveLatestReader reader;

    public GetPublicParkLiveQueryHandler(PublicLiveLatestReader reader)
    {
        this.reader = reader;
    }

    public Task<ApplicationResult<PublicLiveTargetResult>> HandleAsync(
        GetPublicParkLiveQuery query,
        CancellationToken cancellationToken = default)
    {
        return this.reader.ReadParkAsync(query.ParkId, cancellationToken);
    }
}
