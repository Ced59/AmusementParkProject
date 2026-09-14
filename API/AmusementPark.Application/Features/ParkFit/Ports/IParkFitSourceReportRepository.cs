using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Ports;

public interface IParkFitSourceReportRepository
{
    Task<ParkFitSourceReport?> GetAsync(
        ParkFitSourceReportId reportId,
        CancellationToken cancellationToken);

    Task<PagedResult<ParkFitSourceReport>> SearchAsync(
        ParkFitSourceReportSearchCriteria criteria,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, int>> CountPendingByParkIdsAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken);

    Task<ParkFitSourceReportWriteOutcome> CreateAsync(
        ParkFitSourceReport report,
        CancellationToken cancellationToken);

    Task<ParkFitSourceReportWriteOutcome> ReplaceAsync(
        ParkFitSourceReport report,
        long expectedRevision,
        CancellationToken cancellationToken);
}
