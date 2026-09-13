using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class ShareModerationService
{
    private readonly IShareModerationReportRepository reportRepository;
    private readonly ISharePublicationRepository publicationRepository;
    private readonly IProfileComparisonRepository comparisonRepository;
    private readonly SharePublicationCacheInvalidationScheduler invalidationScheduler;
    private readonly TimeProvider timeProvider;

    public ShareModerationService(
        IShareModerationReportRepository reportRepository,
        ISharePublicationRepository publicationRepository,
        IProfileComparisonRepository comparisonRepository,
        SharePublicationCacheInvalidationScheduler invalidationScheduler,
        TimeProvider? timeProvider = null)
    {
        this.reportRepository = reportRepository
            ?? throw new ArgumentNullException(nameof(reportRepository));
        this.publicationRepository = publicationRepository
            ?? throw new ArgumentNullException(nameof(publicationRepository));
        this.comparisonRepository = comparisonRepository
            ?? throw new ArgumentNullException(nameof(comparisonRepository));
        this.invalidationScheduler = invalidationScheduler
            ?? throw new ArgumentNullException(nameof(invalidationScheduler));
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

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        long expectedReportVersion = report.Version;
        try
        {
            if (command.Decision == ShareModerationDecision.Dismiss)
            {
                report.Dismiss(command.ReviewerUserId, command.Note, nowUtc);
            }
            else
            {
                ApplicationResult targetResult = command.Decision == ShareModerationDecision.Suspend
                    ? await this.SuspendTargetAsync(report, nowUtc, cancellationToken)
                    : await this.RestoreTargetAsync(report, nowUtc, cancellationToken);
                if (!targetResult.IsSuccess)
                {
                    return targetResult;
                }

                if (command.Decision == ShareModerationDecision.Suspend)
                {
                    report.MarkPublicationSuspended(
                        command.ReviewerUserId,
                        command.Note,
                        nowUtc);
                }
                else
                {
                    report.MarkPublicationRestored(
                        command.ReviewerUserId,
                        command.Note,
                        nowUtc);
                }
            }
        }
        catch (ShareModerationValidationException)
        {
            return ApplicationResult.Failure(
                SharingApplicationErrors.InvalidModerationTransition());
        }

        ShareModerationReportWriteOutcome outcome = await this.reportRepository.ReplaceAsync(
            report,
            expectedReportVersion,
            cancellationToken);
        return outcome == ShareModerationReportWriteOutcome.Success
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(SharingApplicationErrors.ModerationConflict());
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

    private async Task<ApplicationResult> SuspendTargetAsync(
        ShareModerationReport report,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (report.Status != ShareModerationReportStatus.Pending)
        {
            return ApplicationResult.Failure(
                SharingApplicationErrors.InvalidModerationTransition());
        }

        return report.TargetType == ShareModerationTargetType.ProfileComparison
            ? await this.SetComparisonSuspensionAsync(report.TargetRecordId, true, nowUtc, cancellationToken)
            : await this.SetPublicationSuspensionAsync(report, true, nowUtc, cancellationToken);
    }

    private async Task<ApplicationResult> RestoreTargetAsync(
        ShareModerationReport report,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (report.Status != ShareModerationReportStatus.PublicationSuspended)
        {
            return ApplicationResult.Failure(
                SharingApplicationErrors.InvalidModerationTransition());
        }

        return report.TargetType == ShareModerationTargetType.ProfileComparison
            ? await this.SetComparisonSuspensionAsync(report.TargetRecordId, false, nowUtc, cancellationToken)
            : await this.SetPublicationSuspensionAsync(report, false, nowUtc, cancellationToken);
    }

    private async Task<ApplicationResult> SetPublicationSuspensionAsync(
        ShareModerationReport report,
        bool isSuspended,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (!TryMapPublicationType(report.TargetType, out SharePublicationType publicationType)
            || !SharePublicationId.TryParse(report.TargetRecordId, out SharePublicationId publicationId))
        {
            return ApplicationResult.Failure(
                SharingApplicationErrors.ModerationTargetNotFound());
        }

        SharePublication? publication = await this.publicationRepository.GetByIdAsync(
            publicationId,
            cancellationToken);
        if (publication is null || publication.Type != publicationType)
        {
            return ApplicationResult.Failure(
                SharingApplicationErrors.ModerationTargetNotFound());
        }

        string? shareId = publication.ShareToken?.Value;
        if (publication.IsModerationSuspended == isSuspended)
        {
            return ApplicationResult.Success();
        }

        long expectedVersion = publication.Version;
        try
        {
            if (isSuspended)
            {
                publication.SuspendByModeration(nowUtc);
            }
            else
            {
                publication.RestoreAfterModeration(nowUtc);
            }
        }
        catch (SharePublicationValidationException)
        {
            return ApplicationResult.Failure(
                SharingApplicationErrors.InvalidModerationTransition());
        }

        SharePublicationWriteOutcome outcome = await this.publicationRepository.ReplaceAsync(
            publication,
            expectedVersion,
            cancellationToken);
        if (outcome != SharePublicationWriteOutcome.Success)
        {
            return ApplicationResult.Failure(SharingApplicationErrors.ModerationConflict());
        }

        await this.invalidationScheduler.ScheduleAsync(
            publication,
            publication.Version,
            cancellationToken,
            shareId);
        return ApplicationResult.Success();
    }

    private async Task<ApplicationResult> SetComparisonSuspensionAsync(
        string targetRecordId,
        bool isSuspended,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (!ProfileComparisonId.TryParse(targetRecordId, out ProfileComparisonId comparisonId))
        {
            return ApplicationResult.Failure(
                SharingApplicationErrors.ModerationTargetNotFound());
        }

        ProfileComparison? comparison = await this.comparisonRepository.GetByIdAsync(
            comparisonId,
            cancellationToken);
        if (comparison is null || !comparison.IsActive)
        {
            return ApplicationResult.Failure(
                SharingApplicationErrors.ModerationTargetNotFound());
        }

        if (comparison.IsModerationSuspended == isSuspended)
        {
            return ApplicationResult.Success();
        }

        long expectedVersion = comparison.Version;
        try
        {
            if (isSuspended)
            {
                comparison.SuspendByModeration(nowUtc);
            }
            else
            {
                comparison.RestoreAfterModeration(nowUtc);
            }
        }
        catch (ProfileComparisonValidationException)
        {
            return ApplicationResult.Failure(
                SharingApplicationErrors.InvalidModerationTransition());
        }

        ProfileComparisonWriteOutcome outcome = await this.comparisonRepository.ReplaceAsync(
            comparison,
            expectedVersion,
            cancellationToken);
        return outcome == ProfileComparisonWriteOutcome.Success
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(SharingApplicationErrors.ModerationConflict());
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
