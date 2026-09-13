using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class ShareModerationService
{
    private readonly IShareModerationReportRepository reportRepository;
    private readonly ISharePublicationRepository publicationRepository;
    private readonly IProfileComparisonRepository comparisonRepository;
    private readonly ShareModerationDecisionScheduler decisionScheduler;
    private readonly ShareModerationDecisionExecutor decisionExecutor;
    private readonly TimeProvider timeProvider;

    public ShareModerationService(
        IShareModerationReportRepository reportRepository,
        ISharePublicationRepository publicationRepository,
        IProfileComparisonRepository comparisonRepository,
        ShareModerationDecisionScheduler decisionScheduler,
        ShareModerationDecisionExecutor decisionExecutor,
        TimeProvider? timeProvider = null)
    {
        this.reportRepository = reportRepository
            ?? throw new ArgumentNullException(nameof(reportRepository));
        this.publicationRepository = publicationRepository
            ?? throw new ArgumentNullException(nameof(publicationRepository));
        this.comparisonRepository = comparisonRepository
            ?? throw new ArgumentNullException(nameof(comparisonRepository));
        this.decisionScheduler = decisionScheduler
            ?? throw new ArgumentNullException(nameof(decisionScheduler));
        this.decisionExecutor = decisionExecutor
            ?? throw new ArgumentNullException(nameof(decisionExecutor));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult> SubmitAsync(
        SubmitShareModerationReportCommand command,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(command.TargetType)
            || !Enum.IsDefined(command.Reason)
            || !ShareToken.TryParse(command.ShareId, out ShareToken shareToken))
        {
            return ApplicationResult.Failure(
                SharingApplicationErrors.InvalidModerationReport());
        }

        string? targetRecordId = await this.ResolvePublicTargetIdAsync(
            command.TargetType,
            shareToken,
            cancellationToken);
        if (targetRecordId is null)
        {
            return ApplicationResult.Failure(
                SharingApplicationErrors.ModerationTargetNotFound());
        }

        ShareModerationReport report;
        try
        {
            report = ShareModerationReport.Create(
                ShareModerationReportId.New(),
                command.TargetType,
                targetRecordId,
                command.Reason,
                command.Details,
                this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (ShareModerationValidationException)
        {
            return ApplicationResult.Failure(
                SharingApplicationErrors.InvalidModerationReport());
        }

        ShareModerationReportWriteOutcome outcome =
            await this.reportRepository.CreateAsync(report, cancellationToken);
        return outcome == ShareModerationReportWriteOutcome.Success
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(SharingApplicationErrors.ModerationConflict());
    }

    public async Task<ApplicationResult> ReviewAsync(
        ReviewShareModerationReportCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ReviewerUserId)
            || !Enum.IsDefined(command.Decision)
            || command.Note?.Trim().Length > ShareModerationReport.MaximumDecisionNoteLength
            || !PublicShareTextSafetyPolicy.IsSafePlainText(command.Note)
            || !ShareModerationReportId.TryParse(command.ReportId, out ShareModerationReportId reportId))
        {
            return ApplicationResult.Failure(
                SharingApplicationErrors.InvalidModerationReport());
        }

        ShareModerationReport? report = await this.reportRepository.GetAsync(
            reportId,
            cancellationToken);
        if (report is null)
        {
            return ApplicationResult.Failure(
                SharingApplicationErrors.ModerationReportNotFound());
        }

        DateTime requestedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        if (command.Decision == ShareModerationDecision.Dismiss)
        {
            long expectedReportVersion = report.Version;
            try
            {
                report.Dismiss(command.ReviewerUserId, command.Note, requestedAtUtc);
            }
            catch (ShareModerationValidationException)
            {
                return ApplicationResult.Failure(
                    SharingApplicationErrors.InvalidModerationTransition());
            }

            ShareModerationReportWriteOutcome dismissOutcome =
                await this.reportRepository.ReplaceAsync(
                    report,
                    expectedReportVersion,
                    cancellationToken);
            return dismissOutcome == ShareModerationReportWriteOutcome.Success
                ? ApplicationResult.Success()
                : ApplicationResult.Failure(SharingApplicationErrors.ModerationConflict());
        }

        bool isExpectedTransition = command.Decision switch
        {
            ShareModerationDecision.Suspend =>
                report.Status == ShareModerationReportStatus.Pending,
            ShareModerationDecision.Restore =>
                report.Status == ShareModerationReportStatus.PublicationSuspended,
            _ => false,
        };
        if (!isExpectedTransition)
        {
            return ApplicationResult.Failure(
                SharingApplicationErrors.InvalidModerationTransition());
        }

        ShareModerationDecisionJobPayload payload = new ShareModerationDecisionJobPayload(
            report.Id.Value,
            command.Decision,
            command.ReviewerUserId,
            command.Note,
            requestedAtUtc);
        await this.decisionScheduler.ScheduleAsync(payload, cancellationToken);

        ShareModerationDecisionExecutionOutcome outcome;
        try
        {
            outcome = await this.decisionExecutor.ExecuteAsync(payload, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult.Success();
        }

        return outcome switch
        {
            ShareModerationDecisionExecutionOutcome.Succeeded => ApplicationResult.Success(),
            ShareModerationDecisionExecutionOutcome.RetryableConflict => ApplicationResult.Success(),
            ShareModerationDecisionExecutionOutcome.ReportNotFound =>
                ApplicationResult.Failure(SharingApplicationErrors.ModerationReportNotFound()),
            ShareModerationDecisionExecutionOutcome.TargetNotFound =>
                ApplicationResult.Failure(SharingApplicationErrors.ModerationTargetNotFound()),
            ShareModerationDecisionExecutionOutcome.InvalidTransition =>
                ApplicationResult.Success(),
            _ => ApplicationResult.Failure(SharingApplicationErrors.ModerationConflict()),
        };
    }

    private async Task<string?> ResolvePublicTargetIdAsync(
        ShareModerationTargetType targetType,
        ShareToken shareToken,
        CancellationToken cancellationToken)
    {
        if (targetType == ShareModerationTargetType.ProfileComparison)
        {
            ProfileComparison? comparison = await this.comparisonRepository.GetByShareTokenAsync(
                shareToken,
                cancellationToken);
            return comparison?.IsPubliclyResolvable == true
                ? comparison.Id.Value
                : null;
        }

        if (!TryMapPublicationType(targetType, out SharePublicationType publicationType))
        {
            return null;
        }

        SharePublication? publication = await this.publicationRepository.GetResolvableByTokenAsync(
            shareToken,
            cancellationToken);
        return publication?.Type == publicationType ? publication.Id.Value : null;
    }

    private static bool TryMapPublicationType(
        ShareModerationTargetType targetType,
        out SharePublicationType publicationType)
    {
        publicationType = targetType switch
        {
            ShareModerationTargetType.VisitRecap => SharePublicationType.VisitRecap,
            ShareModerationTargetType.YearRecap => SharePublicationType.YearRecap,
            ShareModerationTargetType.PassportProfile => SharePublicationType.PassportProfile,
            ShareModerationTargetType.PersonalRanking => SharePublicationType.PersonalRanking,
            _ => default,
        };
        return targetType != ShareModerationTargetType.ProfileComparison
            && Enum.IsDefined(publicationType);
    }
}
