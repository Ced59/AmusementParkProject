using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Handlers;

public sealed class ReviewParkFitSourceReportCommandHandler
    : ICommandHandler<ReviewParkFitSourceReportCommand, ApplicationResult>
{
    private readonly IParkFitSourceReportRepository repository;
    private readonly TimeProvider timeProvider;

    public ReviewParkFitSourceReportCommandHandler(
        IParkFitSourceReportRepository repository,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult> HandleAsync(
        ReviewParkFitSourceReportCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!ParkFitSourceReportId.TryParse(command.ReportId, out ParkFitSourceReportId reportId)
            || string.IsNullOrWhiteSpace(command.ReviewerUserId)
            || command.ExpectedRevision < 0
            || command.Decision is not ParkFitSourceReportStatus.Resolved
                and not ParkFitSourceReportStatus.Dismissed)
        {
            return ApplicationResult.Failure(ParkFitOperationsApplicationErrors.InvalidReport());
        }

        ParkFitSourceReport? report = await this.repository.GetAsync(reportId, cancellationToken);
        if (report is null)
        {
            return ApplicationResult.Failure(ParkFitOperationsApplicationErrors.ReportNotFound());
        }

        if (report.Revision != command.ExpectedRevision
            || report.Status != ParkFitSourceReportStatus.Pending)
        {
            return ApplicationResult.Failure(ParkFitOperationsApplicationErrors.Conflict());
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        DateTime reviewedAtUtc = nowUtc < report.SubmittedAtUtc ? report.SubmittedAtUtc : nowUtc;
        try
        {
            if (command.Decision == ParkFitSourceReportStatus.Resolved)
            {
                report.Resolve(command.ReviewerUserId, command.DecisionNote, reviewedAtUtc);
            }
            else
            {
                report.Dismiss(command.ReviewerUserId, command.DecisionNote, reviewedAtUtc);
            }
        }
        catch (ArgumentException)
        {
            return ApplicationResult.Failure(ParkFitOperationsApplicationErrors.InvalidReport());
        }
        catch (InvalidOperationException)
        {
            return ApplicationResult.Failure(ParkFitOperationsApplicationErrors.InvalidTransition());
        }

        ParkFitSourceReportWriteOutcome outcome = await this.repository.ReplaceAsync(
            report,
            command.ExpectedRevision,
            cancellationToken);
        return outcome == ParkFitSourceReportWriteOutcome.Success
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(ParkFitOperationsApplicationErrors.Conflict());
    }
}
