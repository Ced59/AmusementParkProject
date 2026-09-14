using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Results;

namespace AmusementPark.Application.Features.ParkFit.Queries;

public sealed record GetParkFitSourceReportsQuery(
    ParkFitSourceReportSearchCriteria Criteria)
    : IQuery<ApplicationResult<PagedResult<ParkFitSourceReportResult>>>;
