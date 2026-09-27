using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.HistoricalExistenceReports.Ports;
using AmusementPark.Application.Features.HistoricalExistenceReports.Queries;
using AmusementPark.Application.Features.HistoricalExistenceReports.Results;
using AmusementPark.Application.Features.Passport;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.HistoricalExistenceReports.Handlers;

public sealed class ListOwnedHistoricalExistenceReportsQueryHandler
    : IQueryHandler<
        ListOwnedHistoricalExistenceReportsQuery,
        ApplicationResult<IReadOnlyCollection<HistoricalExistenceReportResult>>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IHistoricalExistenceReportRepository reportRepository;

    public ListOwnedHistoricalExistenceReportsQueryHandler(
        IUserVisitRepository visitRepository,
        IHistoricalExistenceReportRepository reportRepository)
    {
        this.visitRepository = visitRepository;
        this.reportRepository = reportRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<HistoricalExistenceReportResult>>>
        HandleAsync(
            ListOwnedHistoricalExistenceReportsQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        string userId = query.UserId?.Trim() ?? string.Empty;
        if (userId.Length == 0 || !VisitId.TryParse(query.VisitId, out VisitId visitId))
        {
            return Failure(PassportApplicationErrors.VisitNotFound());
        }

        Visit? visit = await this.visitRepository.GetOwnedAsync(
            visitId,
            userId,
            cancellationToken);
        if (visit is null)
        {
            return Failure(PassportApplicationErrors.VisitNotFound());
        }

        IReadOnlyCollection<HistoricalExistenceReport> reports =
            await this.reportRepository.ListOwnedByVisitAsync(
                userId,
                visit.Id,
                cancellationToken);
        HistoricalExistenceReportResult[] results = reports
            .Select(static report => report.ToResult())
            .ToArray();
        return ApplicationResult<IReadOnlyCollection<HistoricalExistenceReportResult>>.Success(
            results);
    }

    private static ApplicationResult<IReadOnlyCollection<HistoricalExistenceReportResult>> Failure(
        ApplicationError error)
    {
        return ApplicationResult<IReadOnlyCollection<HistoricalExistenceReportResult>>.Failure(
            error);
    }
}
