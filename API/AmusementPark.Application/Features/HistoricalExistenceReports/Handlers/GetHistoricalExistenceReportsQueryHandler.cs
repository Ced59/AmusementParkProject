using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.HistoricalExistenceReports.Ports;
using AmusementPark.Application.Features.HistoricalExistenceReports.Queries;
using AmusementPark.Application.Features.HistoricalExistenceReports.Results;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.HistoricalExistenceReports.Handlers;

public sealed class GetHistoricalExistenceReportsQueryHandler
    : IQueryHandler<
        GetHistoricalExistenceReportsQuery,
        ApplicationResult<PagedResult<HistoricalExistenceReportResult>>>
{
    private readonly IHistoricalExistenceReportRepository repository;

    public GetHistoricalExistenceReportsQueryHandler(
        IHistoricalExistenceReportRepository repository)
    {
        this.repository = repository;
    }

    public async Task<ApplicationResult<PagedResult<HistoricalExistenceReportResult>>> HandleAsync(
        GetHistoricalExistenceReportsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        long skip = ((long)query.Criteria.Paging.Page - 1L)
            * query.Criteria.Paging.PageSize;
        if (query.Criteria.Paging.Page < 1
            || query.Criteria.Paging.PageSize is < 1 or > 100
            || skip > int.MaxValue
            || query.Criteria.Status.HasValue && !Enum.IsDefined(query.Criteria.Status.Value)
            || query.Criteria.ParkId?.Trim().Length > 200)
        {
            return Failure(HistoricalExistenceReportApplicationErrors.InvalidSearch());
        }

        PagedResult<HistoricalExistenceReport> page = await this.repository.SearchAsync(
            query.Criteria,
            cancellationToken);
        HistoricalExistenceReportResult[] items = page.Items
            .Select(static report => report.ToResult())
            .ToArray();
        return ApplicationResult<PagedResult<HistoricalExistenceReportResult>>.Success(
            new PagedResult<HistoricalExistenceReportResult>(
                items,
                page.Page,
                page.PageSize,
                page.TotalItems));
    }

    private static ApplicationResult<PagedResult<HistoricalExistenceReportResult>> Failure(
        ApplicationError error)
    {
        return ApplicationResult<PagedResult<HistoricalExistenceReportResult>>.Failure(error);
    }
}
