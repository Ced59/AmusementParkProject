using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Results;

namespace AmusementPark.Application.Features.History.Queries;

public sealed record GetPublicParkHistoricalSnapshotQuery(
    string ParkId,
    int Year,
    int? Month,
    int? Day) : IQuery<ApplicationResult<PublicParkHistoricalSnapshotResult>>;
