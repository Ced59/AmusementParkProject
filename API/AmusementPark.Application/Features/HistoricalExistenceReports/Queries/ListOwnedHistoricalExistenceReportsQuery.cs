using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.HistoricalExistenceReports.Queries;

public sealed record ListOwnedHistoricalExistenceReportsQuery(
    string UserId,
    string VisitId) : IQuery<ApplicationResult<IReadOnlyCollection<Results.HistoricalExistenceReportResult>>>;
