using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class GetShareModerationReportsQueryHandler
    : IQueryHandler<GetShareModerationReportsQuery,
        ApplicationResult<PagedResult<ShareModerationReportResult>>>
{
    private readonly IShareModerationReportRepository repository;

    public GetShareModerationReportsQueryHandler(IShareModerationReportRepository repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApplicationResult<PagedResult<ShareModerationReportResult>>> HandleAsync(
        GetShareModerationReportsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        long skip = ((long)query.Criteria.Paging.Page - 1L)
            * query.Criteria.Paging.PageSize;
        if (query.Criteria.Paging.Page < 1
            || query.Criteria.Paging.PageSize is < 1 or > 100
            || skip > int.MaxValue
            || query.Criteria.Status.HasValue && !Enum.IsDefined(query.Criteria.Status.Value)
            || query.Criteria.TargetType.HasValue && !Enum.IsDefined(query.Criteria.TargetType.Value)
            || query.Criteria.Reason.HasValue && !Enum.IsDefined(query.Criteria.Reason.Value))
        {
            return ApplicationResult<PagedResult<ShareModerationReportResult>>.Failure(
                SharingApplicationErrors.InvalidModerationSearch());
        }

        PagedResult<ShareModerationReport> page = await this.repository.SearchAsync(
            query.Criteria,
            cancellationToken);
        ShareModerationReportResult[] items = page.Items.Select(static report =>
            new ShareModerationReportResult(
                report.Id.Value,
                report.TargetType,
                report.Reason,
                report.Details,
                report.Status,
                report.SubmittedAtUtc,
                report.ReviewedAtUtc,
                report.DecisionNote)).ToArray();
        return ApplicationResult<PagedResult<ShareModerationReportResult>>.Success(
            new PagedResult<ShareModerationReportResult>(
                items,
                page.Page,
                page.PageSize,
                page.TotalItems));
    }
}
