using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.HistoricalExistenceReports.Queries;

public sealed record GetHistoricalExistenceReportsQuery(
    Models.HistoricalExistenceReportSearchCriteria Criteria)
    : IQuery<ApplicationResult<PagedResult<Results.HistoricalExistenceReportResult>>>;
