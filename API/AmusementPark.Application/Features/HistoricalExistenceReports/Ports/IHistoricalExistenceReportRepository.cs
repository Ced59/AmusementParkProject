using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.HistoricalExistenceReports.Models;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.HistoricalExistenceReports.Ports;

public interface IHistoricalExistenceReportRepository
{
    Task<HistoricalExistenceReport?> GetAsync(
        HistoricalExistenceReportId reportId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HistoricalExistenceReport>> ListOwnedByVisitAsync(
        string ownerUserId,
        VisitId visitId,
        CancellationToken cancellationToken);

    Task<PagedResult<HistoricalExistenceReport>> SearchAsync(
        HistoricalExistenceReportSearchCriteria criteria,
        CancellationToken cancellationToken);

    Task<HistoricalExistenceReportWriteOutcome> CreateAsync(
        HistoricalExistenceReport report,
        CancellationToken cancellationToken);

    Task<HistoricalExistenceReportWriteOutcome> ReplaceAsync(
        HistoricalExistenceReport report,
        long expectedRevision,
        CancellationToken cancellationToken);
}
