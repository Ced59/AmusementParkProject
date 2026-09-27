using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.HistoricalExistenceReports.Commands;
using AmusementPark.Application.Features.HistoricalExistenceReports.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.HistoricalExistenceReports.Handlers;

public sealed class ReviewHistoricalExistenceReportCommandHandler
    : ICommandHandler<ReviewHistoricalExistenceReportCommand, ApplicationResult>
{
    private readonly IHistoricalExistenceReportRepository repository;
    private readonly TimeProvider timeProvider;

    public ReviewHistoricalExistenceReportCommandHandler(
        IHistoricalExistenceReportRepository repository,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult> HandleAsync(
        ReviewHistoricalExistenceReportCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!HistoricalExistenceReportId.TryParse(
                command.ReportId,
                out HistoricalExistenceReportId reportId)
            || string.IsNullOrWhiteSpace(command.ReviewerUserId)
            || command.ExpectedRevision < 0
            || command.Decision is not HistoricalExistenceReportStatus.AcceptedForResearch
                and not HistoricalExistenceReportStatus.Dismissed)
        {
            return ApplicationResult.Failure(
                HistoricalExistenceReportApplicationErrors.InvalidReport());
        }

        HistoricalExistenceReport? report = await this.repository.GetAsync(
            reportId,
            cancellationToken);
        if (report is null)
        {
            return ApplicationResult.Failure(
                HistoricalExistenceReportApplicationErrors.ReportNotFound());
        }

        if (report.Revision != command.ExpectedRevision
            || report.Status != HistoricalExistenceReportStatus.Pending)
        {
            return ApplicationResult.Failure(
                HistoricalExistenceReportApplicationErrors.Conflict());
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        DateTime reviewedAtUtc = nowUtc < report.SubmittedAtUtc
            ? report.SubmittedAtUtc
            : nowUtc;
        try
        {
            if (command.Decision == HistoricalExistenceReportStatus.AcceptedForResearch)
            {
                report.AcceptForResearch(
                    command.ReviewerUserId,
                    command.DecisionNote,
                    reviewedAtUtc);
            }
            else
            {
                report.Dismiss(
                    command.ReviewerUserId,
                    command.DecisionNote,
                    reviewedAtUtc);
            }
        }
        catch (ArgumentException)
        {
            return ApplicationResult.Failure(
                HistoricalExistenceReportApplicationErrors.InvalidReport());
        }
        catch (InvalidOperationException)
        {
            return ApplicationResult.Failure(
                HistoricalExistenceReportApplicationErrors.InvalidTransition());
        }

        HistoricalExistenceReportWriteOutcome outcome = await this.repository.ReplaceAsync(
            report,
            command.ExpectedRevision,
            cancellationToken);
        return outcome == HistoricalExistenceReportWriteOutcome.Success
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(HistoricalExistenceReportApplicationErrors.Conflict());
    }
}
