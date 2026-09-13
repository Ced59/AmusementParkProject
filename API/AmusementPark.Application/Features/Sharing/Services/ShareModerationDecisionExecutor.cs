using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class ShareModerationDecisionExecutor
{
    private readonly IShareModerationReportRepository reportRepository;
    private readonly ShareModerationPublicationTargetExecutor publicationTargetExecutor;
    private readonly ShareModerationComparisonTargetExecutor comparisonTargetExecutor;

    public ShareModerationDecisionExecutor(
        IShareModerationReportRepository reportRepository,
        ShareModerationPublicationTargetExecutor publicationTargetExecutor,
        ShareModerationComparisonTargetExecutor comparisonTargetExecutor)
    {
        this.reportRepository = reportRepository
            ?? throw new ArgumentNullException(nameof(reportRepository));
        this.publicationTargetExecutor = publicationTargetExecutor
            ?? throw new ArgumentNullException(nameof(publicationTargetExecutor));
        this.comparisonTargetExecutor = comparisonTargetExecutor
            ?? throw new ArgumentNullException(nameof(comparisonTargetExecutor));
    }

    public async Task<ShareModerationDecisionExecutionOutcome> ExecuteAsync(
        ShareModerationDecisionJobPayload payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (!ShareModerationReportId.TryParse(
                payload.ReportId,
                out ShareModerationReportId reportId))
        {
            return ShareModerationDecisionExecutionOutcome.InvalidTransition;
        }

        ShareModerationReport? report = await this.reportRepository.GetAsync(
            reportId,
            cancellationToken);
        if (report is null)
        {
            return ShareModerationDecisionExecutionOutcome.ReportNotFound;
        }

        return payload.Decision switch
        {
            ShareModerationDecision.Dismiss =>
                await this.DismissAsync(report, payload, cancellationToken),
            ShareModerationDecision.Suspend =>
                await this.SuspendAsync(report, payload, cancellationToken),
            ShareModerationDecision.Restore =>
                await this.RestoreAsync(report, payload, cancellationToken),
            _ => ShareModerationDecisionExecutionOutcome.InvalidTransition,
        };
    }

    private async Task<ShareModerationDecisionExecutionOutcome> DismissAsync(
        ShareModerationReport report,
        ShareModerationDecisionJobPayload payload,
        CancellationToken cancellationToken)
    {
        if (report.Status == ShareModerationReportStatus.Dismissed)
        {
            return ShareModerationDecisionExecutionOutcome.Succeeded;
        }

        if (report.Status != ShareModerationReportStatus.Pending)
        {
            return ShareModerationDecisionExecutionOutcome.InvalidTransition;
        }

        long expectedVersion = report.Version;
        try
        {
            report.Dismiss(
                payload.ReviewerUserId,
                payload.Note,
                payload.RequestedAtUtc);
        }
        catch (ShareModerationValidationException)
        {
            return ShareModerationDecisionExecutionOutcome.InvalidTransition;
        }

        ShareModerationReportWriteOutcome outcome =
            await this.reportRepository.ReplaceAsync(
                report,
                expectedVersion,
                cancellationToken);
        return outcome == ShareModerationReportWriteOutcome.Success
            ? ShareModerationDecisionExecutionOutcome.Succeeded
            : ShareModerationDecisionExecutionOutcome.RetryableConflict;
    }

    private async Task<ShareModerationDecisionExecutionOutcome> SuspendAsync(
        ShareModerationReport report,
        ShareModerationDecisionJobPayload payload,
        CancellationToken cancellationToken)
    {
        if (report.Status is not ShareModerationReportStatus.Pending
            and not ShareModerationReportStatus.PublicationSuspended)
        {
            ShareModerationDecisionExecutionOutcome compensationOutcome =
                await this.RestoreTargetAsync(report, cancellationToken);
            return compensationOutcome == ShareModerationDecisionExecutionOutcome.RetryableConflict
                ? compensationOutcome
                : ShareModerationDecisionExecutionOutcome.InvalidTransition;
        }

        ShareModerationDecisionExecutionOutcome targetOutcome =
            await this.SuspendTargetAsync(report, cancellationToken);
        if (targetOutcome != ShareModerationDecisionExecutionOutcome.Succeeded
            || report.Status == ShareModerationReportStatus.PublicationSuspended)
        {
            return targetOutcome;
        }

        long expectedVersion = report.Version;
        try
        {
            report.MarkPublicationSuspended(
                payload.ReviewerUserId,
                payload.Note,
                payload.RequestedAtUtc);
        }
        catch (ShareModerationValidationException)
        {
            return ShareModerationDecisionExecutionOutcome.InvalidTransition;
        }

        ShareModerationReportWriteOutcome outcome = await this.reportRepository.ReplaceAsync(
            report,
            expectedVersion,
            cancellationToken);
        return outcome == ShareModerationReportWriteOutcome.Success
            ? ShareModerationDecisionExecutionOutcome.Succeeded
            : ShareModerationDecisionExecutionOutcome.RetryableConflict;
    }

    private async Task<ShareModerationDecisionExecutionOutcome> RestoreAsync(
        ShareModerationReport report,
        ShareModerationDecisionJobPayload payload,
        CancellationToken cancellationToken)
    {
        if (report.Status is not ShareModerationReportStatus.PublicationSuspended
            and not ShareModerationReportStatus.PublicationRestored)
        {
            return ShareModerationDecisionExecutionOutcome.InvalidTransition;
        }

        if (report.Status == ShareModerationReportStatus.PublicationSuspended)
        {
            long expectedVersion = report.Version;
            try
            {
                report.MarkPublicationRestored(
                    payload.ReviewerUserId,
                    payload.Note,
                    payload.RequestedAtUtc);
            }
            catch (ShareModerationValidationException)
            {
                return ShareModerationDecisionExecutionOutcome.InvalidTransition;
            }

            ShareModerationReportWriteOutcome reportOutcome =
                await this.reportRepository.ReplaceAsync(
                    report,
                    expectedVersion,
                    cancellationToken);
            if (reportOutcome != ShareModerationReportWriteOutcome.Success)
            {
                return ShareModerationDecisionExecutionOutcome.RetryableConflict;
            }
        }

        return await this.RestoreTargetAsync(report, cancellationToken);
    }

    private Task<ShareModerationDecisionExecutionOutcome> SuspendTargetAsync(
        ShareModerationReport report,
        CancellationToken cancellationToken)
    {
        return report.TargetType == ShareModerationTargetType.ProfileComparison
            ? this.comparisonTargetExecutor.SuspendAsync(report, cancellationToken)
            : this.publicationTargetExecutor.SuspendAsync(report, cancellationToken);
    }

    private Task<ShareModerationDecisionExecutionOutcome> RestoreTargetAsync(
        ShareModerationReport report,
        CancellationToken cancellationToken)
    {
        return report.TargetType == ShareModerationTargetType.ProfileComparison
            ? this.comparisonTargetExecutor.RestoreAsync(report, cancellationToken)
            : this.publicationTargetExecutor.RestoreAsync(report, cancellationToken);
    }
}
