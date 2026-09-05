using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Contracts;

namespace AmusementPark.Application.Features.Parks.Queries;

public sealed record GetParkOfficialMapFileQuery(
    string ParkId,
    string OfficialMapId,
    bool IncludeHidden = false) : IQuery<ApplicationResult<ParkOfficialMapBinary>>;
