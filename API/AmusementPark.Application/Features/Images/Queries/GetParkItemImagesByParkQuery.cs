using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Results;

namespace AmusementPark.Application.Features.Images.Queries;

public sealed record GetParkItemImagesByParkQuery(
    PagedQuery Paging,
    string ParkId,
    bool IncludeHidden = false) : IQuery<ApplicationResult<PagedResult<ParkItemImageResult>>>;
