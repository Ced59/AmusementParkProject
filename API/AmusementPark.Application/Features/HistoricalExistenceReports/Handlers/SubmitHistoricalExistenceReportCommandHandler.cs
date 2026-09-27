using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.HistoricalExistenceReports.Commands;
using AmusementPark.Application.Features.HistoricalExistenceReports.Ports;
using AmusementPark.Application.Features.HistoricalExistenceReports.Results;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Passport;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.HistoricalExistenceReports.Handlers;

public sealed class SubmitHistoricalExistenceReportCommandHandler
    : ICommandHandler<
        SubmitHistoricalExistenceReportCommand,
        ApplicationResult<HistoricalExistenceReportResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IParkRepository parkRepository;
    private readonly IHistoricalExistenceReportRepository reportRepository;
    private readonly TimeProvider timeProvider;

    public SubmitHistoricalExistenceReportCommandHandler(
        IUserVisitRepository visitRepository,
        IParkRepository parkRepository,
        IHistoricalExistenceReportRepository reportRepository,
        TimeProvider? timeProvider = null)
    {
        this.visitRepository = visitRepository;
        this.parkRepository = parkRepository;
        this.reportRepository = reportRepository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<HistoricalExistenceReportResult>> HandleAsync(
        SubmitHistoricalExistenceReportCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        string userId = command.UserId?.Trim() ?? string.Empty;
        if (userId.Length == 0 || !VisitId.TryParse(command.VisitId, out VisitId visitId))
        {
            return Failure(HistoricalExistenceReportApplicationErrors.InvalidReport());
        }

        Visit? visit = await this.visitRepository.GetOwnedAsync(
            visitId,
            userId,
            cancellationToken);
        if (visit is null)
        {
            return Failure(PassportApplicationErrors.VisitNotFound());
        }

        Park? park = await this.parkRepository.GetByIdAsync(
            visit.ParkId,
            includeHidden: true,
            cancellationToken);
        if (park is null)
        {
            return Failure(PassportApplicationErrors.ParkNotFound());
        }

        HistoricalExistenceReport report;
        try
        {
            report = HistoricalExistenceReport.Create(
                HistoricalExistenceReportId.New(),
                visit.UserId,
                visit.Id,
                visit.ParkId,
                string.IsNullOrWhiteSpace(park.Name) ? visit.ParkId : park.Name,
                visit.Date,
                command.ClaimedName,
                command.SourceUrl,
                command.SourceReference,
                command.Details,
                this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (ArgumentException)
        {
            return Failure(HistoricalExistenceReportApplicationErrors.InvalidReport());
        }

        HistoricalExistenceReportWriteOutcome outcome = await this.reportRepository.CreateAsync(
            report,
            cancellationToken);
        return outcome == HistoricalExistenceReportWriteOutcome.Success
            ? ApplicationResult<HistoricalExistenceReportResult>.Success(report.ToResult())
            : Failure(HistoricalExistenceReportApplicationErrors.Conflict());
    }

    private static ApplicationResult<HistoricalExistenceReportResult> Failure(
        ApplicationError error)
    {
        return ApplicationResult<HistoricalExistenceReportResult>.Failure(error);
    }
}
