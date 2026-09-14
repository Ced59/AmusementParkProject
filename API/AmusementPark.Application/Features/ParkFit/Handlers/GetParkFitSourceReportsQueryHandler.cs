using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Handlers;

public sealed class GetParkFitSourceReportsQueryHandler
    : IQueryHandler<
        GetParkFitSourceReportsQuery,
        ApplicationResult<PagedResult<ParkFitSourceReportResult>>>
{
    private readonly IParkFitSourceReportRepository repository;

    public GetParkFitSourceReportsQueryHandler(IParkFitSourceReportRepository repository)
    {
        this.repository = repository;
    }

    public async Task<ApplicationResult<PagedResult<ParkFitSourceReportResult>>> HandleAsync(
        GetParkFitSourceReportsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        long skip = ((long)query.Criteria.Paging.Page - 1L) * query.Criteria.Paging.PageSize;
        if (query.Criteria.Paging.Page < 1
            || query.Criteria.Paging.PageSize is < 1 or > 100
            || skip > int.MaxValue
            || query.Criteria.Status.HasValue && !Enum.IsDefined(query.Criteria.Status.Value)
            || query.Criteria.Reason.HasValue && !Enum.IsDefined(query.Criteria.Reason.Value)
            || query.Criteria.ParkId?.Trim().Length > 200)
        {
            return ApplicationResult<PagedResult<ParkFitSourceReportResult>>.Failure(
                ParkFitOperationsApplicationErrors.InvalidSearch());
        }

        PagedResult<ParkFitSourceReport> page = await this.repository.SearchAsync(
            query.Criteria,
            cancellationToken);
        ParkFitSourceReportResult[] items = page.Items.Select(static report =>
            new ParkFitSourceReportResult(
                report.Id.Value,
                report.ParkId,
                report.ParkName,
                report.EvidenceKind,
                report.SourceUrl,
                report.SourceReference,
                report.Reason,
                report.Details,
                report.Status,
                report.SubmittedAtUtc,
                report.ReviewedAtUtc,
                report.DecisionNote,
                report.Revision)).ToArray();
        return ApplicationResult<PagedResult<ParkFitSourceReportResult>>.Success(
            new PagedResult<ParkFitSourceReportResult>(
                items,
                page.Page,
                page.PageSize,
                page.TotalItems));
    }
}
