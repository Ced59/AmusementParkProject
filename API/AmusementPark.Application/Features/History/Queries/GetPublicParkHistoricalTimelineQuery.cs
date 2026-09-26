using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Results;

namespace AmusementPark.Application.Features.History.Queries;

public sealed record GetPublicParkHistoricalTimelineQuery(
    string ParkId,
    int Page = 1,
    int PageSize = GetPublicParkHistoricalTimelineQuery.DefaultPageSize) :
    IQuery<ApplicationResult<PublicParkHistoricalTimelineResult>>
{
    public const int DefaultPageSize = 25;

    public const int MaximumPageSize = 50;
}
