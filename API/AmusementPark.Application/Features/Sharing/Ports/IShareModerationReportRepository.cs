using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IShareModerationReportRepository
{
    Task<ShareModerationReport?> GetAsync(
        ShareModerationReportId reportId,
        CancellationToken cancellationToken);

    Task<PagedResult<ShareModerationReport>> SearchAsync(
        ShareModerationReportSearchCriteria criteria,
        CancellationToken cancellationToken);

    Task<ShareModerationReportWriteOutcome> CreateAsync(
        ShareModerationReport report,
        CancellationToken cancellationToken);

    Task<ShareModerationReportWriteOutcome> ReplaceAsync(
        ShareModerationReport report,
        long expectedVersion,
        CancellationToken cancellationToken);
}
