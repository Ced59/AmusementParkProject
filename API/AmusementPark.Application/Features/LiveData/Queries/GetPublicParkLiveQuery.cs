using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Results;

namespace AmusementPark.Application.Features.LiveData.Queries;

public sealed record GetPublicParkLiveQuery(string ParkId) :
    IQuery<ApplicationResult<PublicLiveTargetResult>>;
